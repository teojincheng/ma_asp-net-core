using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ma.ConstantsValues;
using ma.Models;
using Newtonsoft.Json;
using ma.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ma.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {

        Constants constantValues = new Constants();
        private readonly IHostingEnvironment _hostingEnvironment;
        HtmlEncoder _htmlEncoder;
        JavaScriptEncoder _javaScriptEncoder;
        UrlEncoder _urlEncoder;

        public ItemController(IHostingEnvironment hostingEnvironment, HtmlEncoder htmlEncoder,
                             JavaScriptEncoder javascriptEncoder,
                             UrlEncoder urlEncoder)
        {
            _hostingEnvironment = hostingEnvironment;
            _htmlEncoder = htmlEncoder;
            _javaScriptEncoder = javascriptEncoder;
            _urlEncoder = urlEncoder;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Display the form to add item
        /// </summary>
        /// <returns></returns>
        public ActionResult AddItem()
        {

            if (TempData.ContainsKey("addResult"))
            {
                ViewBag.AddResult = TempData["addResult"].ToString();
            }
            return View();
        }

        /// <summary>
        /// server side processing for adding item into database. 
        /// adds attachment into folder
        /// insert database records
        /// </summary>
        /// <param name="viewModel">data filled inside the form</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddItem(AddItemViewModel viewModel)
        {

            var operationResult = "Success";

            System.Globalization.CultureInfo.CurrentCulture.ClearCachedData();

            //3MB
            var fileAttachmentLimit = 3145728;

            List<string> validContentTypes = new List<string>
            {
                "image/gif",
                "image/jpeg",
                "image/png"
            };


            //begin server side validation

            if (string.IsNullOrEmpty(viewModel.Name))
            {
                ModelState.AddModelError("Name", "Name cannot be empty");
                return View();
            }
            if (viewModel.ExpiryDate == DateTime.MinValue)
            {
                ModelState.AddModelError("ExpiryDate", "Make sure Expiry Date is a valid date");
                return View();
            }
            if (viewModel.ExpiryDate < DateTime.Now)
            {
                ModelState.AddModelError("ExpiryDate", "Make sure Expiry Date is later than today.");
                return View();
            }
            if (viewModel.Name.Length > 255)
            {
                ModelState.AddModelError("Name", "Name cannot exceed 255 characters");
                return View();
            }
            if (viewModel.Location.Length > 255)
            {
                ModelState.AddModelError("Location", "Location cannot exceed 255 characters");
                return View();
            }
            if (viewModel.Qty <= 0)
            {
                ModelState.AddModelError("Qty", "Make sure Qty is more than 0");
                return View();
            }
            if (viewModel.AttachmentFile != null)
            {
                if (!validContentTypes.Contains(viewModel.AttachmentFile.ContentType))
                {
                    ModelState.AddModelError("AttachmentFile", "Make sure image is a .gif, .jpeg or .png");
                    return View();
                }
                //check file size

                if (viewModel.AttachmentFile.Length > fileAttachmentLimit)
                {
                    ModelState.AddModelError("AttachmentFile", "Make sure image is less than 3MB in size");
                    return View();
                }
            }


            DateTime DateNow = DateTime.UtcNow;
            //get user id from asp.net identity. 
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int newItemTableId = 0;

            using (SqlConnection sqlConnection = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("InsertIntoItem", sqlConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;



                        SqlParameter param1 = new SqlParameter
                        {
                            ParameterName = "@itemname",
                            Value = viewModel.Name,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };


                        SqlParameter param2 = new SqlParameter
                        {
                            ParameterName = "@itemlocation",
                            Value = viewModel.Location,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param3 = new SqlParameter
                        {
                            ParameterName = "@expirydate",
                            Value = viewModel.ExpiryDate,
                            SqlDbType = SqlDbType.DateTime,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param4 = new SqlParameter
                        {
                            ParameterName = "@othertext",
                            Value = viewModel.Remarks,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param5 = new SqlParameter
                        {
                            ParameterName = "@qty",
                            Value = viewModel.Qty,
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param6 = new SqlParameter
                        {
                            ParameterName = "@userid",
                            Value = userId,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param7 = new SqlParameter
                        {
                            ParameterName = "@newRecordId",
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Output

                        };



                        cmd.Parameters.Add(param1);
                        cmd.Parameters.Add(param2);
                        cmd.Parameters.Add(param3);
                        cmd.Parameters.Add(param4);
                        cmd.Parameters.Add(param5);
                        cmd.Parameters.Add(param6);
                        cmd.Parameters.Add(param7);

                        sqlConnection.Open();
                        //so that it return a output value.
                        cmd.ExecuteScalar();
                        newItemTableId = (int)cmd.Parameters["@newRecordId"].Value;


                    }
                }
                catch (Exception e)
                {
                    operationResult = "Internal Server Error";
                }
            } // end using


            var friendlyFileName = "";
            var storageFileName = "";
            var filePathToSave = "";
            // if there is attachment for the form submission. 
            if (viewModel.AttachmentFile != null)
            {
                //store image info folder that is part of project but outside wwwroot. 
                try
                {
                    friendlyFileName = Path.GetFileName(viewModel.AttachmentFile.FileName);
                    storageFileName = "";
                    filePathToSave = "";

                    //.net core of server.map path
                    string contentRootPath = _hostingEnvironment.ContentRootPath;

                    var filenameWithoutExt = Path.GetFileNameWithoutExtension(viewModel.AttachmentFile.FileName);
                    var fileExt = Path.GetExtension(viewModel.AttachmentFile.FileName);
                    storageFileName = filenameWithoutExt + "-" + Guid.NewGuid().ToString() + "-" + fileExt;

                    filePathToSave = Path.Combine(contentRootPath + "\\Attachments", storageFileName);
                    viewModel.AttachmentFile.CopyTo(new FileStream(filePathToSave, FileMode.Create));
                }
                catch (Exception ex)
                {
                    operationResult = "Internal Server Error";
                }

                //save details of attachement into attachment database table. 

                using (SqlConnection sqlConnection = new SqlConnection(constantValues.SQLConncectionString))
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("InsertIntoAttachment", sqlConnection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;



                            SqlParameter param1 = new SqlParameter
                            {
                                ParameterName = "@filename",
                                Value = storageFileName,
                                SqlDbType = SqlDbType.NVarChar,
                                Direction = ParameterDirection.Input

                            };


                            SqlParameter param2 = new SqlParameter
                            {
                                ParameterName = "@friendlyfilename",
                                Value = friendlyFileName,
                                SqlDbType = SqlDbType.NVarChar,
                                Direction = ParameterDirection.Input

                            };

                            SqlParameter param3 = new SqlParameter
                            {
                                ParameterName = "@filepath",
                                Value = filePathToSave,
                                SqlDbType = SqlDbType.NVarChar,
                                Direction = ParameterDirection.Input

                            };

                            SqlParameter param4 = new SqlParameter
                            {
                                ParameterName = "@itemid",
                                Value = newItemTableId,
                                SqlDbType = SqlDbType.Int,
                                Direction = ParameterDirection.Input

                            };





                            cmd.Parameters.Add(param1);
                            cmd.Parameters.Add(param2);
                            cmd.Parameters.Add(param3);
                            cmd.Parameters.Add(param4);


                            sqlConnection.Open();
                            cmd.ExecuteNonQuery();

                        }
                    }
                    catch (Exception e)
                    {
                        operationResult = "Internal Server Error";
                    }
                } // end using



            }


            //return View();
            TempData["addResult"] = operationResult;
            return RedirectToAction("AddItem");
        }



        public ActionResult EditItem(int id)
        {
            ItemRawDate currentItem = ItemGetItemFromDBrawDate(id);
            EditItemViewModel currentItemViewModel = new EditItemViewModel { Id = currentItem.ID, Name = _htmlEncoder.Encode(currentItem.ItemName), Location = _htmlEncoder.Encode(currentItem.ItemLocation), Remarks = _htmlEncoder.Encode(currentItem.OtherText), Qty = currentItem.Qty, ExpiryDate = currentItem.ExpiryDate, FileName = currentItem.FileName };

            currentItemViewModel.NumOfDates = new List<SelectListItem>
            {
                  new SelectListItem {Value = "1", Text = "1"},
                  new SelectListItem {Value = "2", Text = "2"},
                  new SelectListItem {Value = "3", Text = "3"}
            };

            if (TempData.ContainsKey("editResult"))
            {
                ViewBag.EditResult = TempData["editResult"].ToString();
            }


            return View(currentItemViewModel);
        }

        [HttpPost]
        public ActionResult EditItem(EditItemViewModel viewModel)
        {

            var operationResult = "Success";

            var fileAttachmentLimit = 3145728;

            List<string> validContentTypes = new List<string>
            {
                "image/gif",
                "image/jpeg",
                "image/png"
            };


            //begin server side validation

            if (string.IsNullOrEmpty(viewModel.Name))
            {
                ModelState.AddModelError("Name", "Name cannot be empty");
                return View(viewModel);
            }
            if (viewModel.ExpiryDate == DateTime.MinValue)
            {
                ModelState.AddModelError("ExpiryDate", "Make sure Expiry Date is a valid date");
                return View(viewModel);
            }
            if (viewModel.ExpiryDate < DateTime.Now)
            {
                ModelState.AddModelError("ExpiryDate", "Make sure Expiry Date is later than today.");
                return View(viewModel);
            }
            if (viewModel.Name.Length > 255)
            {
                ModelState.AddModelError("Name", "Name cannot exceed 255 characters");
                return View(viewModel);
            }
            if (viewModel.Location.Length > 255)
            {
                ModelState.AddModelError("Location", "Location cannot exceed 255 characters");
                return View(viewModel);
            }
            if (viewModel.Qty <= 0)
            {
                ModelState.AddModelError("Qty", "Make sure Qty is more than 0");
                return View(viewModel);
            }
            if (viewModel.AttachmentFile != null)
            {
                if (!validContentTypes.Contains(viewModel.AttachmentFile.ContentType))
                {
                    ModelState.AddModelError("AttachmentFile", "Make sure image is a .gif, .jpeg or .png");
                    return View(viewModel);
                }
                //check file size

                if (viewModel.AttachmentFile.Length > fileAttachmentLimit)
                {
                    ModelState.AddModelError("AttachmentFile", "Make sure image is less than 3MB in size");
                    return View(viewModel);
                }
            }


            //update the details of item other than attachment inside db. 
            using (SqlConnection sqlConnection = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("UpdateItem", sqlConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;



                        SqlParameter param1 = new SqlParameter
                        {
                            ParameterName = "@itemName",
                            Value = viewModel.Name,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };


                        SqlParameter param2 = new SqlParameter
                        {
                            ParameterName = "@itemLocation",
                            Value = viewModel.Location,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param3 = new SqlParameter
                        {
                            ParameterName = "@expiryDate",
                            Value = viewModel.ExpiryDate,
                            SqlDbType = SqlDbType.DateTime,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param4 = new SqlParameter
                        {
                            ParameterName = "@otherText",
                            Value = viewModel.Remarks,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param5 = new SqlParameter
                        {
                            ParameterName = "@qty",
                            Value = viewModel.Qty,
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param6 = new SqlParameter
                        {
                            ParameterName = "@id",
                            Value = viewModel.Id,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };





                        cmd.Parameters.Add(param1);
                        cmd.Parameters.Add(param2);
                        cmd.Parameters.Add(param3);
                        cmd.Parameters.Add(param4);
                        cmd.Parameters.Add(param5);
                        cmd.Parameters.Add(param6);


                        sqlConnection.Open();
                        //so that it return a output value.
                        cmd.ExecuteNonQuery();



                    }
                }
                catch (Exception e)
                {
                    operationResult = "Internal Server Error";
                }
            } // end using


            var currentItemToDelete = GetItemFromDBDateFormatted(viewModel.Id);
            //delete the previous image from db

            using (SqlConnection sqlConnection = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("DeleteAttachmentItemId", sqlConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        SqlParameter param1 = new SqlParameter
                        {
                            ParameterName = "@itemid",
                            Value = viewModel.Id,
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Input

                        };

                        cmd.Parameters.Add(param1);

                        sqlConnection.Open();
                        cmd.ExecuteNonQuery();

                    }
                }
                catch (Exception e)
                {
                    operationResult = "Internal Server Error";
                }
            }


            //delete the previous image from disk


            if (!string.IsNullOrEmpty(currentItemToDelete.FileName))
            {
                //delete the image from disk. 
                string contentRootPath = _hostingEnvironment.ContentRootPath;
                string fullImagePath = Path.Combine(contentRootPath + "\\Attachments", currentItemToDelete.FileName);

                if (System.IO.File.Exists(fullImagePath))
                {
                    try
                    {
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        //File.Delete(picturePath);
                        System.IO.File.Delete(fullImagePath);
                    }
                    catch (Exception e)
                    {
                        operationResult = "Internal Server Error";
                    }
                }

            }



            //add the image to disk

            var friendlyFileName = "";
            var storageFileName = "";
            var filePathToSave = "";
            // if there is attachment for the form submission. 
            if (viewModel.AttachmentFile != null)
            {
                //store image info folder that is part of project but outside wwwroot. 
                try
                {
                    friendlyFileName = Path.GetFileName(viewModel.AttachmentFile.FileName);
                    storageFileName = "";
                    filePathToSave = "";

                    //.net core of server.map path
                    string contentRootPath = _hostingEnvironment.ContentRootPath;

                    var filenameWithoutExt = Path.GetFileNameWithoutExtension(viewModel.AttachmentFile.FileName);
                    var fileExt = Path.GetExtension(viewModel.AttachmentFile.FileName);
                    storageFileName = filenameWithoutExt + "-" + Guid.NewGuid().ToString() + "-" + fileExt;

                    filePathToSave = Path.Combine(contentRootPath + "\\Attachments", storageFileName);
                    viewModel.AttachmentFile.CopyTo(new FileStream(filePathToSave, FileMode.Create));
                }
                catch (Exception ex)
                {
                    operationResult = "Internal Server Error";
                }
            }

            //add image to db. 


            using (SqlConnection sqlConnection = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("InsertIntoAttachment", sqlConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;



                        SqlParameter param1 = new SqlParameter
                        {
                            ParameterName = "@filename",
                            Value = storageFileName,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };


                        SqlParameter param2 = new SqlParameter
                        {
                            ParameterName = "@friendlyfilename",
                            Value = friendlyFileName,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param3 = new SqlParameter
                        {
                            ParameterName = "@filepath",
                            Value = filePathToSave,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                        SqlParameter param4 = new SqlParameter
                        {
                            ParameterName = "@itemid",
                            Value = viewModel.Id,
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Input

                        };





                        cmd.Parameters.Add(param1);
                        cmd.Parameters.Add(param2);
                        cmd.Parameters.Add(param3);
                        cmd.Parameters.Add(param4);


                        sqlConnection.Open();
                        cmd.ExecuteNonQuery();

                    }
                }
                catch (Exception e)
                {
                    operationResult = "Internal Server Error";
                }
            } // end using


            TempData["editResult"] = operationResult;
            return RedirectToAction("EditItem", new { id = viewModel.Id });
        }



        /// <summary>
        /// make sense of what the user wants to search/sort with datatables
        /// </summary>
        /// <param name="model">data sent by datatables js</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ItemsTableProcessing(DataTablesAjaxPostModel model)
        {

            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

            string search = "";
            string sortBy = "";
            string sortDirection = "";
            if (model.search != null)
            {
                search = model.search.value;
            }

            if (model.columns.Count > 0)
            {
                sortBy = model.columns[model.order[0].column].data;
            }

            if (model.order.Count > 0)
            {

                sortDirection = model.order[0].dir;
            }
            /*
             * sortby value default is id
             * sortdirection value default is asc
             */
            var listOfItem = new List<Item>();

            DataSet ds = new DataSet("Items");
            using (SqlConnection conn = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    SqlCommand sqlComm = new SqlCommand("SelectFromItemBasedOn", conn);
                    sqlComm.CommandType = CommandType.StoredProcedure;

                    SqlParameter param1 = new SqlParameter
                    {
                        ParameterName = "@search",
                        Value = search,
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input

                    };


                    SqlParameter param2 = new SqlParameter
                    {
                        ParameterName = "@sortby",
                        Value = sortBy,
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input

                    };

                    SqlParameter param3 = new SqlParameter
                    {
                        ParameterName = "@sortdirection",
                        Value = sortDirection,
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input

                    };

                    SqlParameter param4 = new SqlParameter
                    {
                        ParameterName = "@userid",
                        Value = userId,
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input

                    };
                    sqlComm.Parameters.Add(param1);
                    sqlComm.Parameters.Add(param2);
                    sqlComm.Parameters.Add(param3);
                    sqlComm.Parameters.Add(param4);


                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = sqlComm;
                    da.Fill(ds);

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        int rID = ((int)row["ID"]);
                        string rItemName = _htmlEncoder.Encode((string)row["ItemName"]);
                        string rLocation = _htmlEncoder.Encode((string)row["ItemLocation"]);
                        string rDateTime = _htmlEncoder.Encode((string)row["ExpiryDate"]);
                        string rOtherText = _htmlEncoder.Encode((string)row["OtherText"]);

                        listOfItem.Add(
                            new Item
                            {
                                ID = rID,
                                ItemName = rItemName,
                                ItemLocation = rLocation,
                                ExpiryDate = rDateTime,
                                OtherText = rOtherText
                            }
                            );
                    }
                }
                catch (Exception e)
                {

                }
            }//close sql conn

            int recordsFiltered = listOfItem.Count;
            listOfItem = listOfItem.GetRange(model.start, Math.Min(model.length, listOfItem.Count - model.start));
            To_DatatablesJS<Item> dataTableJsData = new To_DatatablesJS<Item>();
            dataTableJsData.draw = model.draw;
            dataTableJsData.recordsTotal = listOfItem.Count();
            dataTableJsData.recordsFiltered = recordsFiltered;
            dataTableJsData.data = listOfItem;

            var jsonResult = Json(dataTableJsData);
            //jsonResult.MaxJsonLength = int.MaxValue;

            return jsonResult;





        }


        //methods to return data from OR handle ajax request from the view
        #region
        /// <summary>
        /// query the database and return the data for one item
        /// so that the view can use it
        /// </summary>
        /// <param name="id">id of the item in the database</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetDataForOneItem(int id)
        {

            List<Item> listOfItem = new List<Item>();
            DataSet ds = new DataSet("Item");
            using (SqlConnection conn = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    SqlCommand sqlComm = new SqlCommand("SelectOneItem", conn);
                    sqlComm.CommandType = CommandType.StoredProcedure;

                    SqlParameter param1 = new SqlParameter
                    {
                        ParameterName = "@id",
                        Value = id,
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input

                    };



                    sqlComm.Parameters.Add(param1);



                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = sqlComm;
                    da.Fill(ds);

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        int rID = ((int)row["ID"]);
                        string rItemName = ((string)row["ItemName"]);
                        string rLocation = ((string)row["ItemLocation"]);
                        string rDateTime = ((string)row["ExpiryDate"]);
                        string rOtherText = ((string)row["OtherText"]);
                        int rQty = ((int)row["Qty"]);

                        if (!row.IsNull("FileName"))
                        {
                            string rFileName = ((string)row["FileName"]);



                            listOfItem.Add(
                                new Item
                                {
                                    ID = rID,
                                    ItemName = rItemName,
                                    ItemLocation = rLocation,
                                    ExpiryDate = rDateTime,
                                    OtherText = rOtherText,
                                    Qty = rQty,
                                    FileName = rFileName
                                }
                                );

                        }
                        else
                        {

                            listOfItem.Add(
                                new Item
                                {
                                    ID = rID,
                                    ItemName = rItemName,
                                    ItemLocation = rLocation,
                                    ExpiryDate = rDateTime,
                                    OtherText = rOtherText,
                                    Qty = rQty

                                }
                                );
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }//close sql conn

            var item = listOfItem.First();

            return Json(item);
        }

        [HttpPost]
        public string DeleteOneItem(int id)
        {
            var operationResult = "Success";

            //query the database to check if there is image for this item.
            var currentItemToDelete = GetItemFromDBDateFormatted(id);

            using (SqlConnection sqlConnection = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("DeleteOneItem", sqlConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        SqlParameter param1 = new SqlParameter
                        {
                            ParameterName = "@id",
                            Value = id,
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Input

                        };

                        cmd.Parameters.Add(param1);

                        sqlConnection.Open();
                        cmd.ExecuteNonQuery();

                    }
                }
                catch (Exception e)
                {
                    operationResult = "Internal Server Error";
                    return operationResult;
                }
            } // end using



            if (!string.IsNullOrEmpty(currentItemToDelete.FileName))
            {
                //delete the image from disk. 
                string contentRootPath = _hostingEnvironment.ContentRootPath;
                string fullImagePath = Path.Combine(contentRootPath + "\\Attachments", currentItemToDelete.FileName);

                if (System.IO.File.Exists(fullImagePath))
                {
                    try
                    {
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        //File.Delete(picturePath);
                        System.IO.File.Delete(fullImagePath);
                    }
                    catch (Exception e)
                    {
                        operationResult = "Attachment Path. Internal Server Error";
                    }
                }

            }



            return operationResult;

        }
        /// <summary>
        /// At the edit item page, process the form where user select how many reminder dates he wants to set
        /// </summary>
        /// <param name="viewModel">contain data on the number of dates</param>
        [HttpPost]
        public ActionResult SelectReminderDays(EditItemViewModel viewModel)
        {
            List<ReminderDateViewModel> listOfReminderDate = new List<ReminderDateViewModel>();
            for (int i = 0; i < viewModel.SelectNum; i++)
            {
                listOfReminderDate.Add(new ReminderDateViewModel());
            }

            return PartialView("ListReminderParital", listOfReminderDate);
        }


        #endregion

        #region Data Access Layer 

        /// <summary>
        /// query the db and return one Item object
        /// </summary>
        /// <param name="id">id of item in the database</param>
        /// <returns>One item but date is formatted as pretty string</returns>
        public Item GetItemFromDBDateFormatted(int id)
        {

            List<Item> listOfItem = new List<Item>();
            DataSet ds = new DataSet("Item");
            using (SqlConnection conn = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    SqlCommand sqlComm = new SqlCommand("SelectOneItem", conn);
                    sqlComm.CommandType = CommandType.StoredProcedure;

                    SqlParameter param1 = new SqlParameter
                    {
                        ParameterName = "@id",
                        Value = id,
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input

                    };



                    sqlComm.Parameters.Add(param1);



                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = sqlComm;
                    da.Fill(ds);

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        int rID = ((int)row["ID"]);
                        string rItemName = ((string)row["ItemName"]);
                        string rLocation = ((string)row["ItemLocation"]);
                        string rDateTime = ((string)row["ExpiryDate"]);
                        string rOtherText = ((string)row["OtherText"]);
                        int rQty = ((int)row["Qty"]);

                        if (!row.IsNull("FileName"))
                        {
                            string rFileName = ((string)row["FileName"]);



                            listOfItem.Add(
                                new Item
                                {
                                    ID = rID,
                                    ItemName = rItemName,
                                    ItemLocation = rLocation,
                                    ExpiryDate = rDateTime,
                                    OtherText = rOtherText,
                                    Qty = rQty,
                                    FileName = rFileName
                                }
                                );

                        }
                        else
                        {

                            listOfItem.Add(
                                new Item
                                {
                                    ID = rID,
                                    ItemName = rItemName,
                                    ItemLocation = rLocation,
                                    ExpiryDate = rDateTime,
                                    OtherText = rOtherText,
                                    Qty = rQty

                                }
                                );
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }//close sql conn

            var item = listOfItem.First();

            return item;

        }


        /// <summary>
        /// query the db and return one item object
        /// </summary>
        /// <param name="id">id of item in the database</param>
        /// <returns>One item but date is raw sql date time object</returns>
        public ItemRawDate ItemGetItemFromDBrawDate(int id)
        {

            List<ItemRawDate> listOfItem = new List<ItemRawDate>();
            DataSet ds = new DataSet("Item");
            using (SqlConnection conn = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    SqlCommand sqlComm = new SqlCommand("SelectOneItemRawDate", conn);
                    sqlComm.CommandType = CommandType.StoredProcedure;

                    SqlParameter param1 = new SqlParameter
                    {
                        ParameterName = "@id",
                        Value = id,
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input

                    };



                    sqlComm.Parameters.Add(param1);



                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = sqlComm;
                    da.Fill(ds);

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        int rID = ((int)row["ID"]);
                        string rItemName = ((string)row["ItemName"]);
                        string rLocation = ((string)row["ItemLocation"]);
                        DateTime rDateTime = DateTime.Parse(row["ExpiryDate"].ToString());
                        string rOtherText = ((string)row["OtherText"]);
                        int rQty = ((int)row["Qty"]);

                        if (!row.IsNull("FileName"))
                        {
                            string rFileName = ((string)row["FileName"]);



                            listOfItem.Add(
                                new ItemRawDate
                                {
                                    ID = rID,
                                    ItemName = rItemName,
                                    ItemLocation = rLocation,
                                    ExpiryDate = rDateTime,
                                    OtherText = rOtherText,
                                    Qty = rQty,
                                    FileName = rFileName
                                }
                                );

                        }
                        else
                        {

                            listOfItem.Add(
                                new ItemRawDate
                                {
                                    ID = rID,
                                    ItemName = rItemName,
                                    ItemLocation = rLocation,
                                    ExpiryDate = rDateTime,
                                    OtherText = rOtherText,
                                    Qty = rQty

                                }
                                );
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }//close sql conn

            var item = listOfItem.First();

            return item;

        }


        #endregion
    }
}
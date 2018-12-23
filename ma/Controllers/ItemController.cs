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

namespace ma.Controllers
{
    public class ItemController : Controller
    {

        Constants constantValues = new Constants();
        private readonly IHostingEnvironment _hostingEnvironment;

        public ItemController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
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
            System.Globalization.CultureInfo.CurrentCulture.ClearCachedData();
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



                     SqlParameter param1 =   new SqlParameter
                        {
                            ParameterName = "@itemname",
                            Value = viewModel.Name,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };


                      SqlParameter param2 =  new SqlParameter {
                            ParameterName = "@itemlocation",
                            Value = viewModel.Location,
                            SqlDbType = SqlDbType.NVarChar,
                            Direction = ParameterDirection.Input

                        };

                       SqlParameter param3= new SqlParameter
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
                }catch(Exception ex)
                {

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

                    }
                } // end using



            }


            return View();
        }



        /// <summary>
        /// make sense of what the user wants to search/sort with datatables
        /// </summary>
        /// <param name="model">data sent by datatables js</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ItemsTableProcessing(DataTablesAjaxPostModel model)
        {
            string search = "";
            string sortBy = "";
            string sortDirection = "";
            if (model.search != null)
            {
                search = model.search.value;
            }
          
            if(model.columns.Count > 0)
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
                    sqlComm.Parameters.Add(param1);
                    sqlComm.Parameters.Add(param2);
                    sqlComm.Parameters.Add(param3);


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
                catch(Exception e)
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


        //methods to return data from ajax request from the view
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


        #endregion


    }
}
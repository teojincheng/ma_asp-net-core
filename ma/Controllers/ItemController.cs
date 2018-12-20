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


namespace ma.Controllers
{
    public class ItemController : Controller
    {

        Constants constantValues = new Constants();


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
        /// </summary>
        /// <param name="viewModel">data filled inside the form</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddItem(AddItemViewModel viewModel)
        {
            System.Globalization.CultureInfo.CurrentCulture.ClearCachedData();
            DateTime DateNow = DateTime.UtcNow;

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


            return View();
        }


        /// <summary>
        /// make sense of what the user wants to search/sort with datatables
        /// call another function to do server side processing of data
        /// </summary>
        /// <param name="model">data sent by datatables js</param>
        /// <returns></returns>
        public ActionResult ItemsTableProcessing(DataTablesAjaxPostModel model)
        {
            //string search = model.search.value;
            string sortBy = "";
            string sortDirection = "";
            //sortBy = model.columns[model.order[0].column].data;
            //sortDirection = model.order[0].dir;

            var listOfItem = new List<Item>();

            DataSet ds = new DataSet("Items");
            using (SqlConnection conn = new SqlConnection(constantValues.SQLConncectionString))
            {
                try
                {
                    SqlCommand sqlComm = new SqlCommand("SelectFromItem", conn);
                    sqlComm.CommandType = CommandType.StoredProcedure;
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


    }
}
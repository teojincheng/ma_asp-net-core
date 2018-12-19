using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ma.ConstantsValues;
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
                            Value = DateNow,
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


    }
}
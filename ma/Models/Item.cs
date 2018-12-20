using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ma.Models
{
    /// <summary>
    /// data structure to represent one item to be keep tracked of
    /// this class is used to structure data recived from sql database
    /// </summary>
    public class Item
    {
        public int ID { get; set; }
        public string ItemName { get; set; }
        public string ItemLocation { get; set; }
        //public DateTime ExpiryDate { get; set; }
        public string ExpiryDate { get; set; }
        public string OtherText { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ma.Models
{
    public class ItemRawDate
    {
        public int ID { get; set; }
        public string ItemName { get; set; }
        public string ItemLocation { get; set; }
        public DateTime ExpiryDate { get; set; }
        //public string ExpiryDate { get; set; }
        public string OtherText { get; set; }
        public string UserId { get; set; }
        public int Qty { get; set; }
        public string FileName { get; set; }
    }
}

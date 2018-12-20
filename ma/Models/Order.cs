using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ma.Models
{
    /// <summary>
    /// to represent the sequencing or 'order' the data is displayed on datatables js
    /// </summary>
    public class Order
    {
        public int column { get; set; }
        public string dir { get; set; }
    }
}

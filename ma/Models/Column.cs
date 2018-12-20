using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ma.Models
{
    /// <summary>
    /// to represent which 'column' operations are acted on for datatables js
    /// </summary>
    public class Column
    {
        public string data { get; set; }
        public string name { get; set; }
        public bool searchable { get; set; }
        public bool orderable { get; set; }
        public Search search { get; set; }
    }
}

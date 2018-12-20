using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ma.Models
{
    /// <summary>
    /// to represent the search term keyed in by user for datatables js
    /// </summary>
    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
    }
}

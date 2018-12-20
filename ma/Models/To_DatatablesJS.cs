using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ma.Models
{
    /// <summary>
    /// data structure to represent data sent to datatables js as part of server side processing
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class To_DatatablesJS<T>
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public List<T> data { get; set; }
    }
}

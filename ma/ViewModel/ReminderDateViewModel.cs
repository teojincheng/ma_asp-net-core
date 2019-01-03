using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ma.ViewModel
{
    public class ReminderDateViewModel
    {

        [Display(Name = "Reminder Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{dd/MM/yyyy}")]
        public DateTime ReminderDate { get; set; }
    }
}

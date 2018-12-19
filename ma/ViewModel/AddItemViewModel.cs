using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ma.ViewModel
{
    /// <summary>
    /// ViewModel to generate input fields in the html form
    /// </summary>
    public class AddItemViewModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Location { get; set; }
        [Required]
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{dd/MM/yyyy}")]
        public DateTime ExpiryDate {get;set;}
        public string Remarks { get; set; }


    }
}

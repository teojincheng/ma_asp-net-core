using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ma.ViewModel
{
    public class EditItemViewModel
    {
        [HiddenInput(DisplayValue = false)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Location { get; set; }

        [Required]
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{dd/MM/yyyy}")]
        public DateTime ExpiryDate { get; set; }

        public string Remarks { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        public int Qty { get; set; }

        [Display(Name = "Item Picture")]
        [DataType(DataType.Upload)]
        public IFormFile AttachmentFile { get; set; }
    }
}

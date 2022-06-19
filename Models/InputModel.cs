using InvoiceAPIv2.Models.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InvoiceAPI.Models
{
    public class InputModel
    {
        public int? InvoiceID { get; set; }

        [Required]
        public string Description { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }
        public string? PaymentTerms { get; set; }
        public Client Client { get; set; }
        
        public string? BillFromAddress { get; set; }
       
        public string? BillFromCity { get; set; }
      
        public string? BillFromCountry { get; set; }
       
        public string? BillFromPostal { get; set; }
        public List<Item> Items { get; set; }
    }
}

using InvoiceAPI.Models;
using InvoiceAPIv2.Models.Data;
using System;
using System.Collections.Generic;

namespace InvoiceAPIv2.Models.View
{
    public class InvoiceDetailViewModel
    {
        public int InvoiceId { get; set; }
        public string Description { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime PaymentDue { get; set; }
        public int TotalFees { get; set; }
        public string Status { get; set; }
        public Client Client { get; set; } = new Client();


        public string BillFromAddress { get; set; }
        public string BillFromCity { get; set; }
        public string BillFromCountry { get; set; }
        public string BillFromPostal { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();


    }
}

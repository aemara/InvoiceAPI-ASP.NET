using System;
using System.Collections.Generic;

namespace InvoiceAPIv2.Models.Data
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public string ClientId { get; set; }

        public string Description { get; set; }

        /*COULD BE DELETED*/
        public List<Item> Items { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime PaymentDue { get; set; }
        public int TotalFees { get; set; }
        public string Status { get; set; } = "pending";


        public string BillFromAddress { get; set; }
        public string BillFromCity { get; set; }
        public string BillFromCountry { get; set; }
        public string BillFromPostal { get; set; }

    }
}

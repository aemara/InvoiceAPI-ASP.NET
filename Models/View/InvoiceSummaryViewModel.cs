using System;

namespace InvoiceAPIv2.Models.View
{
    public class InvoiceSummaryViewModel
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public DateTime InvoiceDueDate { get; set; }
        public int TotalFees { get; set; }
        public string Status { get; set; }


    }
}

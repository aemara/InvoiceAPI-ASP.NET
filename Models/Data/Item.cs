using System.ComponentModel.DataAnnotations;

namespace InvoiceAPIv2.Models.Data
{
    public class Item
    {
        public int? InvoiceItemId { get; set; }
        public int? InvoiceId { get; set; }

        public string Name { get; set; }

        public int Quantity { get; set; }

        public int Price { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace InvoiceAPI.Models
{
    public class Client
    {
        public string? ClientId { get; set; }

        [Required]
        public string ClientName { get; set; }
       
        public string? ClientEmail { get; set; }
        [Required]
        public string ClientAddress { get; set; }
      
        public string? ClientCity { get; set; }
       
        public string? ClientCountry { get; set; }
        
        public string? ClientPostal { get; set; }
    }
}

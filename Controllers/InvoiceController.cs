using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InvoiceAPI.Models;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using InvoiceAPIv2;
using AspNetCore.Reporting;
using InvoiceAPIv2.Models.View;
using ClosedXML.Excel;
using System.IO;

namespace InvoiceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
       
        private readonly InvoiceService _service;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public InvoicesController(InvoiceService service, IWebHostEnvironment webHostEnvironment)
        {
            _service = service;
            _webHostEnvironment = webHostEnvironment;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceSummaryViewModel>>> GetInvoices()
        {

            return await _service.GetInvoices();
        }

        // GET: api/Invoices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDetailViewModel>> GetInvoice(int id)
        {
            string Id = id.ToString();
            return await _service.GetInvoice(Id);
        }

        // PUT: api/Invoices/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInvoice(int id, InputModel input)
        {
           

            _service.EditInvoice(input, id.ToString());

            return NoContent();
        }

        // POST: api/Invoices
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<InvoiceDetailViewModel> PostInvoice(InputModel input)
        {
            InvoiceDetailViewModel invoice = await _service.AddInvoice(input);
            return invoice;
        }

        /*// DELETE: api/Invoices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            if (_context.Invoices == null)
            {
                return NotFound();
            }
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }*/

        [HttpGet("pdf/{id}")]
        public async Task<IActionResult> GetPrint(string id)
        {
            string mimetype = "";
            int extension = 1;
            var path = $"{this._webHostEnvironment.ContentRootPath}\\ReportFiles\\InvoiceReport.rdlc";

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            //parameters.Add("prm", "RDLC Report");
            LocalReport localReport = new LocalReport(path);
            localReport.AddDataSource("InvoiceDS", await _service.GetInvoiceInfo(id));

            var result = localReport.Execute(RenderType.Pdf, extension, parameters, mimetype);

            //return File(result.MainStream, "application/pdf");
            return File(result.MainStream, System.Net.Mime.MediaTypeNames.Application.Pdf, "InvoiceReport" + ".pdf");

        }

        [HttpGet("excel/{id}")]
        public async Task<IActionResult> OnGetExcelReport(string id)
        {
            var dt = await _service.GetInvoiceInfo(id);

            string Filename = "Invoice Report";
            using (XLWorkbook wb = new XLWorkbook())
            {
                dt.TableName = "Invoice Report";
                wb.Worksheets.Add(dt);

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Filename + ".xlsx");
                }
            }

        }
    }
}

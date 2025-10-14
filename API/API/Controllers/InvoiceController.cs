using Core.DTOs.Invoice;
using Core.Helpers;
using Core.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("{invoiceId}")]
        public async Task<IActionResult> GetInvoiceByIdAsync(int invoiceId)
        {
            var userId = UserHelpers.GetUserId(User);
            var roles = UserHelpers.GetUserRoles(User)?.ToArray();
            var result = await _invoiceService.GetInvoiceByIdAsync(invoiceId, userId, roles);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoicesAsync([FromQuery] InvoiceFilterDto filter)
        {
            var userId = UserHelpers.GetUserId(User);
            var roles = UserHelpers.GetUserRoles(User)?.ToArray();
            var result = await _invoiceService.GetInvoicesAsync(filter, userId, roles);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{invoiceId}/pdf")]
        public async Task<IActionResult> GetInvoicePdfAsync(int invoiceId)
        {
            var userId = UserHelpers.GetUserId(User);
            var roles = UserHelpers.GetUserRoles(User)?.ToArray();
            var result = await _invoiceService.GetInvoicePdfAsync(invoiceId, userId, roles);
            
            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, result);

            return File(result.Data, "application/pdf", $"Invoice-{invoiceId}.pdf");
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;
using System.Security.Claims;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporarily disabled for testing
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all purchase orders
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrders()
    {
        try
        {
            var purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            return Ok(purchaseOrders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders");
            return StatusCode(500, new { message = "An error occurred while retrieving purchase orders" });
        }
    }

    /// <summary>
    /// Get purchase order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(string id)
    {
        try
        {
            var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);

            if (purchaseOrder == null)
            {
                return NotFound(new { message = $"Purchase order with ID {id} not found" });
            }

            return Ok(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order with ID: {PurchaseOrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the purchase order" });
        }
    }

    /// <summary>
    /// Create a new purchase order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(createDto, userId);

            return CreatedAtAction(nameof(GetPurchaseOrder), new { id = purchaseOrder.Id }, purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order: {PurchaseOrderId}", createDto.Id);
            return StatusCode(500, new { message = "An error occurred while creating the purchase order" });
        }
    }
}

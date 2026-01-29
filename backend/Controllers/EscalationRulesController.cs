using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Interfaces.Services.Implementations;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/escalation-rules")]
[Authorize(Roles = "Administrator")]
public class EscalationRulesController : ControllerBase
{
    private readonly IEscalationRuleService escalationRuleService;
    public EscalationRulesController(
        IEscalationRuleService escalationRuleService)
    {
        this.escalationRuleService = escalationRuleService;
        }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EscalationRuleDto>>> GetRules()
    {
        try
        {
            var rules = await escalationRuleService.GetAllAsync();
            return Ok(rules);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "Fehler beim Laden der Eskalationsregeln" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EscalationRuleDto>> GetRule(int id)
    {
        try
        {
            var rule = await escalationRuleService.GetByIdAsync(id);
            if (rule == null) return NotFound(new { message = "Regel nicht gefunden" });
            return Ok(rule);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "Fehler beim Laden der Eskalationsregel" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<EscalationRuleDto>> CreateRule([FromBody] CreateEscalationRuleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await escalationRuleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetRule), new { id = created.Id }, created);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "Fehler beim Anlegen der Eskalationsregel" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EscalationRuleDto>> UpdateRule(int id, [FromBody] UpdateEscalationRuleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await escalationRuleService.UpdateAsync(id, dto);
            if (updated == null) return NotFound(new { message = "Regel nicht gefunden" });
            return Ok(updated);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "Fehler beim Aktualisieren der Eskalationsregel" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        try
        {
            var deleted = await escalationRuleService.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = "Regel nicht gefunden" });
            return NoContent();
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "Fehler beim Löschen der Eskalationsregel" });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Infrastructure.Data;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferencesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReferencesController> _logger;

    public ReferencesController(ApplicationDbContext context, ILogger<ReferencesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category = null)
    {
        var query = _context.ReferenceValues.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(r => r.Category == category);
        }

        var references = await query
            .OrderBy(r => r.Category)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new ReferenceValueDto
            {
                Id = r.Id,
                Category = r.Category,
                Code = r.Code,
                Value = r.Name,
                Description = r.Description,
                Order = r.SortOrder,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(references);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.ReferenceValues
            .Select(r => r.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("by-category")]
    public async Task<IActionResult> GetByCategory()
    {
        var references = await _context.ReferenceValues
            .Where(r => r.IsActive)
            .OrderBy(r => r.Category)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var grouped = references
            .GroupBy(r => r.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new ReferenceValueDto
                {
                    Id = r.Id,
                    Category = r.Category,
                    Code = r.Code,
                    Value = r.Name,
                    Description = r.Description,
                    Order = r.SortOrder,
                    IsActive = r.IsActive
                }).ToList()
            );

        return Ok(grouped);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateReferenceRequest request)
    {
        var reference = new ReferenceValue
        {
            Category = request.Category,
            Code = request.Code,
            Name = request.Value,
            Description = request.Description,
            SortOrder = request.Order,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reference value created: {Category} - {Value}",
            reference.Category, reference.Name);

        return CreatedAtAction(nameof(GetAll), new { }, reference);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReferenceRequest request)
    {
        var reference = await _context.ReferenceValues.FindAsync(id);
        if (reference == null)
            return NotFound();

        reference.Name = request.Value;
        reference.Description = request.Description;
        reference.SortOrder = request.Order;
        reference.IsActive = request.IsActive;
        reference.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var reference = await _context.ReferenceValues.FindAsync(id);
        if (reference == null)
            return NotFound();

        reference.IsActive = false;
        reference.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reference value deactivated: {Category} - {Value}",
            reference.Category, reference.Name);

        return NoContent();
    }

    [HttpPost("{id}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var reference = await _context.ReferenceValues.FindAsync(id);
        if (reference == null)
            return NotFound();

        reference.IsActive = !reference.IsActive;
        reference.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { isActive = reference.IsActive });
    }
}

// DTOs
public record ReferenceValueDto
{
    public int Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Order { get; init; }
    public bool IsActive { get; init; }
}

public record CreateReferenceRequest(
    string Category,
    string Code,
    string Value,
    string? Description,
    int Order
);

public record UpdateReferenceRequest(
    string Value,
    string? Description,
    int Order,
    bool IsActive
);

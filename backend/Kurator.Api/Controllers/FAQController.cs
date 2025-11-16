using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Infrastructure.Data;
using System.Security.Claims;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FAQController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FAQController> _logger;

    public FAQController(ApplicationDbContext context, ILogger<FAQController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // ИЗМЕНЕНО: Убран параметр visibility, теперь фильтруем только по IsActive
        // Все активные FAQ видны всем пользователям
        var faqs = await _context.FAQs
            .Where(f => f.IsActive)
            .OrderBy(f => f.SortOrder)
            .ThenByDescending(f => f.UpdatedAt)
            .Select(f => new FAQDto
            {
                Id = f.Id,
                Title = f.Title,
                Content = f.Content,
                SortOrder = f.SortOrder,
                IsActive = f.IsActive,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                UpdatedBy = f.UpdatedBy
            })
            .ToListAsync();

        return Ok(faqs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // ИЗМЕНЕНО: Убрана проверка visibility, теперь проверяем только IsActive
        var faq = await _context.FAQs.FindAsync(id);

        if (faq == null || !faq.IsActive)
            return NotFound();

        var result = new FAQDto
        {
            Id = faq.Id,
            Title = faq.Title,
            Content = faq.Content,
            SortOrder = faq.SortOrder,
            IsActive = faq.IsActive,
            CreatedAt = faq.CreatedAt,
            UpdatedAt = faq.UpdatedAt,
            UpdatedBy = faq.UpdatedBy
        };

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateFAQRequest request)
    {
        // ИЗМЕНЕНО: UpdatedBy теперь int вместо string
        var userId = GetUserId();

        var faq = new FAQ
        {
            Title = request.Title,
            Content = request.Content,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        _logger.LogInformation("FAQ created: {Title} by user {UserId}", faq.Title, userId);

        return CreatedAtAction(nameof(GetById), new { id = faq.Id }, new { id = faq.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFAQRequest request)
    {
        // ИЗМЕНЕНО: UpdatedBy теперь int вместо string
        var userId = GetUserId();
        var faq = await _context.FAQs.FindAsync(id);

        if (faq == null)
            return NotFound();

        faq.Title = request.Title;
        faq.Content = request.Content;
        faq.SortOrder = request.SortOrder;
        faq.IsActive = request.IsActive;
        faq.UpdatedAt = DateTime.UtcNow;
        faq.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("FAQ updated: {Title} by user {UserId}", faq.Title, userId);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        // ИЗМЕНЕНО: Теперь используем soft delete через IsActive
        var userId = GetUserId();
        var faq = await _context.FAQs.FindAsync(id);

        if (faq == null)
            return NotFound();

        faq.IsActive = false;
        faq.UpdatedAt = DateTime.UtcNow;
        faq.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("FAQ deactivated: {Title} by user {UserId}", faq.Title, userId);

        return NoContent();
    }
}

// DTOs
// ИЗМЕНЕНО: Убрано поле Visibility, DisplayOrder → SortOrder, UpdatedBy теперь int
public record FAQDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int? UpdatedBy { get; init; }
}

public record CreateFAQRequest(
    string Title,
    string Content,
    int SortOrder
);

public record UpdateFAQRequest(
    string Title,
    string Content,
    int SortOrder,
    bool IsActive
);

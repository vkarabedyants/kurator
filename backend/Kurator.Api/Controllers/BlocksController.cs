using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Infrastructure.Data;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BlocksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BlocksController> _logger;

    public BlocksController(ApplicationDbContext context, ILogger<BlocksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // ИЗМЕНЕНО: Теперь используем BlockCurator table для получения кураторов
        var blocks = await _context.Blocks
            .Include(b => b.CuratorAssignments)
                .ThenInclude(bc => bc.User)
            .Select(b => new BlockDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Description = b.Description,
                Status = b.Status.ToString(),
                Curators = b.CuratorAssignments.Select(bc => new BlockCuratorDto
                {
                    UserId = bc.UserId,
                    UserLogin = bc.User.Login,
                    CuratorType = bc.CuratorType.ToString(),
                    AssignedAt = bc.AssignedAt
                }).ToList(),
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync();

        return Ok(blocks);
    }

    [HttpGet("my-blocks")]
    [Authorize(Roles = "Admin,Curator")]
    public async Task<IActionResult> GetMyBlocks()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var isAdmin = User.IsInRole("Admin");

        List<BlockDto> blocks;

        if (isAdmin)
        {
            // Admins can see all active blocks
            blocks = await _context.Blocks
                .Where(b => b.Status == Core.Enums.BlockStatus.Active)
                .Select(b => new BlockDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    Description = b.Description,
                    Status = b.Status.ToString(),
                    Curators = b.CuratorAssignments.Select(bc => new BlockCuratorDto
                    {
                        UserId = bc.UserId,
                        UserLogin = bc.User.Login,
                        CuratorType = bc.CuratorType.ToString(),
                        AssignedAt = bc.AssignedAt
                    }).ToList(),
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();
        }
        else
        {
            // Curators can only see their assigned blocks
            var userBlockIds = await _context.BlockCurators
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .ToListAsync();

            blocks = await _context.Blocks
                .Where(b => userBlockIds.Contains(b.Id) && b.Status == Core.Enums.BlockStatus.Active)
                .Select(b => new BlockDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    Description = b.Description,
                    Status = b.Status.ToString(),
                    Curators = b.CuratorAssignments.Select(bc => new BlockCuratorDto
                    {
                        UserId = bc.UserId,
                        UserLogin = bc.User.Login,
                        CuratorType = bc.CuratorType.ToString(),
                        AssignedAt = bc.AssignedAt
                    }).ToList(),
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();
        }

        return Ok(blocks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // ИЗМЕНЕНО: Используем BlockCurator table
        var block = await _context.Blocks
            .Include(b => b.CuratorAssignments)
                .ThenInclude(bc => bc.User)
            .Where(b => b.Id == id)
            .Select(b => new BlockDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Description = b.Description,
                Status = b.Status.ToString(),
                Curators = b.CuratorAssignments.Select(bc => new BlockCuratorDto
                {
                    UserId = bc.UserId,
                    UserLogin = bc.User.Login,
                    CuratorType = bc.CuratorType.ToString(),
                    AssignedAt = bc.AssignedAt
                }).ToList(),
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (block == null)
            return NotFound();

        return Ok(block);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBlockRequest request)
    {
        if (await _context.Blocks.AnyAsync(b => b.Code == request.Code))
        {
            return BadRequest(new { message = "Block code already exists" });
        }

        // ИЗМЕНЕНО: Убрали PrimaryCuratorId и BackupCuratorId
        var block = new Block
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Block created: {Code}", block.Code);

        return CreatedAtAction(nameof(GetById), new { id = block.Id }, new { id = block.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBlockRequest request)
    {
        var block = await _context.Blocks.FindAsync(id);
        if (block == null)
            return NotFound();

        // ИЗМЕНЕНО: Убрали PrimaryCuratorId и BackupCuratorId
        block.Name = request.Name;
        block.Description = request.Description;
        block.Status = request.Status;
        block.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Block updated: {Code}", block.Code);

        return NoContent();
    }

    // НОВЫЙ ENDPOINT: Назначение кураторов блоку
    [HttpPost("{id}/curators")]
    public async Task<IActionResult> AssignCurator(int id, [FromBody] AssignCuratorRequest request)
    {
        var block = await _context.Blocks.FindAsync(id);
        if (block == null)
            return NotFound(new { message = "Block not found" });

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return BadRequest(new { message = "User not found" });

        // Проверяем, не назначен ли уже этот куратор с таким типом
        var existing = await _context.BlockCurators
            .FirstOrDefaultAsync(bc => bc.BlockId == id && bc.UserId == request.UserId && bc.CuratorType == request.CuratorType);

        if (existing != null)
            return BadRequest(new { message = "Curator already assigned with this type" });

        var blockCurator = new BlockCurator
        {
            BlockId = id,
            UserId = request.UserId,
            CuratorType = request.CuratorType,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0")
        };

        _context.BlockCurators.Add(blockCurator);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Curator {UserId} assigned to block {BlockId} as {Type}",
            request.UserId, id, request.CuratorType);

        return Ok(new { id = blockCurator.Id });
    }

    // НОВЫЙ ENDPOINT: Удаление назначения куратора
    [HttpDelete("{id}/curators/{curatorAssignmentId}")]
    public async Task<IActionResult> RemoveCurator(int id, int curatorAssignmentId)
    {
        var assignment = await _context.BlockCurators
            .FirstOrDefaultAsync(bc => bc.Id == curatorAssignmentId && bc.BlockId == id);

        if (assignment == null)
            return NotFound();

        _context.BlockCurators.Remove(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Curator assignment {AssignmentId} removed from block {BlockId}",
            curatorAssignmentId, id);

        return NoContent();
    }

    // ИЗМЕНЕНО: Теперь архивируем блок вместо удаления
    [HttpPut("{id}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var block = await _context.Blocks.FindAsync(id);
        if (block == null)
            return NotFound();

        block.Status = Core.Enums.BlockStatus.Archived;
        block.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Block archived: {Code}", block.Code);

        return NoContent();
    }
}

// DTOs
// ИЗМЕНЕНО: Убрали PrimaryCuratorId и BackupCuratorId
public record CreateBlockRequest(
    string Name,
    string Code,
    string? Description,
    Core.Enums.BlockStatus Status
);

public record UpdateBlockRequest(
    string Name,
    string? Description,
    Core.Enums.BlockStatus Status
);

public record BlockDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<BlockCuratorDto> Curators { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record BlockCuratorDto
{
    public int UserId { get; init; }
    public string UserLogin { get; init; } = string.Empty;
    public string CuratorType { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
}

public record AssignCuratorRequest(
    int UserId,
    Core.Enums.CuratorType CuratorType
);

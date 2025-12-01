using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Kurator.Api.Controllers;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Data;
using System.Security.Claims;

namespace Kurator.Tests.Controllers;

/// <summary>
/// Comprehensive tests for BlocksController (requires Admin role)
/// Covers: GetAll, GetMyBlocks, GetById, Create, Update, AssignCurator, RemoveCurator, Archive
/// </summary>
public class BlocksControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BlocksController> _logger;
    private readonly BlocksController _controller;
    private readonly User _adminUser;
    private readonly User _curatorUser;

    public BlocksControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BlocksController>();
        _controller = new BlocksController(_context, _logger);

        // Create users for tests
        _adminUser = new User { Login = "admin", PasswordHash = "hash", Role = UserRole.Admin, IsActive = true };
        _curatorUser = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        _context.Users.AddRange(_adminUser, _curatorUser);
        _context.SaveChanges();

        SetupAdminUser();
    }

    private void SetupAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _adminUser.Id.ToString()),
            new Claim(ClaimTypes.Name, _adminUser.Login),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    private void SetupCuratorUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _curatorUser.Id.ToString()),
            new Claim(ClaimTypes.Name, _curatorUser.Login),
            new Claim(ClaimTypes.Role, "Curator")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ShouldReturnAllBlocksWithCurators()
    {
        // Arrange
        var block1 = new Block
        {
            Name = "Block 1",
            Code = "BLK001",
            Description = "First block",
            Status = BlockStatus.Active
        };
        var block2 = new Block
        {
            Name = "Block 2",
            Code = "BLK002",
            Description = "Second block",
            Status = BlockStatus.Archived
        };

        _context.Blocks.AddRange(block1, block2);

        var blockCurator = new BlockCurator
        {
            BlockId = block1.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(blockCurator);

        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;

        blocks.Should().HaveCount(2);

        // Find Block 1 by code
        var block1Dto = blocks.First(b => b.Code == "BLK001");
        block1Dto.Name.Should().Be("Block 1");
        block1Dto.Status.Should().Be("Active");
        block1Dto.Curators.Should().HaveCount(1);
        block1Dto.Curators.First().UserLogin.Should().Be("curator");
        block1Dto.Curators.First().CuratorType.Should().Be("Primary");
    }

    [Fact]
    public async Task GetAll_WhenNoBlocks_ShouldReturnEmptyList()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;
        blocks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldReturnBlocksWithBothPrimaryAndBackupCurators()
    {
        // Arrange
        var curator2 = new User { Login = "curator2", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(curator2);

        var block = new Block { Name = "Test Block", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var primaryAssignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        var backupAssignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = curator2.Id,
            CuratorType = CuratorType.Backup,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.AddRange(primaryAssignment, backupAssignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;

        var testBlock = blocks.First(b => b.Code == "TST");
        testBlock.Curators.Should().HaveCount(2);
        testBlock.Curators.Should().Contain(c => c.CuratorType == "Primary");
        testBlock.Curators.Should().Contain(c => c.CuratorType == "Backup");
    }

    [Fact]
    public async Task GetAll_ShouldIncludeDescription()
    {
        // Arrange
        var block = new Block { Name = "Test Block", Code = "TST", Description = "Test Description", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;

        var testBlock = blocks.First(b => b.Code == "TST");
        testBlock.Description.Should().Be("Test Description");
    }

    #endregion

    #region GetMyBlocks Tests

    [Fact]
    public async Task GetMyBlocks_AsAdmin_ShouldReturnAllActiveBlocks()
    {
        // Arrange
        var activeBlock = new Block { Name = "Active", Code = "ACT", Status = BlockStatus.Active };
        var archivedBlock = new Block { Name = "Archived", Code = "ARC", Status = BlockStatus.Archived };
        _context.Blocks.AddRange(activeBlock, archivedBlock);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyBlocks();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;

        blocks.Should().HaveCount(1);
        blocks.First().Code.Should().Be("ACT");
    }

    [Fact]
    public async Task GetMyBlocks_AsCurator_ShouldReturnOnlyAssignedBlocks()
    {
        // Arrange
        SetupCuratorUser();

        var assignedBlock = new Block { Name = "Assigned", Code = "ASN", Status = BlockStatus.Active };
        var otherBlock = new Block { Name = "Other", Code = "OTH", Status = BlockStatus.Active };
        _context.Blocks.AddRange(assignedBlock, otherBlock);
        await _context.SaveChangesAsync();

        var assignment = new BlockCurator
        {
            BlockId = assignedBlock.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyBlocks();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;

        blocks.Should().HaveCount(1);
        blocks.First().Code.Should().Be("ASN");
    }

    [Fact]
    public async Task GetMyBlocks_AsCurator_ShouldNotReturnArchivedBlocks()
    {
        // Arrange
        SetupCuratorUser();

        var activeBlock = new Block { Name = "Active", Code = "ACT", Status = BlockStatus.Active };
        var archivedBlock = new Block { Name = "Archived", Code = "ARC", Status = BlockStatus.Archived };
        _context.Blocks.AddRange(activeBlock, archivedBlock);
        await _context.SaveChangesAsync();

        // Assign curator to both blocks
        var assignment1 = new BlockCurator
        {
            BlockId = activeBlock.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        var assignment2 = new BlockCurator
        {
            BlockId = archivedBlock.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.AddRange(assignment1, assignment2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyBlocks();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;

        blocks.Should().HaveCount(1);
        blocks.First().Code.Should().Be("ACT");
    }

    [Fact]
    public async Task GetMyBlocks_AsCurator_WithNoAssignments_ShouldReturnEmptyList()
    {
        // Arrange
        SetupCuratorUser();

        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyBlocks();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;
        blocks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyBlocks_AsCurator_WithBackupRole_ShouldReturnBlock()
    {
        // Arrange
        SetupCuratorUser();

        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var backupAssignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Backup,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(backupAssignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyBlocks();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;
        blocks.Should().HaveCount(1);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnBlockWithCurators()
    {
        // Arrange
        var block = new Block
        {
            Name = "Test Block",
            Code = "TEST",
            Description = "Test description",
            Status = BlockStatus.Active
        };
        _context.Blocks.Add(block);

        var blockCurator = new BlockCurator
        {
            BlockId = block.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Backup,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(blockCurator);

        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(block.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blockDto = okResult.Value.Should().BeOfType<BlockDto>().Subject;

        blockDto.Id.Should().Be(block.Id);
        blockDto.Name.Should().Be("Test Block");
        blockDto.Code.Should().Be("TEST");
        blockDto.Status.Should().Be("Active");
        blockDto.Curators.Should().HaveCount(1);
        blockDto.Curators.First().CuratorType.Should().Be("Backup");
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetById_WithNonPositiveId_ShouldReturnNotFound(int invalidId)
    {
        // Act
        var result = await _controller.GetById(invalidId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ShouldIncludeTimestamps()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-5);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var block = new Block
        {
            Name = "Test Block",
            Code = "TEST",
            Status = BlockStatus.Active,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(block.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blockDto = okResult.Value.Should().BeOfType<BlockDto>().Subject;

        blockDto.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
        blockDto.UpdatedAt.Should().BeCloseTo(updatedAt, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ShouldCreateBlock()
    {
        // Arrange
        var request = new CreateBlockRequest("New Block", "NEWBLK", "New block description", BlockStatus.Active);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));

        var createdBlock = await _context.Blocks.FirstOrDefaultAsync(b => b.Code == "NEWBLK");
        createdBlock.Should().NotBeNull();
        createdBlock!.Name.Should().Be("New Block");
        createdBlock.Description.Should().Be("New block description");
        createdBlock.Status.Should().Be(BlockStatus.Active);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ShouldReturnBadRequest()
    {
        // Arrange
        var existingBlock = new Block { Name = "Existing", Code = "DUPLICATE", Status = BlockStatus.Active };
        _context.Blocks.Add(existingBlock);
        await _context.SaveChangesAsync();

        var request = new CreateBlockRequest("New Block", "DUPLICATE", "Description", BlockStatus.Active);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldSetTimestamps()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        var request = new CreateBlockRequest("New Block", "NEWBLK", null, BlockStatus.Active);

        // Act
        await _controller.Create(request);

        // Assert
        var createdBlock = await _context.Blocks.FirstOrDefaultAsync(b => b.Code == "NEWBLK");
        createdBlock!.CreatedAt.Should().BeAfter(beforeCreation.AddSeconds(-1));
        createdBlock.UpdatedAt.Should().BeAfter(beforeCreation.AddSeconds(-1));
    }

    [Theory]
    [InlineData(BlockStatus.Active)]
    [InlineData(BlockStatus.Archived)]
    public async Task Create_WithDifferentStatuses_ShouldWork(BlockStatus status)
    {
        // Arrange
        var request = new CreateBlockRequest($"Block {status}", $"BLK{status}", null, status);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdBlock = await _context.Blocks.FirstOrDefaultAsync(b => b.Code == $"BLK{status}");
        createdBlock!.Status.Should().Be(status);
    }

    [Fact]
    public async Task Create_WithNullDescription_ShouldWork()
    {
        // Arrange
        var request = new CreateBlockRequest("New Block", "NEWBLK", null, BlockStatus.Active);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdBlock = await _context.Blocks.FirstOrDefaultAsync(b => b.Code == "NEWBLK");
        createdBlock!.Description.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithUnicodeName_ShouldWork()
    {
        // Arrange - Russian name
        var request = new CreateBlockRequest("Блок Операций", "OP", "Описание блока", BlockStatus.Active);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdBlock = await _context.Blocks.FirstOrDefaultAsync(b => b.Code == "OP");
        createdBlock!.Name.Should().Be("Блок Операций");
        createdBlock.Description.Should().Be("Описание блока");
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedBlock()
    {
        // Arrange
        var request = new CreateBlockRequest("New Block", "NEWBLK", "Description", BlockStatus.Active);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().NotBeNull();

        var responseType = createdResult.Value!.GetType();
        var idProperty = responseType.GetProperty("id");
        idProperty.Should().NotBeNull();
        var idValue = (int)idProperty!.GetValue(createdResult.Value)!;
        idValue.Should().BeGreaterThan(0);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateBlock()
    {
        // Arrange
        var block = new Block
        {
            Name = "Original Name",
            Code = "ORIGINAL",
            Description = "Original description",
            Status = BlockStatus.Active
        };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request = new UpdateBlockRequest("Updated Name", "Updated description", BlockStatus.Archived);

        // Act
        var result = await _controller.Update(block.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedBlock = await _context.Blocks.FindAsync(block.Id);
        updatedBlock!.Name.Should().Be("Updated Name");
        updatedBlock.Description.Should().Be("Updated description");
        updatedBlock.Status.Should().Be(BlockStatus.Archived);
        updatedBlock.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpdateBlockRequest("Name", "Description", BlockStatus.Active);

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_ShouldNotChangeCode()
    {
        // Arrange
        var block = new Block
        {
            Name = "Original",
            Code = "ORIGINAL",
            Status = BlockStatus.Active
        };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request = new UpdateBlockRequest("Updated Name", "Description", BlockStatus.Active);

        // Act
        await _controller.Update(block.Id, request);

        // Assert
        var updatedBlock = await _context.Blocks.FindAsync(block.Id);
        updatedBlock!.Code.Should().Be("ORIGINAL");
    }

    [Fact]
    public async Task Update_ShouldNotChangeCreatedAt()
    {
        // Arrange
        var originalCreatedAt = DateTime.UtcNow.AddDays(-7);
        var block = new Block
        {
            Name = "Test",
            Code = "TST",
            Status = BlockStatus.Active,
            CreatedAt = originalCreatedAt
        };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request = new UpdateBlockRequest("Updated", "Description", BlockStatus.Active);

        // Act
        await _controller.Update(block.Id, request);

        // Assert
        var updatedBlock = await _context.Blocks.FindAsync(block.Id);
        updatedBlock!.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Update_WithValidData_ShouldReturnNoContent()
    {
        // Arrange
        var block = new Block { Name = "Original", Code = "ORG", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request = new UpdateBlockRequest("Updated", "Description", BlockStatus.Active);

        // Act
        var result = await _controller.Update(block.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region AssignCurator Tests

    [Fact]
    public async Task AssignCurator_WithValidData_ShouldAssignCurator()
    {
        // Arrange
        var block = new Block { Name = "Test Block", Code = "TEST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request = new AssignCuratorRequest(_curatorUser.Id, CuratorType.Primary);

        // Act
        var result = await _controller.AssignCurator(block.Id, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var assignment = await _context.BlockCurators
            .FirstOrDefaultAsync(bc => bc.BlockId == block.Id && bc.UserId == _curatorUser.Id);
        assignment.Should().NotBeNull();
        assignment!.CuratorType.Should().Be(CuratorType.Primary);
        assignment.AssignedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task AssignCurator_WithInvalidBlockId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new AssignCuratorRequest(_curatorUser.Id, CuratorType.Primary);

        // Act
        var result = await _controller.AssignCurator(999, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AssignCurator_WithInvalidUserId_ShouldReturnBadRequest()
    {
        // Arrange
        var block = new Block { Name = "Test Block", Code = "TEST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request = new AssignCuratorRequest(999, CuratorType.Primary);

        // Act
        var result = await _controller.AssignCurator(block.Id, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignCurator_WithDuplicateAssignment_ShouldReturnBadRequest()
    {
        // Arrange
        var block = new Block { Name = "Test Block", Code = "TEST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);

        var existingAssignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(existingAssignment);
        await _context.SaveChangesAsync();

        var request = new AssignCuratorRequest(_curatorUser.Id, CuratorType.Primary);

        // Act
        var result = await _controller.AssignCurator(block.Id, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignCurator_SameCuratorDifferentType_ShouldWork()
    {
        // Arrange
        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var existingAssignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(existingAssignment);
        await _context.SaveChangesAsync();

        var request = new AssignCuratorRequest(_curatorUser.Id, CuratorType.Backup);

        // Act
        var result = await _controller.AssignCurator(block.Id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var assignments = await _context.BlockCurators
            .Where(bc => bc.BlockId == block.Id && bc.UserId == _curatorUser.Id)
            .ToListAsync();
        assignments.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(CuratorType.Primary)]
    [InlineData(CuratorType.Backup)]
    public async Task AssignCurator_WithDifferentCuratorTypes_ShouldWork(CuratorType curatorType)
    {
        // Arrange
        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request = new AssignCuratorRequest(_curatorUser.Id, curatorType);

        // Act
        var result = await _controller.AssignCurator(block.Id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var assignment = await _context.BlockCurators.FirstOrDefaultAsync(bc =>
            bc.BlockId == block.Id && bc.UserId == _curatorUser.Id);
        assignment!.CuratorType.Should().Be(curatorType);
    }

    [Fact]
    public async Task AssignCurator_ShouldSetAssignedAtTimestamp()
    {
        // Arrange
        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var beforeAssignment = DateTime.UtcNow;
        var request = new AssignCuratorRequest(_curatorUser.Id, CuratorType.Primary);

        // Act
        await _controller.AssignCurator(block.Id, request);

        // Assert
        var assignment = await _context.BlockCurators.FirstOrDefaultAsync(bc =>
            bc.BlockId == block.Id && bc.UserId == _curatorUser.Id);
        assignment!.AssignedAt.Should().BeAfter(beforeAssignment.AddSeconds(-1));
    }

    #endregion

    #region RemoveCurator Tests

    [Fact]
    public async Task RemoveCurator_WithValidData_ShouldRemoveAssignment()
    {
        // Arrange
        var block = new Block { Name = "Test Block", Code = "TEST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var assignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RemoveCurator(block.Id, assignment.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var removedAssignment = await _context.BlockCurators.FindAsync(assignment.Id);
        removedAssignment.Should().BeNull();
    }

    [Fact]
    public async Task RemoveCurator_WithInvalidAssignmentId_ShouldReturnNotFound()
    {
        // Arrange
        var block = new Block { Name = "Test Block", Code = "TEST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RemoveCurator(block.Id, 999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RemoveCurator_WithMismatchedBlockId_ShouldReturnNotFound()
    {
        // Arrange
        var block1 = new Block { Name = "Block 1", Code = "BLK1", Status = BlockStatus.Active };
        var block2 = new Block { Name = "Block 2", Code = "BLK2", Status = BlockStatus.Active };
        _context.Blocks.AddRange(block1, block2);
        await _context.SaveChangesAsync();

        var assignment = new BlockCurator
        {
            BlockId = block1.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(assignment);
        await _context.SaveChangesAsync();

        // Act - Try to remove from wrong block
        var result = await _controller.RemoveCurator(block2.Id, assignment.Id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Archive Tests

    [Fact]
    public async Task Archive_WithValidId_ShouldArchiveBlock()
    {
        // Arrange
        var block = new Block
        {
            Name = "Active Block",
            Code = "ACTIVE",
            Status = BlockStatus.Active
        };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Archive(block.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var archivedBlock = await _context.Blocks.FindAsync(block.Id);
        archivedBlock!.Status.Should().Be(BlockStatus.Archived);
        archivedBlock.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Archive_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.Archive(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Archive_AlreadyArchivedBlock_ShouldStillWork()
    {
        // Arrange
        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Archived };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Archive(block.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var archivedBlock = await _context.Blocks.FindAsync(block.Id);
        archivedBlock!.Status.Should().Be(BlockStatus.Archived);
    }

    [Fact]
    public async Task Archive_ShouldReturnNoContent()
    {
        // Arrange
        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Archive(block.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Create_WithVeryLongName_ShouldWork()
    {
        // Arrange
        var longName = new string('A', 200);
        var request = new CreateBlockRequest(longName, "LONG", null, BlockStatus.Active);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WithSpecialCharactersInCode_ShouldWork()
    {
        // Arrange
        var request = new CreateBlockRequest("Test Block", "TEST-01", null, BlockStatus.Active);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetAll_WithManyBlocks_ShouldReturnAll()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            _context.Blocks.Add(new Block
            {
                Name = $"Block {i}",
                Code = $"BLK{i:D3}",
                Status = BlockStatus.Active
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject;
        blocks.Should().HaveCount(100);
    }

    [Fact]
    public async Task AssignCurator_MultipleCuratorsToSameBlock_ShouldWork()
    {
        // Arrange
        var curator2 = new User { Login = "curator2", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(curator2);

        var block = new Block { Name = "Test", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var request1 = new AssignCuratorRequest(_curatorUser.Id, CuratorType.Primary);
        var request2 = new AssignCuratorRequest(curator2.Id, CuratorType.Backup);

        // Act
        var result1 = await _controller.AssignCurator(block.Id, request1);
        var result2 = await _controller.AssignCurator(block.Id, request2);

        // Assert
        result1.Should().BeOfType<OkObjectResult>();
        result2.Should().BeOfType<OkObjectResult>();

        var assignments = await _context.BlockCurators.Where(bc => bc.BlockId == block.Id).ToListAsync();
        assignments.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllBlocksRegardlessOfOrder()
    {
        // Arrange
        var blockZ = new Block { Name = "Zebra Block", Code = "ZBR", Status = BlockStatus.Active };
        var blockA = new Block { Name = "Alpha Block", Code = "ALP", Status = BlockStatus.Active };
        var blockM = new Block { Name = "Middle Block", Code = "MID", Status = BlockStatus.Active };
        _context.Blocks.AddRange(blockZ, blockA, blockM);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var blocks = okResult.Value.Should().BeAssignableTo<IEnumerable<BlockDto>>().Subject.ToList();

        blocks.Should().HaveCount(3);
        blocks.Should().Contain(b => b.Name == "Alpha Block");
        blocks.Should().Contain(b => b.Name == "Middle Block");
        blocks.Should().Contain(b => b.Name == "Zebra Block");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

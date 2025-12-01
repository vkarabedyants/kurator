using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Kurator.Api.Controllers;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Data;
using System.Security.Claims;

namespace Kurator.Tests.Controllers;

/// <summary>
/// Tests for ReferencesController - Reference data management with categorization
/// </summary>
public class ReferencesControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReferencesController> _logger;
    private readonly ReferencesController _controller;
    private User _adminUser = null!;
    private User _curatorUser = null!;

    public ReferencesControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ReferencesController>();
        _controller = new ReferencesController(_context, _logger);

        SetupTestData();
    }

    private void SetupTestData()
    {
        _adminUser = new User { Login = "admin", PasswordHash = "hash", Role = UserRole.Admin };
        _curatorUser = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.AddRange(_adminUser, _curatorUser);
        _context.SaveChanges();
    }

    private void SetupUser(User user, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithoutCategory_ShouldReturnAllReferences()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var ref1 = new ReferenceValue
        {
            Category = "InteractionType",
            Code = "MEETING",
            Name = "Meeting",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref2 = new ReferenceValue
        {
            Category = "InfluenceStatus",
            Code = "POSITIVE",
            Name = "Positive",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(ref1, ref2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var references = okResult.Value.Should().BeAssignableTo<IEnumerable<ReferenceValueDto>>().Subject;

        references.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithCategory_ShouldReturnFilteredReferences()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var ref1 = new ReferenceValue
        {
            Category = "InteractionType",
            Code = "MEETING",
            Name = "Meeting",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref2 = new ReferenceValue
        {
            Category = "InteractionType",
            Code = "CALL",
            Name = "Call",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref3 = new ReferenceValue
        {
            Category = "InfluenceStatus",
            Code = "POSITIVE",
            Name = "Positive",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(ref1, ref2, ref3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(category: "InteractionType");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var references = okResult.Value.Should().BeAssignableTo<IEnumerable<ReferenceValueDto>>().Subject;

        references.Should().HaveCount(2);
        references.Should().OnlyContain(r => r.Category == "InteractionType");
    }

    [Fact]
    public async Task GetAll_ShouldReturnSortedByCategoryThenSortOrderThenName()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var ref1 = new ReferenceValue
        {
            Category = "B_Category",
            Code = "CODE1",
            Name = "Zebra",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref2 = new ReferenceValue
        {
            Category = "A_Category",
            Code = "CODE2",
            Name = "Apple",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref3 = new ReferenceValue
        {
            Category = "A_Category",
            Code = "CODE3",
            Name = "Banana",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(ref1, ref2, ref3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var references = okResult.Value.Should().BeAssignableTo<List<ReferenceValueDto>>().Subject;

        references.Should().HaveCount(3);
        references[0].Category.Should().Be("A_Category");
        references[0].Value.Should().Be("Apple");
        references[1].Category.Should().Be("A_Category");
        references[1].Value.Should().Be("Banana");
        references[2].Category.Should().Be("B_Category");
    }

    [Fact]
    public async Task GetAll_ShouldIncludeInactiveReferences()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var activeRef = new ReferenceValue
        {
            Category = "Test",
            Code = "ACTIVE",
            Name = "Active",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var inactiveRef = new ReferenceValue
        {
            Category = "Test",
            Code = "INACTIVE",
            Name = "Inactive",
            SortOrder = 2,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(activeRef, inactiveRef);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(category: "Test");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var references = okResult.Value.Should().BeAssignableTo<IEnumerable<ReferenceValueDto>>().Subject;

        // GetAll returns both active and inactive
        references.Should().HaveCount(2);
    }

    #endregion

    #region GetCategories Tests

    [Fact]
    public async Task GetCategories_ShouldReturnDistinctCategories()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var ref1 = new ReferenceValue
        {
            Category = "InteractionType",
            Code = "MEETING",
            Name = "Meeting",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref2 = new ReferenceValue
        {
            Category = "InteractionType",
            Code = "CALL",
            Name = "Call",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref3 = new ReferenceValue
        {
            Category = "InfluenceStatus",
            Code = "POSITIVE",
            Name = "Positive",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(ref1, ref2, ref3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var categories = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;

        categories.Should().HaveCount(2);
        categories.Should().Contain("InteractionType");
        categories.Should().Contain("InfluenceStatus");
    }

    [Fact]
    public async Task GetCategories_ShouldReturnSortedCategories()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var categories = new[] { "Zebra", "Apple", "Mango" };
        foreach (var category in categories)
        {
            _context.ReferenceValues.Add(new ReferenceValue
            {
                Category = category,
                Code = "CODE",
                Name = "Name",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
        });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var resultCategories = okResult.Value.Should().BeAssignableTo<List<string>>().Subject;

        resultCategories[0].Should().Be("Apple");
        resultCategories[1].Should().Be("Mango");
        resultCategories[2].Should().Be("Zebra");
    }

    [Fact]
    public async Task GetCategories_WithNoData_ShouldReturnEmptyList()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var categories = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;
        categories.Should().BeEmpty();
    }

    #endregion

    #region GetByCategory Tests

    [Fact]
    public async Task GetByCategory_ShouldReturnGroupedReferences()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var ref1 = new ReferenceValue
        {
            Category = "InteractionType",
            Code = "MEETING",
            Name = "Meeting",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref2 = new ReferenceValue
        {
            Category = "InfluenceStatus",
            Code = "POSITIVE",
            Name = "Positive",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(ref1, ref2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByCategory();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var grouped = okResult.Value.Should().BeAssignableTo<Dictionary<string, List<ReferenceValueDto>>>().Subject;

        grouped.Should().HaveCount(2);
        grouped.Should().ContainKey("InteractionType");
        grouped.Should().ContainKey("InfluenceStatus");
        grouped["InteractionType"].Should().HaveCount(1);
        grouped["InfluenceStatus"].Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByCategory_ShouldOnlyReturnActiveReferences()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var activeRef = new ReferenceValue
        {
            Category = "Test",
            Code = "ACTIVE",
            Name = "Active",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var inactiveRef = new ReferenceValue
        {
            Category = "Test",
            Code = "INACTIVE",
            Name = "Inactive",
            SortOrder = 2,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(activeRef, inactiveRef);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByCategory();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var grouped = okResult.Value.Should().BeAssignableTo<Dictionary<string, List<ReferenceValueDto>>>().Subject;

        grouped["Test"].Should().HaveCount(1);
        grouped["Test"][0].Code.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetByCategory_ShouldReturnSortedWithinCategories()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var ref1 = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE1",
            Name = "Zebra",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var ref2 = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE2",
            Name = "Apple",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.AddRange(ref1, ref2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByCategory();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var grouped = okResult.Value.Should().BeAssignableTo<Dictionary<string, List<ReferenceValueDto>>>().Subject;

        grouped["Test"][0].Value.Should().Be("Apple");
        grouped["Test"][1].Value.Should().Be("Zebra");
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_AsAdmin_ShouldCreateReference()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new CreateReferenceRequest(
            Category: "NewCategory",
            Code: "NEW_CODE",
            Value: "New Value",
            Description: "Description",
            Order: 5
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdRef = await _context.ReferenceValues.FirstOrDefaultAsync(r => r.Code == "NEW_CODE");
        createdRef.Should().NotBeNull();
        createdRef!.Category.Should().Be("NewCategory");
        createdRef.Code.Should().Be("NEW_CODE");
        createdRef.Name.Should().Be("New Value");
        createdRef.Description.Should().Be("Description");
        createdRef.SortOrder.Should().Be(5);
        createdRef.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldSetTimestamps()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");
        var beforeCreate = DateTime.UtcNow;

        var request = new CreateReferenceRequest(
            Category: "Test",
            Code: "CODE",
            Value: "Value",
            Description: null,
            Order: 1
        );

        // Act
        await _controller.Create(request);

        // Assert
        var createdRef = await _context.ReferenceValues.FirstOrDefaultAsync(r => r.Code == "CODE");
        createdRef!.CreatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
        createdRef.UpdatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Create_WithNullDescription_ShouldSucceed()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new CreateReferenceRequest(
            Category: "Test",
            Code: "CODE",
            Value: "Value",
            Description: null,
            Order: 1
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdRef = await _context.ReferenceValues.FirstOrDefaultAsync(r => r.Code == "CODE");
        createdRef!.Description.Should().BeNull();
    }

    [Fact]
    public async Task Create_ShouldSetIsActiveToTrue()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new CreateReferenceRequest(
            Category: "Test",
            Code: "CODE",
            Value: "Value",
            Description: null,
            Order: 1
        );

        // Act
        await _controller.Create(request);

        // Assert
        var createdRef = await _context.ReferenceValues.FirstOrDefaultAsync(r => r.Code == "CODE");
        createdRef!.IsActive.Should().BeTrue();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_AsAdmin_ShouldUpdateReference()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var reference = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE",
            Name = "Original",
            Description = "Original description",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        var request = new UpdateReferenceRequest(
            Value: "Updated",
            Description: "Updated description",
            Order: 2,
            IsActive: true
        );

        // Act
        var result = await _controller.Update(reference.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedRef = await _context.ReferenceValues.FindAsync(reference.Id);
        updatedRef!.Name.Should().Be("Updated");
        updatedRef.Description.Should().Be("Updated description");
        updatedRef.SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task Update_ShouldNotChangeCategoryOrCode()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var reference = new ReferenceValue
        {
            Category = "OriginalCategory",
            Code = "ORIGINAL_CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        var request = new UpdateReferenceRequest(
            Value: "Updated Name",
            Description: "Description",
            Order: 1,
            IsActive: true
        );

        // Act
        await _controller.Update(reference.Id, request);

        // Assert
        var updatedRef = await _context.ReferenceValues.FindAsync(reference.Id);
        updatedRef!.Category.Should().Be("OriginalCategory");
        updatedRef.Code.Should().Be("ORIGINAL_CODE");
    }

    [Fact]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new UpdateReferenceRequest(
            Value: "Updated",
            Description: null,
            Order: 1,
            IsActive: true
        );

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_ShouldUpdateTimestamp()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var originalTime = DateTime.UtcNow.AddHours(-1);
        var reference = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = originalTime,
            UpdatedAt = originalTime
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        var beforeUpdate = DateTime.UtcNow;

        var request = new UpdateReferenceRequest(
            Value: "Updated",
            Description: null,
            Order: 1,
            IsActive: true
        );

        // Act
        await _controller.Update(reference.Id, request);

        // Assert
        var updatedRef = await _context.ReferenceValues.FindAsync(reference.Id);
        updatedRef!.CreatedAt.Should().Be(originalTime);
        updatedRef.UpdatedAt.Should().BeCloseTo(beforeUpdate, TimeSpan.FromSeconds(2));
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public async Task Deactivate_AsAdmin_ShouldSetIsActiveToFalse()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var reference = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Deactivate(reference.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var deactivatedRef = await _context.ReferenceValues.FindAsync(reference.Id);
        deactivatedRef!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        // Act
        var result = await _controller.Deactivate(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Deactivate_ShouldUpdateTimestamp()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var reference = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        var beforeDeactivate = DateTime.UtcNow;

        // Act
        await _controller.Deactivate(reference.Id);

        // Assert
        var deactivatedRef = await _context.ReferenceValues.FindAsync(reference.Id);
        deactivatedRef!.UpdatedAt.Should().BeCloseTo(beforeDeactivate, TimeSpan.FromSeconds(2));
    }

    #endregion

    #region ToggleActive Tests

    [Fact]
    public async Task ToggleActive_FromActiveToInactive_ShouldDeactivate()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var reference = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ToggleActive(reference.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var returnValue = okResult.Value!;
        var isActiveProperty = returnValue.GetType().GetProperty("isActive");
        var isActive = (bool)isActiveProperty!.GetValue(returnValue)!;
        isActive.Should().BeFalse();

        var toggledRef = await _context.ReferenceValues.FindAsync(reference.Id);
        toggledRef!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_FromInactiveToActive_ShouldActivate()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var reference = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ToggleActive(reference.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var returnValue = okResult.Value!;
        var isActiveProperty = returnValue.GetType().GetProperty("isActive");
        var isActive = (bool)isActiveProperty!.GetValue(returnValue)!;
        isActive.Should().BeTrue();

        var toggledRef = await _context.ReferenceValues.FindAsync(reference.Id);
        toggledRef!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActive_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        // Act
        var result = await _controller.ToggleActive(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToggleActive_ShouldUpdateTimestamp()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var reference = new ReferenceValue
        {
            Category = "Test",
            Code = "CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        var beforeToggle = DateTime.UtcNow;

        // Act
        await _controller.ToggleActive(reference.Id);

        // Assert
        var toggledRef = await _context.ReferenceValues.FindAsync(reference.Id);
        toggledRef!.UpdatedAt.Should().BeCloseTo(beforeToggle, TimeSpan.FromSeconds(2));
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public void Create_AsNonAdmin_ShouldRequireAdminRole()
    {
        var method = typeof(ReferencesController).GetMethod(nameof(ReferencesController.Create));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void Update_AsNonAdmin_ShouldRequireAdminRole()
    {
        var method = typeof(ReferencesController).GetMethod(nameof(ReferencesController.Update));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void Deactivate_AsNonAdmin_ShouldRequireAdminRole()
    {
        var method = typeof(ReferencesController).GetMethod(nameof(ReferencesController.Deactivate));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void ToggleActive_AsNonAdmin_ShouldRequireAdminRole()
    {
        var method = typeof(ReferencesController).GetMethod(nameof(ReferencesController.ToggleActive));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetAll_WithNonExistentCategory_ShouldReturnEmptyList()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var reference = new ReferenceValue
        {
            Category = "RealCategory",
            Code = "CODE",
            Name = "Name",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(reference);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(category: "NonExistentCategory");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var references = okResult.Value.Should().BeAssignableTo<IEnumerable<ReferenceValueDto>>().Subject;
        references.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ShouldSucceed()
    {
        // Note: The controller doesn't prevent duplicate codes
        // This is intentional as codes might be reused across different categories

        // Arrange
        SetupUser(_adminUser, "Admin");

        var existingRef = new ReferenceValue
        {
            Category = "Category1",
            Code = "DUPLICATE",
            Name = "First",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceValues.Add(existingRef);
        await _context.SaveChangesAsync();

        var request = new CreateReferenceRequest(
            Category: "Category2",
            Code: "DUPLICATE",
            Value: "Second",
            Description: null,
            Order: 1
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var duplicates = await _context.ReferenceValues.Where(r => r.Code == "DUPLICATE").ToListAsync();
        duplicates.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByCategory_WithEmptyDatabase_ShouldReturnEmptyDictionary()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        // Act
        var result = await _controller.GetByCategory();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var grouped = okResult.Value.Should().BeAssignableTo<Dictionary<string, List<ReferenceValueDto>>>().Subject;
        grouped.Should().BeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

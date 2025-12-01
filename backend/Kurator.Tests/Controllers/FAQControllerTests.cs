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
/// Tests for FAQController - FAQ management with role-based access control
/// </summary>
public class FAQControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FAQController> _logger;
    private readonly FAQController _controller;
    private User _adminUser = null!;
    private User _curatorUser = null!;

    public FAQControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<FAQController>();
        _controller = new FAQController(_context, _logger);

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
    public async Task GetAll_ShouldReturnOnlyActiveFAQs()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var activeFaq = new FAQ
        {
            Title = "Active FAQ",
            Content = "Active content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var inactiveFaq = new FAQ
        {
            Title = "Inactive FAQ",
            Content = "Inactive content",
            SortOrder = 2,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.AddRange(activeFaq, inactiveFaq);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var faqs = okResult.Value.Should().BeAssignableTo<IEnumerable<FAQDto>>().Subject;

        faqs.Should().HaveCount(1);
        faqs.First().Title.Should().Be("Active FAQ");
        faqs.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_ShouldReturnFAQsSortedBySortOrder()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var faq1 = new FAQ
        {
            Title = "FAQ 1",
            Content = "Content 1",
            SortOrder = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var faq2 = new FAQ
        {
            Title = "FAQ 2",
            Content = "Content 2",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var faq3 = new FAQ
        {
            Title = "FAQ 3",
            Content = "Content 3",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.AddRange(faq1, faq2, faq3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var faqs = okResult.Value.Should().BeAssignableTo<List<FAQDto>>().Subject;

        faqs.Should().HaveCount(3);
        faqs[0].SortOrder.Should().Be(1);
        faqs[1].SortOrder.Should().Be(2);
        faqs[2].SortOrder.Should().Be(3);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyListWhenNoActiveFAQs()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var faqs = okResult.Value.Should().BeAssignableTo<IEnumerable<FAQDto>>().Subject;
        faqs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_AsCurator_ShouldReturnAllActiveFAQs()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var faq = new FAQ
        {
            Title = "Public FAQ",
            Content = "Public content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var faqs = okResult.Value.Should().BeAssignableTo<IEnumerable<FAQDto>>().Subject;
        faqs.Should().HaveCount(1);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnFAQ()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var faq = new FAQ
        {
            Title = "Test FAQ",
            Content = "Test content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(faq.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var faqDto = okResult.Value.Should().BeOfType<FAQDto>().Subject;

        faqDto.Id.Should().Be(faq.Id);
        faqDto.Title.Should().Be("Test FAQ");
        faqDto.Content.Should().Be("Test content");
        faqDto.SortOrder.Should().Be(1);
        faqDto.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_WithInactiveFAQ_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var faq = new FAQ
        {
            Title = "Inactive FAQ",
            Content = "Content",
            SortOrder = 1,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(faq.Id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_AsAdmin_ShouldCreateFAQ()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new CreateFAQRequest(
            Title: "New FAQ",
            Content: "New content",
            SortOrder: 5
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdFaq = await _context.FAQs.FirstOrDefaultAsync(f => f.Title == "New FAQ");
        createdFaq.Should().NotBeNull();
        createdFaq!.Title.Should().Be("New FAQ");
        createdFaq.Content.Should().Be("New content");
        createdFaq.SortOrder.Should().Be(5);
        createdFaq.IsActive.Should().BeTrue();
        createdFaq.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task Create_ShouldSetCreatedAndUpdatedTimestamps()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");
        var beforeCreate = DateTime.UtcNow;

        var request = new CreateFAQRequest(
            Title: "Timestamped FAQ",
            Content: "Content",
            SortOrder: 1
        );

        // Act
        await _controller.Create(request);

        // Assert
        var createdFaq = await _context.FAQs.FirstOrDefaultAsync(f => f.Title == "Timestamped FAQ");
        createdFaq.Should().NotBeNull();
        createdFaq!.CreatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
        createdFaq.UpdatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Create_ShouldSetIsActiveToTrue()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new CreateFAQRequest(
            Title: "Active by default",
            Content: "Content",
            SortOrder: 1
        );

        // Act
        await _controller.Create(request);

        // Assert
        var createdFaq = await _context.FAQs.FirstOrDefaultAsync(f => f.Title == "Active by default");
        createdFaq.Should().NotBeNull();
        createdFaq!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtActionWithId()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new CreateFAQRequest(
            Title: "FAQ with ID",
            Content: "Content",
            SortOrder: 1
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdAtResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtResult.ActionName.Should().Be(nameof(FAQController.GetById));

        var returnValue = createdAtResult.Value.Should().NotBeNull().And.Subject;
        var idProperty = returnValue.GetType().GetProperty("id");
        idProperty.Should().NotBeNull();
        var id = (int)idProperty!.GetValue(returnValue)!;
        id.Should().BeGreaterThan(0);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_AsAdmin_ShouldUpdateFAQ()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var faq = new FAQ
        {
            Title = "Original Title",
            Content = "Original content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        var request = new UpdateFAQRequest(
            Title: "Updated Title",
            Content: "Updated content",
            SortOrder: 2,
            IsActive: true
        );

        // Act
        var result = await _controller.Update(faq.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedFaq = await _context.FAQs.FindAsync(faq.Id);
        updatedFaq.Should().NotBeNull();
        updatedFaq!.Title.Should().Be("Updated Title");
        updatedFaq.Content.Should().Be("Updated content");
        updatedFaq.SortOrder.Should().Be(2);
        updatedFaq.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task Update_ShouldUpdateTimestamp()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var originalTime = DateTime.UtcNow.AddHours(-1);
        var faq = new FAQ
        {
            Title = "Original",
            Content = "Content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = originalTime,
            UpdatedAt = originalTime
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        var beforeUpdate = DateTime.UtcNow;

        var request = new UpdateFAQRequest(
            Title: "Updated",
            Content: "New content",
            SortOrder: 1,
            IsActive: true
        );

        // Act
        await _controller.Update(faq.Id, request);

        // Assert
        var updatedFaq = await _context.FAQs.FindAsync(faq.Id);
        updatedFaq!.CreatedAt.Should().Be(originalTime);
        updatedFaq.UpdatedAt.Should().BeCloseTo(beforeUpdate, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var request = new UpdateFAQRequest(
            Title: "Updated",
            Content: "Content",
            SortOrder: 1,
            IsActive: true
        );

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_ShouldAllowDeactivatingFAQ()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var faq = new FAQ
        {
            Title = "Active FAQ",
            Content = "Content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        var request = new UpdateFAQRequest(
            Title: "Active FAQ",
            Content: "Content",
            SortOrder: 1,
            IsActive: false
        );

        // Act
        await _controller.Update(faq.Id, request);

        // Assert
        var updatedFaq = await _context.FAQs.FindAsync(faq.Id);
        updatedFaq!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_AsAdmin_ShouldSoftDeleteFAQ()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var faq = new FAQ
        {
            Title = "To Delete",
            Content = "Content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(faq.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var deletedFaq = await _context.FAQs.FindAsync(faq.Id);
        deletedFaq.Should().NotBeNull();
        deletedFaq!.IsActive.Should().BeFalse();
        deletedFaq.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        // Act
        var result = await _controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldUpdateTimestamp()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var faq = new FAQ
        {
            Title = "To Delete",
            Content = "Content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        var beforeDelete = DateTime.UtcNow;

        // Act
        await _controller.Delete(faq.Id);

        // Assert
        var deletedFaq = await _context.FAQs.FindAsync(faq.Id);
        deletedFaq!.UpdatedAt.Should().BeCloseTo(beforeDelete, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Delete_ShouldPreserveOtherFields()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var faq = new FAQ
        {
            Title = "Preserve Fields",
            Content = "Content to preserve",
            SortOrder = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        // Act
        await _controller.Delete(faq.Id);

        // Assert
        var deletedFaq = await _context.FAQs.FindAsync(faq.Id);
        deletedFaq!.Title.Should().Be("Preserve Fields");
        deletedFaq.Content.Should().Be("Content to preserve");
        deletedFaq.SortOrder.Should().Be(5);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task Create_AsNonAdmin_ShouldBeDenied()
    {
        // Note: This test verifies the authorization attribute is present
        // In a real scenario, this would be enforced by the authorization middleware
        // which is not active in unit tests. This is a structural test.

        var method = typeof(FAQController).GetMethod(nameof(FAQController.Create));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public async Task Update_AsNonAdmin_ShouldBeDenied()
    {
        var method = typeof(FAQController).GetMethod(nameof(FAQController.Update));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public async Task Delete_AsNonAdmin_ShouldBeDenied()
    {
        var method = typeof(FAQController).GetMethod(nameof(FAQController.Delete));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public async Task GetAll_ShouldAllowAnyAuthenticatedUser()
    {
        var method = typeof(FAQController).GetMethod(nameof(FAQController.GetAll));
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        // Should not have role-specific authorization at method level
        // (class-level [Authorize] applies to all)
        if (authorizeAttribute != null)
        {
            authorizeAttribute.Roles.Should().BeNullOrEmpty();
        }
    }

    #endregion

    #region Edge Cases and Business Logic

    [Fact]
    public async Task Create_WithDuplicateSortOrder_ShouldSucceed()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var existingFaq = new FAQ
        {
            Title = "Existing FAQ",
            Content = "Content",
            SortOrder = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(existingFaq);
        await _context.SaveChangesAsync();

        var request = new CreateFAQRequest(
            Title: "New FAQ",
            Content: "New content",
            SortOrder: 5
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var allFaqs = await _context.FAQs.Where(f => f.SortOrder == 5).ToListAsync();
        allFaqs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithMultipleFAQsSameSortOrder_ShouldSortByUpdatedAtDescending()
    {
        // Arrange
        SetupUser(_curatorUser, "Curator");

        var faq1 = new FAQ
        {
            Title = "FAQ 1",
            Content = "Content 1",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var faq2 = new FAQ
        {
            Title = "FAQ 2",
            Content = "Content 2",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.AddRange(faq1, faq2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var faqs = okResult.Value.Should().BeAssignableTo<List<FAQDto>>().Subject;

        faqs[0].Title.Should().Be("FAQ 2"); // More recent update
        faqs[1].Title.Should().Be("FAQ 1");
    }

    [Fact]
    public async Task Update_MultipleTimes_ShouldTrackLastUpdatedBy()
    {
        // Arrange
        var anotherAdmin = new User { Login = "admin2", PasswordHash = "hash", Role = UserRole.Admin };
        _context.Users.Add(anotherAdmin);
        await _context.SaveChangesAsync();

        SetupUser(_adminUser, "Admin");

        var faq = new FAQ
        {
            Title = "Original",
            Content = "Content",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        // First update by admin
        var request1 = new UpdateFAQRequest("Updated 1", "Content 1", 1, true);
        await _controller.Update(faq.Id, request1);

        // Second update by another admin
        SetupUser(anotherAdmin, "Admin");
        var request2 = new UpdateFAQRequest("Updated 2", "Content 2", 1, true);
        await _controller.Update(faq.Id, request2);

        // Assert
        var updatedFaq = await _context.FAQs.FindAsync(faq.Id);
        updatedFaq!.UpdatedBy.Should().Be(anotherAdmin.Id);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

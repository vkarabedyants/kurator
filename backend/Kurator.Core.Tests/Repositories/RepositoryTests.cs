using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Data;
using Kurator.Infrastructure.Repositories;

namespace Kurator.Core.Tests.Repositories;

/// <summary>
/// Тесты для базового репозитория
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<User> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new Repository<User>(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnEntity()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = "hash123",
            Role = UserRole.Curator,
            IsFirstLogin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be("testuser");
        result.PasswordHash.Should().Be("hash123");
        result.Role.Should().Be(UserRole.Curator);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Login = "user1", PasswordHash = "hash1", Role = UserRole.Curator },
            new User { Login = "user2", PasswordHash = "hash2", Role = UserRole.Admin },
            new User { Login = "user3", PasswordHash = "hash3", Role = UserRole.ThreatAnalyst }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(u => u.Login == "user1");
        result.Should().Contain(u => u.Login == "user2");
        result.Should().Contain(u => u.Login == "user3");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoEntities_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var user = new User
        {
            Login = "newuser",
            PasswordHash = "newhash",
            Role = UserRole.Curator
        };

        // Act
        var result = await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(user);

        var savedUser = await _context.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Login.Should().Be("newuser");
        savedUser.PasswordHash.Should().Be("newhash");
    }

    [Fact]
    public async Task AddAsync_ShouldReturnEntityBeforeSave()
    {
        // Arrange
        var user = new User { Login = "testuser", PasswordHash = "hash", Role = UserRole.Curator };

        // Act
        var result = await _repository.AddAsync(user);

        // Assert
        result.Should().Be(user);
        result.Id.Should().BeGreaterThan(0); // EF присваивает ID при добавлении
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var user = new User
        {
            Login = "originaluser",
            PasswordHash = "originalhash",
            Role = UserRole.Curator,
            IsFirstLogin = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        user.Login = "updateduser";
        user.IsFirstLogin = false;
        await _repository.UpdateAsync(user);
        await _repository.SaveChangesAsync();

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Login.Should().Be("updateduser");
        updatedUser.IsFirstLogin.Should().BeFalse();
        updatedUser.PasswordHash.Should().Be("originalhash"); // не изменено
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        // Arrange
        var user = new User { Login = "user", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(user);
        await _repository.SaveChangesAsync();

        // Assert
        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithDetachedEntity_ShouldRemoveEntity()
    {
        // Arrange
        var user = new User { Login = "user", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Detach entity (симуляция получения из другого контекста)
        _context.Entry(user).State = EntityState.Detached;
        var detachedUser = new User { Id = user.Id, Login = "user", PasswordHash = "hash", Role = UserRole.Curator };

        // Act
        await _repository.DeleteAsync(detachedUser);
        await _repository.SaveChangesAsync();

        // Assert
        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnNumberOfChanges()
    {
        // Arrange
        _context.Users.Add(new User { Login = "user1", PasswordHash = "hash1", Role = UserRole.Curator });
        _context.Users.Add(new User { Login = "user2", PasswordHash = "hash2", Role = UserRole.Admin });

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoChanges_ShouldReturnZero()
    {
        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Repository_ShouldWorkWithDifferentEntityTypes()
    {
        // Arrange
        var contactRepository = new Repository<Contact>(_context);
        var contact = new Contact
        {
            ContactId = "TEST-001",
            BlockId = 1,
            FullNameEncrypted = "encrypted_name",
            ResponsibleCuratorId = 1
        };

        // Act
        var result = await contactRepository.AddAsync(contact);
        await contactRepository.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        var savedContact = await _context.Contacts.FindAsync(contact.Id);
        savedContact.Should().NotBeNull();
        savedContact!.ContactId.Should().Be("TEST-001");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

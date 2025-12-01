using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;

namespace Kurator.Core.Tests.Entities;

public class FAQTests
{
    [Fact]
    public void FAQ_ShouldInitializeWithDefaultValues()
    {
        // Act
        var faq = new FAQ();

        // Assert
        faq.Id.Should().Be(0);
        faq.Title.Should().Be(string.Empty);
        faq.Content.Should().Be(string.Empty);
        faq.SortOrder.Should().Be(0);
        faq.IsActive.Should().BeTrue();
        faq.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        faq.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        faq.UpdatedBy.Should().BeNull();
        faq.UpdatedByUser.Should().BeNull();
    }

    [Fact]
    public void FAQ_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var faq = new FAQ
        {
            Id = 1,
            Title = "Как создать контакт?",
            Content = "Для создания контакта перейдите в раздел 'Контакты' и нажмите кнопку 'Создать'.",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            UpdatedBy = 5
        };

        // Assert
        faq.Id.Should().Be(1);
        faq.Title.Should().Be("Как создать контакт?");
        faq.Content.Should().Contain("создания контакта");
        faq.SortOrder.Should().Be(1);
        faq.IsActive.Should().BeTrue();
        faq.CreatedAt.Should().Be(createdAt);
        faq.UpdatedAt.Should().Be(updatedAt);
        faq.UpdatedBy.Should().Be(5);
    }

    [Fact]
    public void FAQ_Title_ShouldSupportCyrillicCharacters()
    {
        // Arrange & Act
        var faq = new FAQ
        {
            Title = "Как работать с блоками?",
            Content = "Блоки представляют собой организационные единицы."
        };

        // Assert
        faq.Title.Should().Be("Как работать с блоками?");
        faq.Content.Should().Contain("организационные");
    }

    [Fact]
    public void FAQ_Content_ShouldSupportRichText()
    {
        // Arrange
        var richTextContent = @"
# Заголовок первого уровня
## Заголовок второго уровня

**Жирный текст** и *курсив*

- Пункт 1
- Пункт 2
- Пункт 3

```code
var example = 'test';
```";

        // Act
        var faq = new FAQ
        {
            Title = "Markdown Support",
            Content = richTextContent
        };

        // Assert
        faq.Content.Should().Contain("# Заголовок");
        faq.Content.Should().Contain("**Жирный текст**");
        faq.Content.Should().Contain("- Пункт");
        faq.Content.Should().Contain("```code");
    }

    [Fact]
    public void FAQ_Content_ShouldSupportLongText()
    {
        // Arrange
        var longContent = new string('A', 5000);

        // Act
        var faq = new FAQ
        {
            Title = "Long FAQ",
            Content = longContent
        };

        // Assert
        faq.Content.Should().HaveLength(5000);
    }

    [Fact]
    public void FAQ_SortOrder_ShouldAllowOrdering()
    {
        // Arrange
        var faqs = new List<FAQ>
        {
            new FAQ { Title = "Third", SortOrder = 3 },
            new FAQ { Title = "First", SortOrder = 1 },
            new FAQ { Title = "Second", SortOrder = 2 }
        };

        // Act
        var sorted = faqs.OrderBy(f => f.SortOrder).ToList();

        // Assert
        sorted[0].Title.Should().Be("First");
        sorted[1].Title.Should().Be("Second");
        sorted[2].Title.Should().Be("Third");
    }

    [Fact]
    public void FAQ_IsActive_ShouldSupportSoftDelete()
    {
        // Arrange
        var faq = new FAQ
        {
            Title = "Old FAQ",
            Content = "Outdated content",
            IsActive = true
        };

        // Act - Deactivate
        faq.IsActive = false;
        faq.UpdatedAt = DateTime.UtcNow;

        // Assert
        faq.IsActive.Should().BeFalse();
        faq.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FAQ_UpdatedBy_CanBeNull()
    {
        // Arrange & Act
        var faq = new FAQ
        {
            Title = "Test FAQ",
            Content = "Test content",
            UpdatedBy = null
        };

        // Assert
        faq.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void FAQ_CreatedAt_ShouldNotChangeOnUpdate()
    {
        // Arrange
        var originalCreatedAt = DateTime.UtcNow.AddDays(-30);
        var faq = new FAQ
        {
            Title = "Original Title",
            Content = "Original content",
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };

        // Act - Simulate update
        faq.Title = "Updated Title";
        faq.Content = "Updated content";
        faq.UpdatedAt = DateTime.UtcNow;
        faq.UpdatedBy = 1;

        // Assert
        faq.CreatedAt.Should().Be(originalCreatedAt);
        faq.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        faq.Title.Should().Be("Updated Title");
    }

    [Fact]
    public void FAQ_ShouldSupportMultipleFAQsWithSameSortOrder()
    {
        // Arrange & Act
        var faq1 = new FAQ { Title = "FAQ 1", SortOrder = 1 };
        var faq2 = new FAQ { Title = "FAQ 2", SortOrder = 1 };

        // Assert
        faq1.SortOrder.Should().Be(faq2.SortOrder);
    }

    [Fact]
    public void FAQ_ShouldSupportZeroSortOrder()
    {
        // Arrange & Act
        var faq = new FAQ
        {
            Title = "First FAQ",
            SortOrder = 0
        };

        // Assert
        faq.SortOrder.Should().Be(0);
    }

    [Fact]
    public void FAQ_ShouldSupportNegativeSortOrder()
    {
        // Arrange & Act
        var faq = new FAQ
        {
            Title = "Prepend FAQ",
            SortOrder = -1
        };

        // Assert
        faq.SortOrder.Should().Be(-1);
    }

    [Fact]
    public void FAQ_UpdatedByUser_NavigationProperty_ShouldBeNullable()
    {
        // Arrange & Act
        var faq = new FAQ
        {
            Title = "Test FAQ",
            Content = "Content",
            UpdatedBy = 1,
            UpdatedByUser = null
        };

        // Assert
        faq.UpdatedByUser.Should().BeNull();
    }

    [Fact]
    public void FAQ_Content_ShouldSupportHtmlContent()
    {
        // Arrange
        var htmlContent = @"
<div class='faq-section'>
  <h1>Заголовок</h1>
  <p>Параграф с <strong>жирным</strong> и <em>курсивом</em></p>
  <ul>
    <li>Элемент списка 1</li>
    <li>Элемент списка 2</li>
  </ul>
</div>";

        // Act
        var faq = new FAQ
        {
            Title = "HTML FAQ",
            Content = htmlContent
        };

        // Assert
        faq.Content.Should().Contain("<div");
        faq.Content.Should().Contain("<h1>Заголовок</h1>");
        faq.Content.Should().Contain("<strong>жирным</strong>");
    }

    [Theory]
    [InlineData("Как создать контакт?")]
    [InlineData("Как редактировать блок?")]
    [InlineData("Как добавить взаимодействие?")]
    [InlineData("Что такое Watchlist?")]
    public void FAQ_Title_ShouldAcceptVariousQuestions(string title)
    {
        // Arrange & Act
        var faq = new FAQ
        {
            Title = title,
            Content = "Answer content"
        };

        // Assert
        faq.Title.Should().Be(title);
    }

    [Fact]
    public void FAQ_ShouldTrackUpdateHistory()
    {
        // Arrange
        var faq = new FAQ
        {
            Title = "Test FAQ",
            Content = "Original content",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedBy = 1
        };

        // Act - First update
        faq.Content = "Updated content v1";
        faq.UpdatedAt = DateTime.UtcNow.AddDays(-5);
        faq.UpdatedBy = 2;

        // Second update
        var secondUpdateTime = DateTime.UtcNow;
        faq.Content = "Updated content v2";
        faq.UpdatedAt = secondUpdateTime;
        faq.UpdatedBy = 3;

        // Assert
        faq.Content.Should().Be("Updated content v2");
        faq.UpdatedAt.Should().Be(secondUpdateTime);
        faq.UpdatedBy.Should().Be(3);
    }

    [Fact]
    public void FAQ_ShouldBeVisibleWhenActive()
    {
        // Arrange
        var activeFaq = new FAQ { IsActive = true, Title = "Active" };
        var inactiveFaq = new FAQ { IsActive = false, Title = "Inactive" };

        var faqs = new List<FAQ> { activeFaq, inactiveFaq };

        // Act
        var visibleFaqs = faqs.Where(f => f.IsActive).ToList();

        // Assert
        visibleFaqs.Should().HaveCount(1);
        visibleFaqs.First().Title.Should().Be("Active");
    }
}

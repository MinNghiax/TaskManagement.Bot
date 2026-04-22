using TaskManagement.Bot.Application.Services.Reminders;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using Xunit;

namespace TaskManagement.Bot.Tests;

public class ReviewAutoCompletePolicyTests
{
    [Theory]
    [InlineData("30p")]
    [InlineData("30m")]
    [InlineData("1h")]
    public void GetDueAtUtc_ReturnsReviewStartedAtPlusConfiguredDelay(string autoCompleteAfter)
    {
        var reviewStartedAt = new DateTime(2026, 4, 21, 1, 0, 0, DateTimeKind.Utc);
        var task = new TaskItem
        {
            Title = "Review task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            Status = ETaskStatus.Review,
            ReviewStartedAt = reviewStartedAt
        };

        var dueAt = ReviewAutoCompletePolicy.GetDueAtUtc(task, autoCompleteAfter);

        Assert.NotNull(dueAt);
        Assert.True(dueAt > reviewStartedAt);
    }

    [Fact]
    public void GetDueAtUtc_ReturnsNullWhenTaskIsNotInReview()
    {
        var task = new TaskItem
        {
            Title = "Doing task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            Status = ETaskStatus.Doing,
            ReviewStartedAt = DateTime.UtcNow.AddHours(-1)
        };

        var dueAt = ReviewAutoCompletePolicy.GetDueAtUtc(task, "30p");

        Assert.Null(dueAt);
    }
}

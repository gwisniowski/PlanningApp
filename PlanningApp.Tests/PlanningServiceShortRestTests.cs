using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanningApp.Models;
using PlanningApp.Services;

namespace PlanningApp.Tests;

[TestClass]
public class PlanningServiceShortRestTests
{
    private static async Task AddAssignment(
        TestDbContextFactory factory, int employeeId, DateTime date, int shift)
    {
        await using var db = factory.CreateDbContext();
        db.ScheduleAssignments.Add(new ScheduleAssignment
        {
            EmployeeId = employeeId,
            WorkDate = date,
            Shift = shift,
            ProductionLineId = 1,
            PositionOnLine = "A"
        });
        await db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Morning_shift_after_yesterdays_afternoon_is_short_rest()
    {
        // Arrange
        var factory = new TestDbContextFactory();
        var service = new PlanningService(factory);
        await AddAssignment(factory, employeeId: 1, new DateTime(2026, 11, 3), shift: 2);

        // Act
        var result = await service.HasShortRestAsync(1, new DateTime(2026, 11, 4), shift: 1);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Afternoon_shift_before_tomorrows_morning_is_short_rest()
    {
        var factory = new TestDbContextFactory();
        var service = new PlanningService(factory);
        await AddAssignment(factory, 1, new DateTime(2026, 11, 4), shift: 1);

        var result = await service.HasShortRestAsync(1, new DateTime(2026, 11, 3), shift: 2);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Same_shift_on_consecutive_days_is_not_short_rest()
    {
        var factory = new TestDbContextFactory();
        var service = new PlanningService(factory);
        await AddAssignment(factory, 1, new DateTime(2026, 11, 3), shift: 1);

        var result = await service.HasShortRestAsync(1, new DateTime(2026, 11, 4), shift: 1);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Other_employees_assignment_does_not_count()
    {
        var factory = new TestDbContextFactory();
        var service = new PlanningService(factory);
        await AddAssignment(factory, employeeId: 2, new DateTime(2026, 11, 3), shift: 2);

        var result = await service.HasShortRestAsync(1, new DateTime(2026, 11, 4), shift: 1);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Short_rest_is_detected_across_week_boundary()
    {
        // niedziela 1.11.2026 (zmiana 2) -> poniedziałek 2.11.2026 (zmiana 1)
        var factory = new TestDbContextFactory();
        var service = new PlanningService(factory);
        await AddAssignment(factory, 1, new DateTime(2026, 11, 1), shift: 2);

        var result = await service.HasShortRestAsync(1, new DateTime(2026, 11, 2), shift: 1);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task No_assignments_means_no_short_rest()
    {
        var factory = new TestDbContextFactory();
        var service = new PlanningService(factory);

        var result = await service.HasShortRestAsync(1, new DateTime(2026, 11, 4), shift: 1);

        Assert.IsFalse(result);
    }
}
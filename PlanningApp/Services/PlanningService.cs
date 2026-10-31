using Microsoft.EntityFrameworkCore;
using PlanningApp.Data;
using PlanningApp.Models;
using PlanningApp.Interfaces;

namespace PlanningApp.Services
{
    public enum AssignResult
    {
        Success,
        AlreadyAssignedThatDay,
        ShortRest
    }
    public class PlanningService : IPlanningService
    {
        private readonly IDbContextFactory<PlanningDbContext> _contextFactory;
        public PlanningService(IDbContextFactory<PlanningDbContext> context)
        {
            _contextFactory = context;
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
        public async Task<List<Models.ScheduleAssignment>> GetWeekPlanningAsync(DateTime anyDayInWeek) 
        {

            DateTime startOfWeek = GetStartOfWeek(anyDayInWeek);
            DateTime endOfWeek = startOfWeek.AddDays(7).Date;

            await using var context = _contextFactory.CreateDbContext();
            return await context.ScheduleAssignments
                .Include(x => x.Employee)
                .Include(x => x.ProductionLine)
                .Where(e => e.WorkDate >= startOfWeek && e.WorkDate < endOfWeek)
                .OrderBy(x => x.WorkDate)
                .ThenBy(x => x.Shift)
                .ThenBy(x => x.ProductionLineId)
                .ThenBy(x => x.PositionOnLine)
                .ToListAsync ();
        }

        //Przypisanie pracownika z imienia i nazwiska do konkretnej komórki w planingu
        public async Task<AssignResult> AssignEmployeeAsync(DateTime workDate, int shift, int productionLineId, string position, 
            int? employeeId, bool ignoreShortRest = false) 
        {
            var start = workDate.Date;  

            if (employeeId.HasValue) 
            {
                if (!await CanAssignEmployeeAsync(employeeId.Value, start, shift))
                    return AssignResult.AlreadyAssignedThatDay;

                if (!ignoreShortRest && await HasShortRestAsync(employeeId.Value, start, shift))
                    return AssignResult.ShortRest;
            }

            await using var context = _contextFactory.CreateDbContext();
            var end = start.AddDays(1);
            var assignment = await context.ScheduleAssignments
                .FirstOrDefaultAsync(x => x.WorkDate >= start && x.WorkDate < end &&
                x.Shift == shift &&
                x.ProductionLineId == productionLineId &&
                x.PositionOnLine == position);

            if (assignment == null) 
            {
                var newAssignment = new Models.ScheduleAssignment
                {
                    WorkDate = start,
                    Shift = shift,
                    ProductionLineId = productionLineId,
                    PositionOnLine = position,
                    EmployeeId = employeeId
                };

                context.ScheduleAssignments.Add(newAssignment);
            }
            else 
            { 
                assignment.EmployeeId = employeeId; 
            }
            await context.SaveChangesAsync();
            return AssignResult.Success;
        }

        //Walidacja przed przypisaniem pracownika
        public async Task<bool> CanAssignEmployeeAsync(int employeeId, DateTime workDate, int shift)
        {
            await using var context = _contextFactory.CreateDbContext();

            var start = workDate.Date;
            var end = start.AddDays(1);

            bool alreadyAssigned = await context.ScheduleAssignments.AnyAsync(x =>
            x.EmployeeId == employeeId &&
            x.WorkDate >= start &&
            x.WorkDate < end &&
            x.Shift != shift);

            return !alreadyAssigned;
        }

        //Walidacja krotkiej przerwy pracownika przed przypisaniem
        public async Task<bool> HasShortRestAsync(int employeeId, DateTime workDate, int shift) 
        {
            await using var context = _contextFactory.CreateDbContext();

            var targetDate = workDate.Date;

            if (shift == 1)
            {
                var yesterdayStart = targetDate.AddDays(-1);
                var yesterdayEnd = targetDate;

                return await context.ScheduleAssignments.AnyAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.WorkDate >= yesterdayStart &&
                    x.WorkDate < yesterdayEnd &&
                    x.Shift == 2);
            }
            else if (shift == 2)
            {
                var tomorrowStart = targetDate.AddDays(1);
                var tomorrowEnd = tomorrowStart.AddDays(1);

                return await context.ScheduleAssignments.AnyAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.WorkDate >= tomorrowStart &&
                    x.WorkDate < tomorrowEnd &&
                    x.Shift == 1);
            }
            return false;
        }

        public async Task UnassignEmployeeAsync(DateTime workDate, int shift, int productionLineId, string position) 
        {
            var start = workDate.Date;
            var end = start.AddDays(1);

            await using var context = _contextFactory.CreateDbContext();

            var assignment = await context.ScheduleAssignments
            .FirstOrDefaultAsync(x => x.WorkDate >= start && x.WorkDate < end &&
                x.Shift == shift &&
                x.ProductionLineId == productionLineId &&
                x.PositionOnLine == position);

            if (assignment != null) 
            {
                context.ScheduleAssignments.Remove(assignment);
                await context.SaveChangesAsync();
            }
        }

        //Funkcja kopiująca harmonogra na kolejny tydzien
        public async Task<bool> CopyToNextWeekAsync(DateTime anyDayInWeek)
        {
            var assignments = await GetWeekPlanningAsync(anyDayInWeek);
            await using var context = _contextFactory.CreateDbContext();

            DateTime startOfWeek = GetStartOfWeek(anyDayInWeek);
            DateTime nextWeekStart = startOfWeek.AddDays(7);
            DateTime nextWeekEnd = nextWeekStart.AddDays(7);

            bool exists = await context.ScheduleAssignments.AnyAsync(x =>
                x.WorkDate >= nextWeekStart &&
                x.WorkDate < nextWeekEnd);

            if (exists)
                return false;

            foreach (var assignment in assignments)
            {
                context.ScheduleAssignments.Add(new ScheduleAssignment
                {
                    WorkDate = assignment.WorkDate.AddDays(7),
                    Shift = assignment.Shift,
                    ProductionLineId = assignment.ProductionLineId,
                    PositionOnLine = assignment.PositionOnLine,
                    EmployeeId = assignment.EmployeeId
                });
            }

            await context.SaveChangesAsync();
            return true;
        }

        //Funkcja czyszcząca cały harmonogram z bieżącego tygodnia
        public async Task CleanAllSchedule(DateTime anyDayInWeek, int? productionLineId = null, int? shift = null) 
        {
            DateTime startOfWeek = GetStartOfWeek(anyDayInWeek);
            DateTime endOfWeek = startOfWeek.AddDays(7);

            await using var context = _contextFactory.CreateDbContext();

            var query = context.ScheduleAssignments.Where(x => x.WorkDate >= startOfWeek && x.WorkDate < endOfWeek);

             if (productionLineId.HasValue)
                query = query.Where(x => x.ProductionLineId == productionLineId.Value);

            if (shift.HasValue)
                query = query.Where(x => x.Shift == shift.Value);

            var items = await query.ToListAsync();

            context.ScheduleAssignments.RemoveRange(items);
            await context.SaveChangesAsync();
        }
    }
}

using PlanningApp.Services;

namespace PlanningApp.Interfaces
{
    public interface IPlanningService
    {
        Task<List<Models.ScheduleAssignment>> GetWeekPlanningAsync(DateTime anyDayInWeek);
        Task<AssignResult> AssignEmployeeAsync(DateTime workDate, int shift, int productionLineId, string position, int? employeeId, bool ignoreShortRest = false);
        Task<bool> CanAssignEmployeeAsync(int employeeId, DateTime workDate, int shift);
        Task UnassignEmployeeAsync(DateTime workDate, int shift, int productionLineId, string position);
        Task<bool> CopyToNextWeekAsync(DateTime anyDayInWeek);

    }
}

using PlanningApp.Models;

namespace PlanningApp.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllAsync();
        Task AddAsync(Employee employee);
        Task DeactivateAsync(Employee employee);
        Task EditAsync(Employee employee);
    }
}

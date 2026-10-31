using Microsoft.EntityFrameworkCore;
using PlanningApp.Data;
using PlanningApp.Models;
using PlanningApp.Interfaces;

namespace PlanningApp.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IDbContextFactory <PlanningDbContext> _contextFactory;

        public EmployeeService(IDbContextFactory<PlanningDbContext> context) 
        {
            _contextFactory = context;
        }

        public async Task<List<Employee>> GetAllAsync() 
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.Employees.OrderBy(e => e.LastName).ToListAsync();
        }

        public async Task AddAsync(Employee employee) 
        {
            await using var context = _contextFactory.CreateDbContext();
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
        }

        public async Task DeactivateAsync(Employee employee) 
        {
            await using var context = _contextFactory.CreateDbContext();
            var employeToDelete = await context.Employees.FirstOrDefaultAsync(x => x.Id == employee.Id);
            if (employeToDelete == null) { return; }
            employeToDelete.IsActive = false;
            await context.SaveChangesAsync();
        }

        public async Task EditAsync(Employee employee)
        {
            await using var context = _contextFactory.CreateDbContext();
            context.Employees.Update(employee);
            await context.SaveChangesAsync();
        }
    }
}

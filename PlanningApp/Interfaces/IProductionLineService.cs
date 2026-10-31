using PlanningApp.Models;

namespace PlanningApp.Interfaces
{
    public interface IProductionLineService
    {
        Task<List<ProductionLine>> GetAllAsync();
        Task AddAsync(ProductionLine productionLine);
        Task DeactivateAsync(ProductionLine productionLine);
        Task EditAsync(ProductionLine productionLine);

    }
}

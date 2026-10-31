using Microsoft.EntityFrameworkCore;
using PlanningApp.Data;
using PlanningApp.Models;
using PlanningApp.Interfaces;

namespace PlanningApp.Services
{
    public class ProductionLineService : IProductionLineService
    {
        private readonly IDbContextFactory<PlanningDbContext> _contextFactory;

        public ProductionLineService(IDbContextFactory<PlanningDbContext> context)
        {
            _contextFactory = context;
        }

        public async Task<List<ProductionLine>> GetAllAsync()
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.ProductionLines.OrderBy(e => e.Name).ToListAsync();
        }
        public async Task AddAsync(ProductionLine productionLine)
        {
            await using var context = _contextFactory.CreateDbContext();
            context.ProductionLines.Add(productionLine);
            await context.SaveChangesAsync();
        }
        public async Task DeactivateAsync(ProductionLine productionLine)
        {
            await using var context = _contextFactory.CreateDbContext();
            var lineToDelete = await context.ProductionLines.FirstOrDefaultAsync(x => x.Id == productionLine.Id);
            if (lineToDelete == null) { return; }
            lineToDelete.IsActive = false;
            await context.SaveChangesAsync();
        }
        public async Task EditAsync(ProductionLine productionLine)
        {
            await using var context = _contextFactory.CreateDbContext();
            context.ProductionLines.Update(productionLine);
            await context.SaveChangesAsync();
        }
    }
}

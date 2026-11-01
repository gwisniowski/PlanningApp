using Microsoft.EntityFrameworkCore;
using PlanningApp.Data;

namespace PlanningApp.Tests
{
    public class TestDbContextFactory : IDbContextFactory<PlanningDbContext>
    {
        private readonly DbContextOptions<PlanningDbContext> _options;

        public TestDbContextFactory()
        {
            _options = new DbContextOptionsBuilder<PlanningDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())  
                .Options;
        }

        public PlanningDbContext CreateDbContext() => new PlanningDbContext(_options);
    }
}

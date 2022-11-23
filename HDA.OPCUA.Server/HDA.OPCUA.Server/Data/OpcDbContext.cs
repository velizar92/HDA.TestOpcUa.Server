using HDA.OPCUA.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace HDA.OPCUA.Server.Data
{
    public class OpcDbContext : DbContext
    {

        public OpcDbContext()
        {

        }

        public OpcDbContext(DbContextOptions<OpcDbContext> options)
          : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured == false)
            {
                optionsBuilder.UseSqlServer(Configuration.ConnectionString);
            }
        }

        public DbSet<History> History { get; set; }
        public DbSet<ModifiedHistory> ModifiedHistory { get; set; }

    }
}

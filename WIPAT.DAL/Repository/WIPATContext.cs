using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;

namespace WIPAT.DAL
{
    public class WIPATContext : DbContext
    {
        public WIPATContext() : base("name=dbContext") { Database.SetInitializer(new MigrateDatabaseToLatestVersion<WIPATContext, Migrations.Configuration>()); }
        //---------------------

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // This disables "Cascade Delete" globally.
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            base.OnModelCreating(modelBuilder);
        }
        //---------------------
        public DbSet<ItemCatalogue> ItemCatalogues { get; set; }
        public DbSet<InitialStock> InitialStocks { get; set; }
        public DbSet<ActualOrder> ActualOrders { get; set; }
        public DbSet<Miscellaneous> Miscellaneous { get; set; }

        public DbSet<ForecastMaster> ForecastMasters { get; set; }
        public DbSet<ForecastDetail> ForecastDetails { get; set; }

        public DbSet<WipMaster> WipMasters { get; set; }
        public DbSet<WipDetail> WipDetails { get; set; }

        public DbSet<User> Users { get; set; }

    }
}

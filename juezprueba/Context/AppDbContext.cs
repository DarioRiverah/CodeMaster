using juezprueba.Models;
using Microsoft.EntityFrameworkCore;

namespace juezprueba.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Problema> Problemas { get; set; }
        public DbSet<CasoDePrueba> CasosDePrueba { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Problema>()
                .HasMany(p => p.CasosDePrueba)
                .WithOne(c => c.Problema)
                .HasForeignKey(c => c.ProblemaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

﻿using juezprueba.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace juezprueba.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Problema> Problemas { get; set; }
        public DbSet<CasoDePrueba> CasosDePrueba { get; set; }
        public DbSet<PerfilUsuario> PerfilesUsuario { get; set; }
        public DbSet<ProblemaResuelto> ProblemasResueltos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // 👈 MUY IMPORTANTE para que Identity funcione

            modelBuilder.Entity<Problema>()
                .HasMany(p => p.CasosDePrueba)
                .WithOne(c => c.Problema)
                .HasForeignKey(c => c.ProblemaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProblemaResuelto>()
            .HasOne(pr => pr.Usuario)
            .WithMany()  // Si quieres, luego puedes agregar colección en ApplicationUser
            .HasForeignKey(pr => pr.UsuarioId);

            modelBuilder.Entity<ProblemaResuelto>()
                .HasOne(pr => pr.Problema)
                .WithMany()
                .HasForeignKey(pr => pr.ProblemaId);
        }
    }
}

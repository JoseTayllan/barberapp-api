using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Barbeiro> Barbeiros => Set<Barbeiro>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Servico> Servicos => Set<Servico>();
        public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
        public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // importante — inicializa tabelas do Identity

            // Barbeiro
            modelBuilder.Entity<Barbeiro>(e =>
            {
                e.HasKey(b => b.Id);
                e.Property(b => b.Nome).IsRequired().HasMaxLength(100);
                e.Property(b => b.Telefone).IsRequired().HasMaxLength(20);
            });
            // Servico
            modelBuilder.Entity<Servico>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Nome).IsRequired().HasMaxLength(100);
                e.Property(s => s.Preco).HasColumnType("decimal(10,2)");
            });
            // Cliente
            modelBuilder.Entity<Cliente>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Nome).IsRequired().HasMaxLength(100);
                e.Property(c => c.Email).IsRequired().HasMaxLength(150);
                e.HasIndex(c => c.Email).IsUnique();

            });

            // Agendamento
            modelBuilder.Entity<Agendamento>(e =>
            {
                e.HasKey(a => a.Id);

                e.HasOne(a => a.Cliente)
                   .WithMany()
                   .HasForeignKey(a => a.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Barbeiro)
                    .WithMany()
                    .HasForeignKey(a => a.BarbeiroId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Servico)
                    .WithMany()
                    .HasForeignKey(a => a.ServicoId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.Property(a => a.Status)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<Pagamento>(e =>
            {
                e.HasKey(p => p.Id);

                e.HasOne(p => p.Agendamento)
                    .WithOne()
                    .HasForeignKey<Pagamento>(p => p.AgendamentoId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(p => p.Valor)
                .HasColumnType("numeric(10,2)");
                e.Property(p => p.Status)
                    .HasConversion<string>();
            });
        }

    }
}
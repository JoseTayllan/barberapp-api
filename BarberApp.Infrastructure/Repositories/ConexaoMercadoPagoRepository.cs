using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories;

public sealed class ConexaoMercadoPagoRepository(AppDbContext context)
    : IConexaoMercadoPagoRepository, IConsultaConexaoMercadoPagoRepository
{
    public Task<MetadadosConexaoMercadoPago?> ConsultarAsync(CancellationToken cancellationToken) =>
        context.ConexoesMercadoPago.AsNoTracking()
            .Select(conexao => new MetadadosConexaoMercadoPago(conexao.ContaId, conexao.ExpiraEm, conexao.Sandbox))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SalvarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken)
    {
        var atual = await context.ConexoesMercadoPago.SingleOrDefaultAsync(cancellationToken);
        if (atual is null)
            await context.ConexoesMercadoPago.AddAsync(conexao, cancellationToken);
        else
            atual.Atualizar(conexao);
        await context.SaveChangesAsync(cancellationToken);
    }
}

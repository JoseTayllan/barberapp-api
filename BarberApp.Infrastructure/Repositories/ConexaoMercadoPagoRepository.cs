using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories;

public sealed class ConexaoMercadoPagoRepository(AppDbContext context)
    : IConexaoMercadoPagoRepository, IConsultaConexaoMercadoPagoRepository, ICredenciaisConexaoMercadoPagoRepository,
        IPendenciaConexaoMercadoPagoRepository, IRemocaoConexaoMercadoPagoRepository
{
    public async Task RemoverAsync(CancellationToken cancellationToken)
    {
        var atual = await context.ConexoesMercadoPago.SingleOrDefaultAsync(cancellationToken);
        if (atual is null) return;
        context.ConexoesMercadoPago.Remove(atual);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RegistrarPendenciaAsync(CancellationToken cancellationToken)
    {
        var atual = await context.ConexoesMercadoPago.SingleOrDefaultAsync(cancellationToken);
        if (atual is null || atual.RequerReautorizacao) return false;
        atual.RegistrarPendenciaRenovacao();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<ConexaoMercadoPago?> ObterCredenciaisAsync(CancellationToken cancellationToken) =>
        context.ConexoesMercadoPago.AsNoTracking().SingleOrDefaultAsync(cancellationToken);

    public Task<MetadadosConexaoMercadoPago?> ConsultarAsync(CancellationToken cancellationToken) =>
        context.ConexoesMercadoPago.AsNoTracking()
            .Select(conexao => new MetadadosConexaoMercadoPago(
                conexao.ContaId, conexao.ExpiraEm, conexao.Sandbox, conexao.RequerReautorizacao))
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

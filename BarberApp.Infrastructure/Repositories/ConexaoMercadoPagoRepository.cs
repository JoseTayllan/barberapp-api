using BarberApp.Domain.Entities;
using BarberApp.Domain.Interfaces;
using BarberApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberApp.Infrastructure.Repositories;

public sealed class ConexaoMercadoPagoRepository(AppDbContext context) : IConexaoMercadoPagoRepository
{
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

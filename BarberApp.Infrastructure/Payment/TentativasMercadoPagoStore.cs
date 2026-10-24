using Microsoft.Extensions.Caching.Memory;

namespace BarberApp.Infrastructure.Payment;

public sealed class TentativasMercadoPagoStore(IMemoryCache cache, TimeProvider timeProvider)
{
    private readonly object _lock = new();
    private Guid _geracao = Guid.NewGuid();

    internal void InvalidarTodas()
    {
        lock (_lock)
        {
            // Invalida logicamente sem enumerar cache compartilhado; entradas antigas expiram pelo TTL.
            _geracao = Guid.NewGuid();
        }
    }

    private string ChaveAdministrador(string administradorId) => $"mercado-pago:oauth:{_geracao:N}:{administradorId}";
    private string ChaveState(string state) => $"mercado-pago:state:{_geracao:N}:{state}";

    internal void Salvar(TentativaMercadoPago tentativa)
    {
        lock (_lock)
        {
            var key = ChaveAdministrador(tentativa.AdministradorId);
            if (cache.TryGetValue<TentativaMercadoPago>(key, out var anterior) && anterior is not null)
                cache.Remove(ChaveState(anterior.State));
            cache.Set(key, tentativa, tentativa.ExpiraEm);
            cache.Set(ChaveState(tentativa.State), tentativa, tentativa.ExpiraEm);
        }
    }

    internal TentativaMercadoPago? Consultar(string state)
    {
        lock (_lock)
        {
            return cache.TryGetValue<TentativaMercadoPago>(ChaveState(state), out var tentativa)
                && tentativa is not null && tentativa.ExpiraEm > timeProvider.GetUtcNow() ? tentativa : null;
        }
    }

    internal TentativaMercadoPago? Consumir(string state)
    {
        lock (_lock)
        {
            var stateKey = ChaveState(state);
            if (!cache.TryGetValue<TentativaMercadoPago>(stateKey, out var tentativa) || tentativa is null)
                return null;
            cache.Remove(stateKey);
            cache.Remove(ChaveAdministrador(tentativa.AdministradorId));
            return tentativa.ExpiraEm > timeProvider.GetUtcNow() ? tentativa : null;
        }
    }
}

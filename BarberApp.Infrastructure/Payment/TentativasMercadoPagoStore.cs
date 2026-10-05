using Microsoft.Extensions.Caching.Memory;

namespace BarberApp.Infrastructure.Payment;

public sealed class TentativasMercadoPagoStore(IMemoryCache cache, TimeProvider timeProvider)
{
    private readonly object _lock = new();

    internal void Salvar(TentativaMercadoPago tentativa)
    {
        lock (_lock)
        {
            var key = $"mercado-pago:oauth:{tentativa.AdministradorId}";
            if (cache.TryGetValue<TentativaMercadoPago>(key, out var anterior) && anterior is not null)
                cache.Remove($"mercado-pago:state:{anterior.State}");
            cache.Set(key, tentativa, tentativa.ExpiraEm);
            cache.Set($"mercado-pago:state:{tentativa.State}", tentativa, tentativa.ExpiraEm);
        }
    }

    internal TentativaMercadoPago? Consumir(string state)
    {
        lock (_lock)
        {
            var stateKey = $"mercado-pago:state:{state}";
            if (!cache.TryGetValue<TentativaMercadoPago>(stateKey, out var tentativa) || tentativa is null)
                return null;
            cache.Remove(stateKey);
            cache.Remove($"mercado-pago:oauth:{tentativa.AdministradorId}");
            return tentativa.ExpiraEm > timeProvider.GetUtcNow() ? tentativa : null;
        }
    }
}

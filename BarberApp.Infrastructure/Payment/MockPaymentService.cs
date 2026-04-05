using BarberApp.Domain.Interfaces;

namespace BarberApp.Infrastructure.Payment;

public class MockPaymentService : IPaymentService
{
    public Task<PaymentResult> ProcessarPagamentoAsync(PaymentRequest request)
    {
        // Simula aprovação com ID falso
        var resultado = new PaymentResult(
            Sucesso: true,
            TransacaoId: $"MOCK-{Guid.NewGuid()}",
            MensagemErro: null
        );

        return Task.FromResult(resultado);
    }

    public Task<PaymentResult> ReembolsarAsync(string transacaoId)
    {
        var resultado = new PaymentResult( 
            Sucesso: true,
            TransacaoId: transacaoId,
            MensagemErro: null
        );

        return Task.FromResult(resultado);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberApp.Domain.Interfaces
{
    public record PaymentRequest(
        Guid AgendamentoId,
        decimal Valor,
        string DescricaoServico,
        string EmailCliente
    );
    public record PaymentResult(
        bool Sucesso,
        string? TransacaoId,
        string? MensagemErro
    );

    public interface IPaymentService
    {
        Task<PaymentResult> ProcessarPagamentoAsync(PaymentRequest request);
        Task<PaymentResult> ReembolsarAsync(string transacaoId);
    }


}
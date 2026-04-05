using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Application.DTOs;
using FluentValidation;

namespace BarberApp.Application.Validators
{
    public class CriarAgendamentoValidator : AbstractValidator<CriarAgendamentoRequest>
{
    public CriarAgendamentoValidator()
    {
        RuleFor(x => x.BarbeiroId)
            .NotEmpty().WithMessage("Barbeiro é obrigatório.");

        RuleFor(x => x.ServicoId)
            .NotEmpty().WithMessage("Serviço é obrigatório.");

        RuleFor(x => x.DataHora)
            .GreaterThan(DateTime.UtcNow).WithMessage("A data do agendamento deve ser futura.");
    }
}
}
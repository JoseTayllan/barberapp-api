using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Application.DTOs;
using FluentValidation;

namespace BarberApp.Application.Validators
{
    public class AgendamentoValidator : AbstractValidator<CriarAgendamentoRequest>
    {
        public AgendamentoValidator()
        {
            RuleFor(x => x.ClienteId)
                .NotEmpty().WithMessage("ClienteId é obrigatório.");

            RuleFor(x => x.BarbeiroId)
                .NotEmpty().WithMessage("BarbeiroId é obrigatório.");

            RuleFor(x => x.ServicoId)
                .NotEmpty().WithMessage("ServicoId é obrigatório.");

            RuleFor(x => x.DataHora)
                .GreaterThan(DateTime.Now).WithMessage("Data e hora devem ser no futuro.");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Application.DTOs;
using FluentValidation;

namespace BarberApp.Application.Validators
{
    public class ServicoValidator : AbstractValidator<CriarServicoRequest>
    {
        public ServicoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Preco)
            .GreaterThan(0).WithMessage("Preço deve ser maior que zero.");

            RuleFor(x => x.DuracaoMinutos)
            .GreaterThan(0).WithMessage("Duração deve ser maior que zero.")
            .LessThanOrEqualTo(480).WithMessage("Duração não pode ultrapassar 8 horas.");
        }
        
    }
}
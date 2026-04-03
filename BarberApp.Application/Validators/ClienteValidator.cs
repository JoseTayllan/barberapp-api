using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberApp.Application.DTOs;
using FluentValidation;

namespace BarberApp.Application.Validators
{
    public class ClienteValidator : AbstractValidator<CriarClienteRequest>
    {
        public ClienteValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email é obrigatório.")
                .EmailAddress().WithMessage("Email deve ser um endereço de email válido.");

            RuleFor(x => x.Telefone)
                .NotEmpty().WithMessage("Telefone é obrigatório.")
                .Matches(@"^\d+$").WithMessage("Telefone deve conter apenas números.");
        }

    }
}
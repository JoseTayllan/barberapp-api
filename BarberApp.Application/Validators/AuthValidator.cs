using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BarberApp.Application.DTOs;
using FluentValidation;

namespace BarberApp.Application.Validators
{
    public class AuthValidator : AbstractValidator<RegistroRequest>
    {
        public AuthValidator()
        {
            RuleFor(x => x.NomeCompleto)
                .NotEmpty().WithMessage("Username é obrigatório.")
                .MaximumLength(50).WithMessage("Username deve ter no máximo 50 caracteres.");


            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email é obrigatório.")
                .EmailAddress().WithMessage("Email deve ser um endereço de email válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Senha é obrigatória.")
                .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres.");

            RuleFor(x => x.Telefone)
                .NotEmpty().WithMessage("Telefone é obrigatório.")
                .Matches(@"^\d+$").WithMessage("Telefone deve conter apenas números.");
        }
    }
}
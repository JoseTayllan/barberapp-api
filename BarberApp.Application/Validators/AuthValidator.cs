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
        public class AtualizarPerfilValidator : AbstractValidator<AtualizarPerfilRequest>
        {
            public AtualizarPerfilValidator()
            {
                RuleFor(x => x.NomeCompleto)
                    .NotEmpty().WithMessage("Nome completo é obrigatório.")
                    .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

                RuleFor(x => x.Telefone)
                    .NotEmpty().WithMessage("Telefone é obrigatório.")
                    .Matches(@"^\d+$").WithMessage("Telefone deve conter apenas números.");
            }
        }

        public class AlterarSenhaValidator : AbstractValidator<AlterarSenhaRequest>
        {
            public AlterarSenhaValidator()
            {
                RuleFor(x => x.SenhaAtual)
                    .NotEmpty().WithMessage("Senha atual é obrigatória.");

                RuleFor(x => x.NovaSenha)
                    .NotEmpty().WithMessage("Nova senha é obrigatória.")
                    .MinimumLength(6).WithMessage("Nova senha deve ter no mínimo 6 caracteres.")
                    .NotEqual(x => x.SenhaAtual).WithMessage("Nova senha deve ser diferente da atual.");
            }
        }
    }
}
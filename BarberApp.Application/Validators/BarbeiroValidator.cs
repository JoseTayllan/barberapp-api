using BarberApp.Application.DTOs;
using FluentValidation;

namespace BarberApp.Application.Validators;

public class CriarBarbeiroValidator : AbstractValidator<BarbeiroCreateRequest>
{
    public CriarBarbeiroValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Telefone)
            .NotEmpty().WithMessage("Telefone é obrigatório.")
            .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres.")
            .Matches(@"^\d+$").WithMessage("Telefone deve conter apenas números.");
    }
}
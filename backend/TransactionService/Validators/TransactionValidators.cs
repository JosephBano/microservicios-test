using FluentValidation;
using TransactionService.DTOs;
using TransactionService.Models;

namespace TransactionService.Validators;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo de transacción es obligatorio.")
            .Must(t => Enum.TryParse<TransactionType>(t, true, out _))
            .WithMessage("El tipo de transacción debe ser 'Purchase' o 'Sale'.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("El ID del producto es obligatorio.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario debe ser mayor o igual a 0.");
    }
}

public class UpdateTransactionValidator : AbstractValidator<UpdateTransactionDto>
{
    public UpdateTransactionValidator()
    {
        // Todos los campos son opcionales en la edición
    }
}

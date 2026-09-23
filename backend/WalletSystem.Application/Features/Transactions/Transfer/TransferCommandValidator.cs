using FluentValidation;

namespace WalletSystem.Application.Features.Transactions.Transfer;

public class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.FromWalletId)
            .NotEmpty().WithMessage("FromWalletId is required");

        RuleFor(x => x.ToWalletId)
            .NotEmpty().WithMessage("ToWalletId is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero");

        RuleFor(x => x.ToWalletId)
            .NotEqual(x => x.FromWalletId).WithMessage("FromWalletId and ToWalletId cannot be the same");
    }
}

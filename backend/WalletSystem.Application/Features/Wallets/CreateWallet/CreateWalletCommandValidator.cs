using FluentValidation;

namespace WalletSystem.Application.Features.Wallets.CreateWallet;

public class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Wallet name is required")
            .MaximumLength(100).WithMessage("Wallet name cannot exceed 100 characters");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code")
            .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be uppercase letters only");
    }
}

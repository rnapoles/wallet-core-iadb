using FluentValidation;

namespace WalletSystem.Application.Features.Wallets.GetWalletById;

public class GetWalletByIdQueryValidator : AbstractValidator<GetWalletByIdQuery>
{
    public GetWalletByIdQueryValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("Wallet ID is required");
    }
}

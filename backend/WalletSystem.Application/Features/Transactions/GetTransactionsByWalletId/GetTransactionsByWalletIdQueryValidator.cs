using FluentValidation;

namespace WalletSystem.Application.Features.Transactions.GetTransactionsByWalletId;

public class GetTransactionsByWalletIdQueryValidator : AbstractValidator<GetTransactionsByWalletIdQuery>
{
    public GetTransactionsByWalletIdQueryValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("Wallet ID is required");
    }
}

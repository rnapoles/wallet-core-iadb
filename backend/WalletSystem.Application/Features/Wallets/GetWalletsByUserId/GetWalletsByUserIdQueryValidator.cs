using FluentValidation;

namespace WalletSystem.Application.Features.Wallets.GetWalletsByUserId;

public class GetWalletsByUserIdQueryValidator : AbstractValidator<GetWalletsByUserIdQuery>
{
    public GetWalletsByUserIdQueryValidator()
    {
        // No parameters; auth check happens in handler via ICurrentUserService
    }
}

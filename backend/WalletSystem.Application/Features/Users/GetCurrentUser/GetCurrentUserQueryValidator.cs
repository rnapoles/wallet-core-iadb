using FluentValidation;

namespace WalletSystem.Application.Features.Users.GetCurrentUser;

public class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserQueryValidator()
    {
        // GetCurrentUserQuery has no parameters, but validator is registered for completeness
        // The authentication check happens in the handler via ICurrentUserService
    }
}

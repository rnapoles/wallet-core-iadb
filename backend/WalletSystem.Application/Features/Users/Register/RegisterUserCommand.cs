using MediatR;

namespace WalletSystem.Application.Features.Users.Register;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<RegisterUserResponse>;

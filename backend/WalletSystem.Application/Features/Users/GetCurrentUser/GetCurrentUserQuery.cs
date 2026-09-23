using MediatR;

namespace WalletSystem.Application.Features.Users.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<GetCurrentUserResponse>;

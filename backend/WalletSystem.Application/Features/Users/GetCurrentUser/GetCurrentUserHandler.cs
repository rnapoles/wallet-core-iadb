using MediatR;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Application.Features.Users.GetCurrentUser;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetCurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        return new GetCurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName
        );
    }
}

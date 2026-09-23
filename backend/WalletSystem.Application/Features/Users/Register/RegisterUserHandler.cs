using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Users;

namespace WalletSystem.Application.Features.Users.Register;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RegisterUserHandler> _logger;
    private readonly IIdGenerator _idGenerator;

    public RegisterUserHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IPublishEndpoint publishEndpoint,
        ILogger<RegisterUserHandler> logger,
        IIdGenerator idGenerator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = new User
        {
            Id = _idGenerator.CreateId(),
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userRegisteredEvent = new UserRegistered
        {
            UserId = user.Id,
            Email = user.Email,
            RegisteredAt = user.CreatedAt
        };

        await _publishEndpoint.Publish(userRegisteredEvent, cancellationToken);
        
        // Force save in outbox
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Published UserRegistered event for user {UserId}", user.Id);

        return new RegisterUserResponse(user.Id, user.Email, "User registered successfully");
    }
}


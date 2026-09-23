using System.Net.Http.Json;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Commands;

/// <summary>
/// Command to authenticate a user and obtain a JWT token
/// Applied Command Pattern
/// </summary>
public sealed class LoginCommand : WalletCommandBase<LoginResponse>
{
    private readonly string _email;
    private readonly string _password;

    public LoginCommand(HttpClient httpClient, string email, string password, IIdGenerator? idGenerator = null)
        : base(httpClient, null, idGenerator)
    {
        _email = email;
        _password = password;
    }

    public override async Task<LoginResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var loginRequest = new { Email = _email, Password = _password };
        var response = await HttpClient.PostAsJsonAsync("api/auth/login", loginRequest, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Login failed: {content}", null, response.StatusCode);
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Login response was null");

        return loginResponse;
    }
}

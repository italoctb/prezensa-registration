using Registration.Application.Accounts;

namespace Registration.Application.Abstractions.Persistence;

public interface IAccountSessionRepository
{
    Task CreateAsync(AccountSession session, CancellationToken cancellationToken);
    Task<AccountSession?> FindActiveAsync(string tokenHash, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task RevokeAsync(string tokenHash, CancellationToken cancellationToken);
}

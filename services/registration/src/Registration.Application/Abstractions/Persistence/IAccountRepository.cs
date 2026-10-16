using Registration.Application.Accounts;

namespace Registration.Application.Abstractions.Persistence;

public interface IAccountRepository
{
    Task<UserAccount?> FindByEmailAsync(CorporateEmail email, CancellationToken cancellationToken);
    Task<bool> TryCreateAsync(UserAccount account, CancellationToken cancellationToken);
}

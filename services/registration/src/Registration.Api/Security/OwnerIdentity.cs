using System.Security.Claims;
using Registration.Domain.Drafts;

namespace Registration.Api.Security;

public static class OwnerIdentity
{
    public static OwnerId From(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var value) || value == Guid.Empty)
            throw new OwnerClaimException();
        return OwnerId.From(value);
    }
}

public sealed class OwnerClaimException : Exception;

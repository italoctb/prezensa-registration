using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.UpdateAddress;

public sealed record UpdateAddressCommand(RegistrationDraftId DraftId, OwnerId OwnerId,
    string? PostalCode, string? Street, string? District, string? City,
    string? FederativeUnit, string? Number, string? Complement);

public sealed class UpdateAddressHandler(IRegistrationDraftRepository drafts, TimeProvider timeProvider)
{
    public async Task<DraftSnapshot> HandleAsync(UpdateAddressCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await DraftAccess.LoadOwnedAsync(drafts, command.DraftId, command.OwnerId, cancellationToken);
        var expectedVersion = draft.Version;
        var address = Address.Create(
            PostalCode.Create(command.PostalCode), Street.Create(command.Street),
            District.Create(command.District), City.Create(command.City),
            FederativeUnit.Create(command.FederativeUnit), AddressNumber.Create(command.Number),
            command.Complement is null ? null : AddressComplement.Create(command.Complement));
        draft.SetAddress(address, timeProvider.GetUtcNow());
        await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
        return DraftSnapshot.From(draft);
    }
}

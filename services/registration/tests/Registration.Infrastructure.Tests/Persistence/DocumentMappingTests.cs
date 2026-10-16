using Registration.Infrastructure.Persistence.Mongo;
using Registration.Infrastructure.Persistence.Oracle;
using Registration.Infrastructure.Tests.Support;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;

namespace Registration.Infrastructure.Tests.Persistence;

public sealed class DocumentMappingTests
{
    [Fact]
    public void Oracle_applicant_json_round_trip_preserves_values()
    {
        var applicant = Sample.Applicant();

        var restored = ApplicantJsonMapper.ToDomain(ApplicantJsonMapper.ToJson(applicant));

        Assert.Equal(applicant, restored);
    }

    [Fact]
    public void Mongo_document_round_trip_preserves_complete_draft_without_replaying_events()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);

        var restored = MongoDraftDocumentMapper.ToDomain(MongoDraftDocumentMapper.ToDocument(draft));

        Assert.Equal(draft.Id, restored.Id);
        Assert.Equal(draft.OwnerId, restored.OwnerId);
        Assert.Equal(draft.Version, restored.Version);
        Assert.Equal(draft.ToApplicantProfile(DateOnly.FromDateTime(Sample.T1.UtcDateTime)),
            restored.ToApplicantProfile(DateOnly.FromDateTime(Sample.T1.UtcDateTime)));
        Assert.Empty(restored.DomainEvents);
    }

    [Fact]
    public void Mongo_document_round_trip_preserves_submitted_link()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var registrationId = RegistrationId.From(Guid.NewGuid());
        draft.MarkSubmitted(registrationId, Sample.T2);

        var restored = MongoDraftDocumentMapper.ToDomain(MongoDraftDocumentMapper.ToDocument(draft));

        Assert.Equal(RegistrationDraftStatus.Submitted, restored.Status);
        Assert.Equal(registrationId, restored.RegistrationId);
        Assert.Equal(Sample.T2, restored.SubmittedAt);
        Assert.Empty(restored.DomainEvents);
    }
}

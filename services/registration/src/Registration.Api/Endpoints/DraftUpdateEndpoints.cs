using Microsoft.AspNetCore.Routing;
using Registration.Api.Contracts;
using Registration.Api.Security;
using Registration.Application.Drafts.UpdateAddress;
using Registration.Application.Drafts.UpdateBirthDate;
using Registration.Application.Drafts.UpdateDocument;
using Registration.Application.Drafts.UpdateEmail;
using Registration.Application.Drafts.UpdateFullName;
using Registration.Application.Drafts.UpdateMonthlyIncome;
using Registration.Application.Drafts.UpdateMotherName;
using Registration.Application.Drafts.UpdateNationality;
using Registration.Application.Drafts.UpdatePhone;
using Registration.Application.Drafts.UpdateProfession;
using Registration.Domain.Drafts;
using Registration.Domain;

namespace Registration.Api.Endpoints;

public static class DraftUpdateEndpoints
{
    public static RouteGroupBuilder MapDraftUpdateEndpoints(this RouteGroupBuilder drafts)
    {
        drafts.MapPut("/{draftId:guid}/full-name", UpdateFullName);
        drafts.MapPut("/{draftId:guid}/document", UpdateDocument);
        drafts.MapPut("/{draftId:guid}/birth-date", UpdateBirthDate);
        drafts.MapPut("/{draftId:guid}/email", UpdateEmail);
        drafts.MapPut("/{draftId:guid}/phone", UpdatePhone);
        drafts.MapPut("/{draftId:guid}/mother-name", UpdateMotherName);
        drafts.MapPut("/{draftId:guid}/nationality", UpdateNationality);
        drafts.MapPut("/{draftId:guid}/address", UpdateAddress);
        drafts.MapPut("/{draftId:guid}/profession", UpdateProfession);
        drafts.MapPut("/{draftId:guid}/monthly-income", UpdateMonthlyIncome);
        return drafts;
    }

    private static async Task<IResult> UpdateFullName(Guid draftId, UpdateFullNameRequest request,
        HttpContext context, UpdateFullNameHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.FullName), cancellationToken)));

    private static async Task<IResult> UpdateDocument(Guid draftId, UpdateDocumentRequest request,
        HttpContext context, UpdateDocumentHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.Document), cancellationToken)));

    private static async Task<IResult> UpdateBirthDate(Guid draftId, UpdateBirthDateRequest request,
        HttpContext context, UpdateBirthDateHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.BirthDate), cancellationToken)));

    private static async Task<IResult> UpdateEmail(Guid draftId, UpdateEmailRequest request,
        HttpContext context, UpdateEmailHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.Email), cancellationToken)));

    private static async Task<IResult> UpdatePhone(Guid draftId, UpdatePhoneRequest request,
        HttpContext context, UpdatePhoneHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.Phone), cancellationToken)));

    private static async Task<IResult> UpdateMotherName(Guid draftId, UpdateMotherNameRequest request,
        HttpContext context, UpdateMotherNameHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.MotherName), cancellationToken)));

    private static async Task<IResult> UpdateNationality(Guid draftId, UpdateNationalityRequest request,
        HttpContext context, UpdateNationalityHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.Nationality), cancellationToken)));

    private static async Task<IResult> UpdateAddress(Guid draftId, UpdateAddressRequest request,
        HttpContext context, UpdateAddressHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.PostalCode, request.Street,
            request.District, request.City, request.FederativeUnit, request.Number,
            request.Complement), cancellationToken)));

    private static async Task<IResult> UpdateProfession(Guid draftId, UpdateProfessionRequest request,
        HttpContext context, UpdateProfessionHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(DraftResponse.From(await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.Profession), cancellationToken)));

    private static async Task<IResult> UpdateMonthlyIncome(Guid draftId, UpdateMonthlyIncomeRequest request,
        HttpContext context, UpdateMonthlyIncomeHandler handler, CancellationToken cancellationToken)
    {
        if (request.MonthlyIncome is null)
            throw new DomainException(DomainErrorCodes.InvalidMonthlyIncome, "Monthly income is required.");
        var result = await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User), request.MonthlyIncome.Value), cancellationToken);
        return Results.Ok(DraftResponse.From(result));
    }
}

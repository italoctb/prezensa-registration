using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Registration.Api.Errors;
using Registration.Api.Security;
using Registration.Application;
using Registration.Application.Accounts;
using Registration.Domain;

namespace Registration.Api.Tests;

public sealed class ApiBoundaryTests
{
    [Fact]
    public void Owner_comes_from_the_authenticated_subject_claim()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString("D"))], "Test"));

        Assert.Equal(id, OwnerIdentity.From(principal).Value);
    }

    [Fact]
    public void Missing_or_non_guid_subject_cannot_become_an_owner()
    {
        var missing = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test"));
        var invalid = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "someone")], "Test"));

        Assert.Throws<OwnerClaimException>(() => OwnerIdentity.From(missing));
        Assert.Throws<OwnerClaimException>(() => OwnerIdentity.From(invalid));
    }

    [Fact]
    public async Task Domain_error_becomes_a_bad_request_with_stable_code()
    {
        var (status, code) = await Handle(new DomainException(DomainErrorCodes.InvalidName, "Invalid name."));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal(DomainErrorCodes.InvalidName, code);
    }

    [Fact]
    public async Task Duplicate_cpf_becomes_a_conflict_with_stable_code()
    {
        var (status, code) = await Handle(new RegistrationApplicationException(
            ApplicationErrorCodes.CpfAlreadyRegistered, "CPF is already registered."));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal(ApplicationErrorCodes.CpfAlreadyRegistered, code);
    }

    [Theory]
    [InlineData(AccountErrorCodes.InvalidCorporateEmail, StatusCodes.Status400BadRequest)]
    [InlineData(AccountErrorCodes.InvalidCode, StatusCodes.Status401Unauthorized)]
    [InlineData(AccountErrorCodes.AccountAlreadyExists, StatusCodes.Status409Conflict)]
    public async Task Account_errors_have_stable_http_status(string errorCode, int expectedStatus)
    {
        var (status, code) = await Handle(new RegistrationApplicationException(errorCode, "Account error."));
        Assert.Equal(expectedStatus, status);
        Assert.Equal(errorCode, code);
    }

    private static async Task<(int Status, string? Code)> Handle(Exception exception)
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddProblemDetails();
        using var services = registrations.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var handled = await new RegistrationExceptionHandler().TryHandleAsync(context, exception, CancellationToken.None);
        Assert.True(handled);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body);
        return (context.Response.StatusCode, json.RootElement.GetProperty("code").GetString());
    }
}

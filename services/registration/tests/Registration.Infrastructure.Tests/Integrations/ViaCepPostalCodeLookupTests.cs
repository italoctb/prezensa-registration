using System.Net;
using Registration.Domain.Applicants.Addresses;
using Registration.Infrastructure.Integrations;

namespace Registration.Infrastructure.Tests.Integrations;

public sealed class ViaCepPostalCodeLookupTests
{
    [Fact]
    public async Task Maps_a_complete_response_to_local_value_objects()
    {
        var handler = new StubHandler("""
            {"logradouro":"Praça da Sé","bairro":"Sé","localidade":"São Paulo","uf":"SP"}
            """);
        var lookup = Create(handler);

        var result = await lookup.LookupAsync(PostalCode.Create("01001-000"), CancellationToken.None);

        Assert.Equal("ws/01001000/json/", handler.LastPath);
        Assert.NotNull(result);
        Assert.Equal("01001000", result.PostalCode.Digits);
        Assert.Equal("Praça da Sé", result.Street.Value);
        Assert.Equal("Sé", result.District.Value);
        Assert.Equal("São Paulo", result.City.Value);
        Assert.Equal("SP", result.FederativeUnit.Code);
    }

    [Theory]
    [InlineData("{\"erro\":true}")]
    [InlineData("{\"logradouro\":\"\",\"bairro\":\"Centro\",\"localidade\":\"São Paulo\",\"uf\":\"SP\"}")]
    public async Task Missing_or_incomplete_provider_result_returns_null(string body)
    {
        var result = await Create(new StubHandler(body)).LookupAsync(PostalCode.Create("01001000"), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Provider_http_failure_is_not_misreported_as_not_found()
    {
        var lookup = Create(new StubHandler("{}", HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            lookup.LookupAsync(PostalCode.Create("01001000"), CancellationToken.None));
    }

    private static ViaCepPostalCodeLookup Create(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br/") });

    private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? LastPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastPath = request.RequestUri?.PathAndQuery.TrimStart('/');
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}

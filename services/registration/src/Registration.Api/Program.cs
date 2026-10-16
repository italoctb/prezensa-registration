using Registration.Api;
using Registration.Api.Endpoints;
using Registration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRegistrationApi(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapAccountEndpoints();
app.MapDraftEndpoints();
app.MapPostalCodeEndpoints();
app.MapSubmitDraftEndpoints();

app.Run();

public partial class Program;

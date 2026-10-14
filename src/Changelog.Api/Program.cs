using Changelog.Api.Auth;
using Changelog.Api.Data;
using Changelog.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(o => o.Secret.Length >= 32, "Jwt:Secret must be configured and at least 32 characters long.")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddSingleton<UpdateRepository>();
builder.Services.AddSingleton<UpdatePointRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        // Keep "sub" as "sub" so ClaimsPrincipalExtensions.GetUserId can find it.
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = jwt.Value.SigningKey(),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        bearer.Events = new JwtBearerEvents
        {
            // Same body as the Express `protect` middleware.
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new MessageResponse { Message = "user not authorized" });
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    var db = app.Services.GetRequiredService<SqlConnectionFactory>();
    await DatabaseInitializer.InitializeAsync(db.ConnectionString, app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler(error => error.Run(context =>
        context.Response.WriteAsJsonAsync(new MessageResponse { Message = "internal server error" })));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Lets the integration tests reference the entry point via WebApplicationFactory<Program>.
public partial class Program;

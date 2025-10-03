using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using Tannit.AuthServer.Config;
using Tannit.AuthServer.Repositories;
using Tannit.AuthServer.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURATION DE LA CONNEXION SQL (Dapper)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<IDbConnection>(_ =>
{
    var connection = new SqlConnection(connectionString);
    connection.Open(); // Connexion ouverte pour chaque requête HTTP
    return connection;
});

// ===== DEPENDENCY INJECTION : Services & Repos
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
builder.Services.AddHostedService<ExpiredCodeCleanupService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// ===== CONTROLLERS & SWAGGER
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===== AUTHENTICATION (token maison + Google)

/*
 //token émis par IdentityServer
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:7111"; // IdentityServer
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
*/

//token maison
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT] {ctx.Exception.GetType().Name}: {ctx.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google"; // endpoint où Google redirige
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = ctx =>
            {
                // "select_account" => force l’écran de choix de compte
                // "consent"        => force l’écran de consentement
                var separator = ctx.RedirectUri.Contains('?') ? "&" : "?";
                ctx.Response.Redirect($"{ctx.RedirectUri}{separator}prompt=select_account");
                return Task.CompletedTask;
            }
        };
    });

// ===== IDENTITY SERVER (Duende)
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // Active explicitement le support du flow "password"
    options.InputLengthRestrictions.Password = 128;
})
.AddInMemoryClients(IdentityServerConfig.Clients)
.AddInMemoryApiScopes(IdentityServerConfig.ApiScopes)
.AddInMemoryIdentityResources(IdentityServerConfig.IdentityResources)
.AddDeveloperSigningCredential(); // Ne pas utiliser en prod (remplacer par une clé persistante)

// ===== Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Authorization");
    });
});

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseRouting();
app.UseIdentityServer(); // Nécessaire pour IdentityServer

app.UseCors("CorsPolicy");

app.UseAuthentication(); // Nécessaire pour JWT & Google OAuth
app.UseAuthorization();

app.MapDefaultControllerRoute(); // mappe automatiquement les controllers
app.MapControllers();

app.Run();

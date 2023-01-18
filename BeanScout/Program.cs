using BeanScout.Services;
using BeanScout.Data;
using BeanScout.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Configuration;
using BeanScout.JwtFeatures;
using BeanScout.Services.EmailService;
using Microsoft.OpenApi.Models;
using BeanScout.CustomTokenProviders;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger =>
// To Enable authorization using Swagger (JWT)    
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
    })
);
builder.Services.AddDbContext<BeanScoutContext>();
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    //options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.Tokens.EmailConfirmationTokenProvider = "emailconfirmation";
}
    )
    .AddEntityFrameworkStores<BeanScoutContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<EmailConfirmationTokenProvider<IdentityUser>>("emailconfirmation");
builder.Services.Configure<EmailConfirmationTokenProviderOptions>(opt =>
     opt.TokenLifespan = TimeSpan.FromDays(3));
builder.Services.AddIdentityServer()
    .AddInMemoryCaching()
    .AddClientStore<InMemoryClientStore>()
    .AddResourceStore<InMemoryResourcesStore>()
    .AddAspNetIdentity<IdentityUser>();
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var googleAuthSettings = builder.Configuration.GetSection("Google");
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["validIssuer"],
        ValidAudience = jwtSettings["validAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
            .GetBytes(jwtSettings.GetSection("securityKey").Value))
    };
})
.AddCookie()
.AddGoogle("google", googleOptions =>
{
    googleOptions.ClientId = googleAuthSettings.GetSection("ClientId").Value;
    googleOptions.ClientSecret = googleAuthSettings.GetSection("ClientSecret").Value;
    googleOptions.SaveTokens = true;
});
//builder.Services.AddSqlite<ReviewContext>("Data Source=BeanScout.db");
var emailConfig = builder.Configuration
     .GetSection("EmailSettings")
     .Get<EmailConfiguration>();
builder.Services.AddSingleton(emailConfig);
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<JwtHandler>();
builder.Services.AddScoped<EmailSender>();

builder.Services.AddRazorPages();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Adding this mapping stuff to enable deeplinks to iOS
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?tabs=aspnetcore2x&view=aspnetcore-7.0#fileextensioncontenttypeprovider
// Set up custom content types - associating file extension to MIME type
var provider = new FileExtensionContentTypeProvider();
// Add new mappings
provider.Mappings["Unknown"] = "application/json";
provider.Mappings[".htm3"] = "text/html";
provider.Mappings[".image"] = "image/png";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
// End section for iOS deeplinks

app.UseIdentityServer();
app.UseAuthentication();
app.UseRouting();
app.MapControllers();
app.UseAuthorization();
app.MapRazorPages();

app.Run();


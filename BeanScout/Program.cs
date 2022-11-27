using BeanScout.Services;
using BeanScout.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Duende.IdentityServer.Stores;

var builder = WebApplication.CreateBuilder(args);
var dbConnectionString = builder.Configuration["BeanScout:ConnectionString"];

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<BeanScoutContext>();
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<BeanScoutContext>()
                .AddDefaultTokenProviders();
builder.Services.AddIdentityServer()
    .AddInMemoryCaching()
    .AddClientStore<InMemoryClientStore>()
    .AddResourceStore<InMemoryResourcesStore>()
    .AddAspNetIdentity<IdentityUser>();
builder.Services.AddAuthentication()
.AddIdentityServerJwt();
//builder.Services.AddSqlite<ReviewContext>("Data Source=BeanScout.db");
builder.Services.AddScoped<ReviewService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseIdentityServer();
app.UseAuthorization();

app.MapControllers();

app.Run();


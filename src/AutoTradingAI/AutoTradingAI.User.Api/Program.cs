using AutoTradingAI.User.Core.Interfaces;
using AutoTradingAI.User.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using System.Text;
using Azure.Messaging.ServiceBus;
using AutoTradingAI.User.Infrastructure.Models;
using Microsoft.Extensions.Options;
using AutoTradingAI.Logging;

var builder = WebApplication.CreateBuilder(args);

// configure logging

// add services to the container
Logger.ConfigureLogging(builder.Configuration);

// configure logging
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// register mongodb client
builder.Services.Configure<MongoSettings>(s =>
    new MongoClient(builder.Configuration.GetSection("MongoSettings:ConnectionString").Value));

builder.Services.AddScoped<IMongoClient>(s =>
    (IMongoClient)s.GetRequiredService<IMongoClient>().GetDatabase(
        builder.Configuration.GetSection("MongoSettings:DatabaseName").Value));

// register auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// register jwt service
builder.Services.AddSingleton<ServiceBusClient>(s =>
    new ServiceBusClient(builder.Configuration.GetSection("ServiceBus:ConnectionString").Value));

// register service bus client
builder.Services.AddSingleton<ServiceBusClient>(s =>
    new ServiceBusClient(builder.Configuration["ServiceBus:ConnectionString"]));

// configure jwt authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"]);
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

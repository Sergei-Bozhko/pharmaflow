using PharmaFlow.Api.Common.Idempotency;
using PharmaFlow.Api.Endpoints;
using PharmaFlow.Application;
using PharmaFlow.Application.Common.Idempotency;
using PharmaFlow.Infrastructure;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("v1");
builder.Services.AddPharmaFlowApplication();
builder.Services.AddPharmaFlowInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdempotencyKeyProvider, HttpIdempotencyKeyProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.MapHealthEndpoints();

app.Run();

public partial class Program;
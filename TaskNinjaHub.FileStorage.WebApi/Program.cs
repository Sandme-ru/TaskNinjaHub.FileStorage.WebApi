using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using Minio.AspNetCore;
using Newtonsoft.Json;
using TaskNinjaHub.FileStorage.WebApi.Subdomain;

var builder = WebApplication.CreateBuilder(args);

var endpoint = "https://minio-server.sandme.ru:9000";

var accessKey = "minio";
var secretKey = "miniostorage";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
#if RELEASE
    options.DocumentFilter<SubdomainRouteAttribute>();
#endif
});

builder.Services.AddMinio(options =>
{
    options.Endpoint = endpoint;
    options.ConfigureClient(client =>
    {
        client.WithSSL();
    });
});

// Url based configuration
builder.Services.AddMinio(new Uri("s3://minio:miniostorage@minio-server.sandme.ru:9000/region"));

// Create new from factory

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
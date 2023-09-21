using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("{**route}", (HttpContext context,  [FromRoute] string route) =>
{
    string output = "";
    foreach (var header in context.Request.Headers)
        output += (output == "" ? "" : "\r\n") + 
            header.Key + ": " + header.Value;

    output += $"\r\n\r\nLocalIpAddress: {context.Connection.LocalIpAddress}";
    output += $"\r\n\r\nRemoteIpAddress: {context.Connection.RemoteIpAddress}";
    output += $"\r\n\r\nroute: {route}";
    return output;
})
.WithName("GetAll")
.WithOpenApi();

app.MapPost("{**route}", (HttpContext context, [FromRoute] string route, [FromBody] JsonObject body) =>
{
    string output = "";
    foreach (var header in context.Request.Headers)
        output += (output == "" ? "" : "\r\n") + 
            header.Key + ": " + header.Value;

    output += $"\r\n\r\nLocalIpAddress: {context.Connection.LocalIpAddress}";
    output += $"\r\n\r\nRemoteIpAddress: {context.Connection.RemoteIpAddress}";
    output += $"\r\n\r\nroute: {route}";
    output += $"\r\n\r\nbody: {body.ToJsonString()}";
    return output;
})
.WithName("PostAll")
.WithOpenApi();

app.Run();
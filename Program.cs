using ai_scans_reader.BackgroundServices;
using ai_scans_reader.Common;
using ai_scans_reader.Controllers;
using ai_scans_reader.Models;

const string version = "0.1";

Console.WriteLine($"Start reader, version: {version}");
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ScansOptions>(builder.Configuration.GetSection("Scans"));
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

builder.Services.AddSingleton<ScansContainer>();

builder.Services.AddSingleton<ScansInfo>();

builder.Services.AddSingleton<JsonResultService>();

builder.Services.AddHttpClient();

builder.Services.AddHostedService<ScansFolderReader>();
builder.Services.AddHostedService<OllamaService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
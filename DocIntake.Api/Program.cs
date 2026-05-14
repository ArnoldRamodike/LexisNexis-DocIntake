using DocIntake.Api.Endpoints;
using DocIntake.Api.Services;
using DocIntake.Api.Services.IService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services
builder.Services.AddSingleton<IMetadataStore, InMemoryMetadataStore>();
builder.Services.AddSingleton<IQueueService, InMemoryQueueService>();
builder.Services.AddSingleton<IBlobStorageService, InMemoryBlobStorageService>();
builder.Services.AddHostedService<DocumentProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDocumentEndpoints();

app.Run();
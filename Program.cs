using ExcelReplacement.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowElectronApp",
        builder => builder
            .WithOrigins("file://")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Register services
builder.Services.AddSingleton<ExcelService>();
builder.Services.AddSingleton<WordService>();
builder.Services.AddSingleton<CsvService>();
builder.Services.AddSingleton<PreviewService>();
builder.Services.AddSingleton<FileProcessingController>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowElectronApp");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
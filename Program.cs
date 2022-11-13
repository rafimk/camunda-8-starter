using Cloudstarter.Services;
using NLog.Web;

var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IZeebeService, ZeebeService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var zeebeService = app.Services.GetRequiredService<IZeebeService>();
zeebeService.Deploy("test-process.bpmn");
zeebeService.StartWorkers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

 

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

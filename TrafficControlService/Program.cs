using Microsoft.Extensions.Options;
using TrafficControlService.Actors;
using TrafficControlService.DomainServices;
using TrafficControlService.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ISpeedingViolationCalculator>(new DefaultSpeedingViolationCalculator("A12",10,100,5));
builder.Services.AddSingleton<IVehicleStateRepository, DaprVehicleStateRepository>();
// Add services to the container.
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3601";
var daprGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "60001";
builder.Services.AddDaprClient(builder => builder
    .UseHttpEndpoint($"http://localhost:{daprHttpPort}")
    .UseGrpcEndpoint($"http://localhost:{daprGrpcPort}"));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddActors(Options => {
    Options.Actors.RegisterActor<VehicleActor>();
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCloudEvents();
app.UseAuthorization();

app.MapControllers();
app.MapActorsHandlers();
app.MapSubscribeHandler();
app.Run();


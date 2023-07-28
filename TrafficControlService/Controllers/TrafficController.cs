using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Mvc;
using TrafficControlService.Actors;
using TrafficControlService.Events;

namespace TrafficControlService.Controllers;

[ApiController]
[Route("[controller]")]
public class TrafficController : ControllerBase
{

    private readonly ILogger<TrafficController> _logger;

    public TrafficController(ILogger<TrafficController> logger)
    {
        _logger = logger;
    }

    [Topic("pubsub", "cam-entry")]
    [HttpPost("cam-entry")]
    public async Task<ActionResult> HandleCamEntry(VehicleRegistered vehicleRegistered)
    {
        try
        {
            var actorId = new ActorId(vehicleRegistered.LicenseNumber);
            var proxy = ActorProxy.Create<IVehicleActor>(actorId, nameof(VehicleActor));
            await proxy.RegisterEntryAsync(vehicleRegistered);
            return Ok();
        }
        catch
        {
            return StatusCode(500);
        }
    }

    [Topic("pubsub", "cam-exit")]
    [HttpPost("cam-exit")]
    public async Task<ActionResult> HandleCamExit(VehicleRegistered vehicleRegistered)
    {
              try
        {
            var actorId = new ActorId(vehicleRegistered.LicenseNumber);
            var proxy = ActorProxy.Create<IVehicleActor>(actorId, nameof(VehicleActor));
            await proxy.RegisterExitAsync(vehicleRegistered);
            return Ok();
        }
        catch
        {
            return StatusCode(500);
        }
    }
}


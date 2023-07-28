using CameraSimulation.Events;
using Dapr.Client;
namespace CameraSimulation.Proxies
{
    public class TrafficControlService : ITrafficControlService
    {
        private readonly DaprClient _daprClient;
        private readonly int _camNumber;

        public TrafficControlService(DaprClient rdapClient, int camNumber)
        {

            _daprClient = rdapClient;
            _camNumber = camNumber;

        }

        public async Task SendVehicleEntryAsync(VehicleRegistered vehicleRegistered)
        {
            await _daprClient.PublishEventAsync("pubsub", "cam-entry", vehicleRegistered);
        }

        public async Task SendVehicleExitAsync(VehicleRegistered vehicleRegistered)
        {
            await _daprClient.PublishEventAsync("pubsub", "cam-exit", vehicleRegistered);
        }
    }
}
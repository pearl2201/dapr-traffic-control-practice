using CameraSimulation.Proxies;
using Dapr.Client;

int lanes = 3;
Simulation.CameraSimulation[] cameras = new Simulation.CameraSimulation[lanes];
var client = new DaprClientBuilder().Build();
for(var i = 0; i < lanes; i++)
{
    int camNumber = i + 1;
    var trafficControlService= new TrafficControlService(client,camNumber);
    cameras[i] = new Simulation.CameraSimulation(camNumber, trafficControlService);

}

Parallel.ForEach(cameras,camera => camera.Start());

Task.Run(() => Thread.Sleep(Timeout.Infinite)).Wait();
using System;
using Dapr.Client;
using TrafficControlService.Models;

namespace TrafficControlService.Repositories
{
	public class DaprVehicleStateRepository : IVehicleStateRepository
	{
		private readonly DaprClient _daprClient;
		public DaprVehicleStateRepository(DaprClient daprClient)
		{
			_daprClient = daprClient;
		}

		public async Task<VehicleState?> GetVehicleStateAsync(string licenseNumber)
		{
			return await _daprClient.GetStateAsync<VehicleState>("traffic", licenseNumber);
		}

		public async Task SaveVehicleStateAsync(VehicleState vehicleState)
		{
			await _daprClient.SaveStateAsync("traffic", vehicleState.LicenseNumber, vehicleState);
		}
	}
}


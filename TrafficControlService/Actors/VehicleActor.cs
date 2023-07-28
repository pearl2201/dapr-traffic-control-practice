using System;
using System.Numerics;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;
using TrafficControlService.DomainServices;
using TrafficControlService.Events;
using TrafficControlService.Models;

namespace TrafficControlService.Actors
{
	public class VehicleActor : Actor, IVehicleActor, IRemindable

	{
		public readonly ISpeedingViolationCalculator _speedingViolationCalculator;
		private readonly string _roadId;
		private readonly DaprClient _daprClient;

		public VehicleActor(ActorHost host, DaprClient daprClient, ISpeedingViolationCalculator speedingViolationCalculator) : base(host)
		{
			_daprClient = daprClient;
			_speedingViolationCalculator = speedingViolationCalculator;
			_roadId = _speedingViolationCalculator.GetRoadId();
		}

		public  async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
		{
			if (reminderName == "VehicleLost")
			{
				await UnregisterReminderAsync("VehicleLost");
				var vehicleState = await this.StateManager.GetStateAsync<VehicleState>("VehicleState");
				Logger.LogInformation($"Lost track of vehicle with license-number {vehicleState.LicenseNumber}");
			}
		}

		public async Task RegisterEntryAsync(VehicleRegistered msg)
		{
			try
			{
				Logger.LogInformation($"ENTRY detected in lane {msg.Lane} at {msg.Timestamp.ToString("hh:mm:ss")} of vehicle with license-number {msg.LicenseNumber}");
				var vehicleState = new VehicleState(msg.LicenseNumber, msg.Timestamp, null);
				await this.StateManager.SetStateAsync("VehicleState", vehicleState);
				await RegisterReminderAsync("VehicleLost", null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error in RegisterEntry");
			}
		}

		public async Task RegisterExitAsync(VehicleRegistered msg)
		{
			try
			{
				Logger.LogInformation($"EXIT detected in lane {msg.Lane} at {msg.Timestamp.ToString("hh:mm:ss")} of vehicle with license-number {msg.LicenseNumber}");
				await UnregisterReminderAsync("VehicleLost");

				var vehicleState = await this.StateManager.GetStateAsync<VehicleState>("VehicleState");
				vehicleState = vehicleState with { ExitTimestamp = msg.Timestamp };
				await this.StateManager.SetStateAsync("VehicleState", vehicleState);

				int violation = _speedingViolationCalculator.DetermineSpeedingViolationInKmh(vehicleState.EntryTimestamp, vehicleState.ExitTimestamp.Value);
				if (violation > 0)
				{
					var speedingViolation = new SpeedingViolation
					{
						VehicleId = msg.LicenseNumber,
						RoadId = _roadId,
						ViolationInKmh = violation,
						Timestamp = msg.Timestamp
					};
					Logger.LogInformation($"{msg.LicenseNumber} violation");
					await _daprClient.PublishEventAsync("pubsub", "speedingviolations", speedingViolation);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error in RegisterExit");
			}
		}
	}
}


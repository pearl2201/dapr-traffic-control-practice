﻿using System;
namespace CameraSimulation.Events
{
    public record struct VehicleRegistered(int Lane, string LicenseNumber, DateTime Timestamp);
}


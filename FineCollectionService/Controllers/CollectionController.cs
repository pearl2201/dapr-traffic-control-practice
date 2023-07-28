using System.Text.Json;
using Dapr;
using Dapr.Client;
using FineCollectionService.DomainServices;
using FineCollectionService.Helpers;
using FineCollectionService.Models;
using FineCollectionService.Proxies;
using Microsoft.AspNetCore.Mvc;

namespace FineCollectionService.Controllers
{
    [ApiController]
    [Route("")]
    public class CollectionController : ControllerBase
    {
        private static string? _fineCalculatorLicenseKey = null;
        private readonly ILogger<CollectionController> _logger;
        private readonly IFineCalculator _fineCalculator;
        private readonly VehicleRegistrationService _vehicleRegistrationService;
        private readonly DaprClient _daprClient;
    

        public CollectionController(IFineCalculator fineCalculator, ILogger<CollectionController> logger, VehicleRegistrationService vehicleRegistrationService, DaprClient daprClient)
        {
            _daprClient = daprClient;
            _logger = logger;
            _fineCalculator = fineCalculator;
            _vehicleRegistrationService = vehicleRegistrationService;
            if (_fineCalculatorLicenseKey == null)
            {
                _logger.LogInformation("Init _fineCalculatorLicenseKey");
                string secretName = "finecalculator.licensekey";
                var metadata = new Dictionary<string, string> { { "namespace", "dapr-trafficcontrol" } };
                var secrets = daprClient.GetSecretAsync("trafficcontrol-secrets", secretName, metadata).Result;
                _fineCalculatorLicenseKey = secrets[secretName];
            }
        }

        [Topic("pubsub", "speedingviolations")]
        [HttpPost("collectionfine")]
        public async Task<ActionResult> CollectionFine(SpeedingViolation speedingViolation)
        {
            decimal fine = _fineCalculator.CalculateFine(_fineCalculatorLicenseKey!, speedingViolation.ViolationInKmh);

            var vehicleInfo = await _vehicleRegistrationService.GetVehicleInfo(speedingViolation.VehicleId);

            string fineString = fine == 0 ? "tbd by the prosecutor" : $"{fine} Euro";

            _logger.LogInformation($"Sent speeding ticket to {vehicleInfo.OwnerName}. Road: {speedingViolation.RoadId}, LicenseNumber: {speedingViolation.VehicleId}, Vehicle: {vehicleInfo.Brand} {vehicleInfo.Model}, Violation: {speedingViolation.ViolationInKmh} Km/h, Fine: {fineString}, on: {speedingViolation.Timestamp.ToString("dd-MM-yyyy")} at {speedingViolation.Timestamp.ToString("hh:mm:ss")}");

            var body = EmailUtils.CreateEmailBody(speedingViolation, vehicleInfo, fineString);
            var metadata = new Dictionary<string, string>
            {
                ["emailFrom"] = "noreply@cfca.gov",
                ["emailTo"] = vehicleInfo.OwnerEmail,
                ["subject"] = $"Speeding violation on the {speedingViolation.RoadId}"
            };
            await _daprClient.InvokeBindingAsync("sendmail", "create", body, metadata);
            return Ok();
        }

        [Topic("pubsub", "deadletters")]
        public ActionResult HandleDeadLetter(object message)
        {
            _logger.LogError("The service was not able to handle a collecfine message.");
            try
            {
                var messageJson = JsonSerializer.Serialize<object>(message);
                _logger.LogInformation($"Unhanlded message content:${messageJson}");
            }
            catch
            {
                _logger.LogError("Unhandled message's payload could not be deserianlized.");
            }
            return Ok();
        }
    }
}
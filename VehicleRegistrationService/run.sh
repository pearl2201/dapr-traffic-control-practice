dapr run --app-id vehicleregistrationservice --app-port 6004 --dapr-http-port 3604 --dapr-grpc-port 60004 --resources-path ../components/ -- dotnet run
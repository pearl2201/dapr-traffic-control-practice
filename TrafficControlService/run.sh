dapr run --app-id trafficcontrolservice --app-port 6001 --dapr-http-port 3601 --dapr-grpc-port 60001 --resources-path ../components/ -- dotnet run
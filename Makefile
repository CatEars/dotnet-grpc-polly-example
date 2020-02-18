build:
	dotnet build

lefty:
	JAEGER_ENDPOINT=http://localhost:14268/api/traces JAEGER_SERVICE_NAME=lefty ./bin/Debug/netcoreapp3.1/grpc-test --urls=http://localhost:5001

righty:
	JAEGER_ENDPOINT=http://localhost:14268/api/traces JAEGER_SERVICE_NAME=righty ./bin/Debug/netcoreapp3.1/grpc-test --urls=http://localhost:5002

pinger:
	TARGET_A=localhost:5001 TARGET_B=localhost:5002 ./bin/Debug/netcoreapp3.1/grpc-test ping

stats:
	python -m webbrowser -t "http://localhost:16686"

jaeger:
	docker-compose up jaeger

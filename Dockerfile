FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder

WORKDIR /app
COPY Properties Properties
COPY Protos Protos
COPY appsettings.Development.json appsettings.json \
     grpc-test.csproj Program.cs Startup.cs ./
COPY Services Services

RUN dotnet build

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

COPY --from=builder /app/bin /app/bin

CMD /app/bin/Debug/netcoreapp3.1/grpc-test --urls=http://127.0.0.1:5001
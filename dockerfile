# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY *.sln .
COPY ClinicBookingSystem/*.csproj ./ClinicBookingSystem/
RUN dotnet restore

COPY . .
WORKDIR /app/ClinicBookingSystem
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/ClinicBookingSystem/out ./
ENTRYPOINT ["dotnet", "ClinicBookingSystem.dll"]

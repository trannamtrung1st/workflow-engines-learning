# See https://aka.ms/containerfastmode to understand how Visual Studio uses this
# Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0.28-alpine3.19-arm64v8 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.203-alpine3.19-arm64v8 AS build
WORKDIR /src
COPY ["Samples/WELearning.Samples.DeviceService/WELearning.Samples.DeviceService.csproj", "Samples/WELearning.Samples.DeviceService/"]
RUN dotnet restore "Samples/WELearning.Samples.DeviceService/WELearning.Samples.DeviceService.csproj"
COPY . .
WORKDIR "/src/Samples/WELearning.Samples.DeviceService"
RUN dotnet build "WELearning.Samples.DeviceService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WELearning.Samples.DeviceService.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WELearning.Samples.DeviceService.dll"]
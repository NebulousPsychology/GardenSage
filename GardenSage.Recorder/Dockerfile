FROM mcr.microsoft.com/dotnet/sdk:8.0 AS workspace
WORKDIR /app

# copy csproj and restore as distinct layers
# COPY *.csproj .
# RUN dotnet restore

# copy and build app
COPY . .
RUN dotnet restore

FROM workspace AS build
RUN dotnet publish GardenSage.Recorder/ -c release -o out

# FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim-arm32v7
# FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-arm32v7 
#  'Microsoft.AspNetCore.App' is missing
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim-arm32v7 
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update -y 
RUN apt-get install -y curl
EXPOSE 22845 44398 
EXPOSE 7094 5123
EXPOSE 8080
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "GardenSage.Recorder.dll", "--environment", "Development"]

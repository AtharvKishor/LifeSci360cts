FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ProtocolService/ProtocolService.csproj", "ProtocolService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "ProtocolService/ProtocolService.csproj"
COPY ProtocolService/ ProtocolService/
COPY Shared/ Shared/
WORKDIR "/src/ProtocolService"
RUN dotnet publish "ProtocolService.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProtocolService.dll"]

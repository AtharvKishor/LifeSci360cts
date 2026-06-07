FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ReportingService/ReportingService.csproj", "ReportingService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "ReportingService/ReportingService.csproj"
COPY ReportingService/ ReportingService/
COPY Shared/ Shared/
WORKDIR "/src/ReportingService"
RUN dotnet publish "ReportingService.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReportingService.dll"]

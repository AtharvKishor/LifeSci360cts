FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AuditLogService.API/AuditLogService.API.csproj", "AuditLogService.API/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "AuditLogService.API/AuditLogService.API.csproj"
COPY AuditLogService.API/ AuditLogService.API/
COPY Shared/ Shared/
WORKDIR "/src/AuditLogService.API"
RUN dotnet publish "AuditLogService.API.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditLogService.API.dll"]

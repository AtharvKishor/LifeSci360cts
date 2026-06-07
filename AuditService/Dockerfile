FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AuditService/AuditService.csproj", "AuditService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "AuditService/AuditService.csproj"
COPY AuditService/ AuditService/
COPY Shared/ Shared/
WORKDIR "/src/AuditService"
RUN dotnet publish "AuditService.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditService.dll"]

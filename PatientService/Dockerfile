FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PatientService/PatientService.csproj", "PatientService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "PatientService/PatientService.csproj"
COPY PatientService/ PatientService/
COPY Shared/ Shared/
WORKDIR "/src/PatientService"
RUN dotnet publish "PatientService.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PatientService.dll"]

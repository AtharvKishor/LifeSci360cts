FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NotificationService/NotificationService.csproj", "NotificationService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "NotificationService/NotificationService.csproj"
COPY NotificationService/ NotificationService/
COPY Shared/ Shared/
WORKDIR "/src/NotificationService"
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]

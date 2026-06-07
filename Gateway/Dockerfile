FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Gateway/Gateway.csproj", "Gateway/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Gateway/Gateway.csproj"
COPY Gateway/ Gateway/
COPY Shared/ Shared/
WORKDIR "/src/Gateway"
RUN dotnet publish "Gateway.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]

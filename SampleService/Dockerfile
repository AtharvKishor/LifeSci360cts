FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SampleService/SampleService.csproj", "SampleService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "SampleService/SampleService.csproj"
COPY SampleService/ SampleService/
COPY Shared/ Shared/
WORKDIR "/src/SampleService"
RUN dotnet publish "SampleService.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SampleService.dll"]

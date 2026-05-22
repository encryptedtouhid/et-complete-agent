FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["src/ET.CompleteAgent.Domain/ET.CompleteAgent.Domain.csproj", "src/ET.CompleteAgent.Domain/"]
COPY ["src/ET.CompleteAgent.Application/ET.CompleteAgent.Application.csproj", "src/ET.CompleteAgent.Application/"]
COPY ["src/ET.CompleteAgent.Infrastructure/ET.CompleteAgent.Infrastructure.csproj", "src/ET.CompleteAgent.Infrastructure/"]
COPY ["src/ET.CompleteAgent.Host/ET.CompleteAgent.Host.csproj", "src/ET.CompleteAgent.Host/"]
RUN dotnet restore "src/ET.CompleteAgent.Host/ET.CompleteAgent.Host.csproj"

COPY . .
WORKDIR /src/src/ET.CompleteAgent.Host
RUN dotnet publish "ET.CompleteAgent.Host.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_NOLOGO=1 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ET.CompleteAgent.Host.dll"]

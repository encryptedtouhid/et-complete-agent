FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["src/EncryptedTouhid.CompleteAgent.Domain/EncryptedTouhid.CompleteAgent.Domain.csproj", "src/EncryptedTouhid.CompleteAgent.Domain/"]
COPY ["src/EncryptedTouhid.CompleteAgent.Application/EncryptedTouhid.CompleteAgent.Application.csproj", "src/EncryptedTouhid.CompleteAgent.Application/"]
COPY ["src/EncryptedTouhid.CompleteAgent.Infrastructure/EncryptedTouhid.CompleteAgent.Infrastructure.csproj", "src/EncryptedTouhid.CompleteAgent.Infrastructure/"]
COPY ["src/EncryptedTouhid.CompleteAgent.Host/EncryptedTouhid.CompleteAgent.Host.csproj", "src/EncryptedTouhid.CompleteAgent.Host/"]
RUN dotnet restore "src/EncryptedTouhid.CompleteAgent.Host/EncryptedTouhid.CompleteAgent.Host.csproj"

COPY . .
WORKDIR /src/src/EncryptedTouhid.CompleteAgent.Host
RUN dotnet publish "EncryptedTouhid.CompleteAgent.Host.csproj" \
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

ENTRYPOINT ["dotnet", "EncryptedTouhid.CompleteAgent.Host.dll"]

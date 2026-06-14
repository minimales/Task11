# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the SDK pin and the production project files first to maximise layer caching.
COPY ["global.json", "./"]
COPY ["task11.ApplicationCore/task11.ApplicationCore.csproj", "task11.ApplicationCore/"]
COPY ["task11.Infrastructure/task11.Infrastructure.csproj", "task11.Infrastructure/"]
COPY ["task11.Web/task11.Web.csproj", "task11.Web/"]
RUN dotnet restore "task11.Web/task11.Web.csproj"

COPY . .
RUN dotnet publish "task11.Web/task11.Web.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Run as the built-in non-root user provided by the aspnet image.
USER app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "task11.Web.dll"]

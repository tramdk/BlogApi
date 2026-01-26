# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Validating layers cache: Copy csproj first
COPY ["BlogApi.csproj", "./"]
RUN dotnet restore "BlogApi.csproj"

# Copy source code (respecting .dockerignore)
COPY . .

# Build and Publish
# UseAppHost=false ensures we get a .dll, not an exe (better for Docker)
RUN dotnet publish "BlogApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Expose ports (Default .NET 8 ports)
EXPOSE 8080
EXPOSE 8081

# Security: Run as non-root user 'app' (built-in)
USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlogApi.dll"]

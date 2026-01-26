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

# Copy artifacts first so we can fix permissions on them
COPY --from=build /app/publish .

# Create uploads folder and fix permissions so 'app' user can write
# This prevents UnauthorizedAccessException (403) when uploading files
RUN mkdir -p uploads && chown -R app:app /app

# Security: Run as non-root user 'app' (built-in)
USER app

ENTRYPOINT ["dotnet", "BlogApi.dll"]

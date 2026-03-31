FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY Holdus.sln .
COPY src/Holdus.Domain/Holdus.Domain.csproj src/Holdus.Domain/
COPY src/Holdus.Infrastructure/Holdus.Infrastructure.csproj src/Holdus.Infrastructure/
COPY src/Holdus.Application/Holdus.Application.csproj src/Holdus.Application/
COPY src/Holdus.API/Holdus.API.csproj src/Holdus.API/
RUN dotnet restore

# Copy all source and build
COPY . .
RUN dotnet publish src/Holdus.API/Holdus.API.csproj \
    -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Security: non-root user
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "Holdus.API.dll"]

# ═══════════════════════════════════════════════════════════════
# HOLDUS Backend — ASP.NET Core 9
# Multi-stage build para produção
# ═══════════════════════════════════════════════════════════════

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar csproj e restaurar dependências (cache layer)
COPY src/Holdus.Domain/Holdus.Domain.csproj src/Holdus.Domain/
COPY src/Holdus.Application/Holdus.Application.csproj src/Holdus.Application/
COPY src/Holdus.Infrastructure/Holdus.Infrastructure.csproj src/Holdus.Infrastructure/
COPY src/Holdus.API/Holdus.API.csproj src/Holdus.API/
RUN dotnet restore src/Holdus.API/Holdus.API.csproj

# Copiar todo o código e compilar
COPY . .
RUN dotnet publish src/Holdus.API/Holdus.API.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Timezone Brasil
ENV TZ=America/Sao_Paulo
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Copiar build
COPY --from=build /app/publish .

# Railway injeta PORT como variável de ambiente
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "Holdus.API.dll"]

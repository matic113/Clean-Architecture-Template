# Runtime base image (Alpine for minimal attack surface and lightweight footprint)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
RUN apk add --no-cache curl
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# SDK build image
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for optimal layer caching during restore
COPY ["src/CleanBase.Domain/CleanBase.Domain.csproj", "src/CleanBase.Domain/"]
COPY ["src/CleanBase.Application/CleanBase.Application.csproj", "src/CleanBase.Application/"]
COPY ["src/CleanBase.Infrastructure/CleanBase.Infrastructure.csproj", "src/CleanBase.Infrastructure/"]
COPY ["src/CleanBase.Api/CleanBase.Api.csproj", "src/CleanBase.Api/"]

RUN dotnet restore "src/CleanBase.Api/CleanBase.Api.csproj"

# Copy source and publish
COPY src/ src/
WORKDIR "/src/src/CleanBase.Api"
RUN dotnet publish "CleanBase.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final production runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CleanBase.Api.dll"]

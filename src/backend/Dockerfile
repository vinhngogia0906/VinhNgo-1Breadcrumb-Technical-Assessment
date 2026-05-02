# syntax=docker/dockerfile:1.7
# Build context: src/backend

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore in its own layer for better caching
COPY LibraryApi.slnx ./
COPY LibraryApi.Domain/LibraryApi.Domain.csproj         LibraryApi.Domain/
COPY LibraryApi.Application/LibraryApi.Application.csproj LibraryApi.Application/
COPY LibraryApi.Infrastructure/LibraryApi.Infrastructure.csproj LibraryApi.Infrastructure/
COPY LibraryApi.Web/LibraryApi.Web.csproj               LibraryApi.Web/
COPY LibraryApi.Tests/LibraryApi.Tests.csproj           LibraryApi.Tests/
RUN dotnet restore LibraryApi.slnx

COPY . .
RUN dotnet publish LibraryApi.Web/LibraryApi.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "LibraryApi.Web.dll"]

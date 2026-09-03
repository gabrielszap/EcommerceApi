FROM mcr.microsoft.com/dotnet/sdk:10.0.203 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json EcommerceApi.sln ./
COPY src ./src
COPY tests ./tests
RUN dotnet restore EcommerceApi.sln
RUN dotnet publish src/EcommerceApi.Api/EcommerceApi.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
RUN useradd --create-home --uid 10001 appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appuser /app
COPY --from=build --chown=appuser:appuser /app/publish .
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "EcommerceApi.Api.dll"]

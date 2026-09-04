FROM mcr.microsoft.com/dotnet/sdk:10.0.203 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props EcommerceApi.sln ./
COPY src ./src
COPY tests ./tests
RUN dotnet restore EcommerceApi.sln
RUN mkdir -p /tmp/https \
    && dotnet dev-certs https -ep /tmp/https/ecommerceapi.pfx -p ecommerceapi-dev-cert
RUN dotnet publish src/EcommerceApi.Api/EcommerceApi.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_HTTPS_PORTS=8081
RUN useradd --create-home --uid 10001 appuser \
    && mkdir -p /app/data /https \
    && chown -R appuser:appuser /app /https
COPY --from=build --chown=appuser:appuser /app/publish .
COPY --from=build --chown=appuser:appuser /tmp/https/ecommerceapi.pfx /https/ecommerceapi.pfx
USER appuser
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "EcommerceApi.Api.dll"]

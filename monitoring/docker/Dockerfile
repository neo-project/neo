# monitoring/docker/neo-cli/Dockerfile
# Use the ASP.NET Core runtime image which includes necessary frameworks
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Copy only necessary built files from the build context (repo root)
# Remove --from=base and adjust source path relative to repo root
COPY ./bin/Neo.CLI/net9.0/ ./

# Expose the default Prometheus metrics port (internal)
EXPOSE 9101
# Expose default P2P port (internal)
EXPOSE 10333

# Entrypoint to run Neo-CLI
# Command arguments (like --prometheus) will be appended from docker-compose.yml
ENTRYPOINT ["dotnet", "neo-cli.dll"]
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS Build

# Run this from the repository root folder
COPY src . 
COPY NuGet.Config /Neo.CLI

WORKDIR /Neo.CLI
RUN dotnet restore && dotnet publish  -f net7.0 -c Release -o /app 

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:7.0 AS Final
RUN apt-get update && apt-get install -y \
  screen \
  libleveldb-dev \
  sqlite3
RUN rm -rf /var/lib/apt/lists/*

WORKDIR /Neo.CLI
COPY  --from=Build /app .

ENTRYPOINT ["screen","-DmS","node","dotnet","neo-cli.dll","-r"]

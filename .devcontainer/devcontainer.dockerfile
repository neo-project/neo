# https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md
# https://mcr.microsoft.com/en-us/artifact/mar/dotnet/sdk/tags <-- this shows all images
FROM mcr.microsoft.com/dotnet/sdk:9.0.101-noble

# Install the libleveldb-dev package 
RUN apt-get update && apt-get install -y libleveldb-dev

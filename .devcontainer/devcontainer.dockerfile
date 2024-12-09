FROM mcr.microsoft.com/dotnet/sdk:9.0.101-noble

# Install the libleveldb-dev package 
RUN apt-get update && apt-get install -y libleveldb-dev

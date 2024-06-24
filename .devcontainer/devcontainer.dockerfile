FROM mcr.microsoft.com/devcontainers/dotnet:8.0-jammy
# Install the libleveldb-dev package 
RUN apt-get update
RUN apt-get install -y libleveldb-dev
RUN dotnet workload update

COPY * /neo/source
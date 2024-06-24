FROM mcr.microsoft.com/devcontainers/dotnet:8.0-bookworm-slim
# Install the libleveldb-dev package 
RUN apt-get update
RUN apt-get install -y libleveldb-dev screen

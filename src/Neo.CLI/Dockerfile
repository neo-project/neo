FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble

# Install all dependencies in a single RUN to reduce layers and speed up build
RUN apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        net-tools telnet bash-completion wget lrzsz zip \
        software-properties-common \
        apt-transport-https \
        build-essential \
        unzip \
        sqlite3 libsqlite3-dev libunwind8-dev \
        screen vim ca-certificates gnupg && \
    add-apt-repository ppa:dotnet/backports && \
    apt-get update && \
    apt-get install -y dotnet-sdk-9.0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /neo

# Copy and run scripts
COPY prepare-node.sh .
RUN chmod +x prepare-node.sh

# Accept build arg and set environment
ARG NEO_VERSION
ENV NEO_VERSION=${NEO_VERSION}

RUN ./prepare-node.sh v${NEO_VERSION}

# Modify config in-place
RUN sed -i 's/"BindAddress":[^,]*/"BindAddress": "0.0.0.0"/' neo-cli/Plugins/RpcServer/RpcServer.json

# Copy and set permissions
COPY start.sh .
RUN chmod -R +x ./neo-cli

# Expose port (optional but informative)
EXPOSE 10332

# Define entrypoint
ENTRYPOINT ["sh", "./start.sh"]
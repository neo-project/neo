FROM ubuntu:24.04 as Build
RUN apt-get update
RUN apt-get -y install net-tools telnet bash-completion wget  lrzsz zip
RUN apt-get update && apt-get install -y software-properties-common
RUN add-apt-repository ppa:dotnet/backports
RUN apt-get update && apt-get install -y dotnet-sdk-9.0
RUN apt-get install -y apt-transport-https && apt-get update && apt update && apt install build-essential unzip -y
RUN apt-get install -y sqlite3 libsqlite3-dev libunwind8-dev
RUN apt-get install -y screen

WORKDIR /neo
COPY prepare-node.sh .
RUN chmod +x prepare-node.sh
ARG NEO_VERSION
ENV NEO_VERSION=${NEO_VERSION}

RUN chmod +x prepare-node.sh && ./prepare-node.sh v${NEO_VERSION}

RUN apt-get update && apt-get install -y vim
RUN sed -i 's/"BindAddress":[^,]*/"BindAddress": "0.0.0.0"/' neo-cli/Plugins/RpcServer/RpcServer.json
COPY start.sh .
RUN chmod -R +x ./neo-cli
ENTRYPOINT ["sh", "./start.sh"]
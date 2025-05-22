#!/bin/sh
echo "Downloading neo node $1"
wget  https://github.com/neo-project/neo/releases/download/$1/neo-cli.$1-linux-x64.tar.gz
mkdir neo-cli-linux-x64
tar -zxvf neo-cli.$1-linux-x64.tar.gz -C neo-cli-linux-x64/
mv neo-cli-linux-x64 neo-cli

# Allow CLI and Plugins in different versions in case only CLI is released or for any other test usage
if [ -z "$2" ]; then
    echo "Downloading plugins $1"
    wget https://github.com/neo-project/neo/releases/download/$1/ApplicationLogs.zip
    wget https://github.com/neo-project/neo/releases/download/$1/RpcServer.zip
    wget https://github.com/neo-project/neo/releases/download/$1/TokensTracker.zip
else
    echo "Downloading plugins $2"
    wget https://github.com/neo-project/neo/releases/download/$2/ApplicationLogs.zip
    wget https://github.com/neo-project/neo/releases/download/$2/RpcServer.zip
    wget https://github.com/neo-project/neo/releases/download/$2/TokensTracker.zip
fi

unzip -n ApplicationLogs.zip -d ./neo-cli/
unzip -n RpcServer.zip -d ./neo-cli/
unzip -n TokensTracker.zip -d ./neo-cli/

echo "Node Is Ready!"
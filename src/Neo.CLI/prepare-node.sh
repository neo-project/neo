#!/bin/sh

set -e

# if $1 not provided, show usage
if [ -z "$1" ]; then
    echo "Usage: $0 <version> [plugins-version]"
    echo "Example: $0 x.y.z"
    echo "Example: $0 x.y.z1 x.y.z2"
    exit 1
fi

NEO_VERSION=$1
PLUGINS_VERSION=$2

# if $NEO_VERSION not start with v, then add v to it
if [[ $NEO_VERSION != v* ]]; then
    NEO_VERSION=v$NEO_VERSION
fi

# Allow CLI and Plugins in different versions in case only CLI is released or for any other test usage
if [ -z "$2" ]; then
    PLUGINS_VERSION=$NEO_VERSION
elif [[ $PLUGINS_VERSION != v* ]]; then
    PLUGINS_VERSION=v$PLUGINS_VERSION
fi

echo "Downloading neo node $NEO_VERSION"
wget  https://github.com/neo-project/neo/releases/download/$NEO_VERSION/neo-cli.$NEO_VERSION-linux-x64.tar.gz
mkdir neo-cli-linux-x64
tar -zxvf neo-cli.$NEO_VERSION-linux-x64.tar.gz -C neo-cli-linux-x64/
mv neo-cli-linux-x64 neo-cli

echo "Downloading plugins $PLUGINS_VERSION"
wget https://github.com/neo-project/neo/releases/download/$PLUGINS_VERSION/ApplicationLogs.zip
wget https://github.com/neo-project/neo/releases/download/$PLUGINS_VERSION/RpcServer.zip
wget https://github.com/neo-project/neo/releases/download/$PLUGINS_VERSION/TokensTracker.zip

unzip -n ApplicationLogs.zip -d ./neo-cli/
unzip -n RpcServer.zip -d ./neo-cli/
unzip -n TokensTracker.zip -d ./neo-cli/

rm neo-cli.$NEO_VERSION-linux-x64.tar.gz ApplicationLogs.zip RpcServer.zip TokensTracker.zip

echo "Node Is Ready!"

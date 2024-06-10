#!/bin/bash
WORKSPACE_ROOT_PATH=/workspaces/neo-cris
NEO_CLI_PATH=$WORKSPACE_ROOT_PATH/src/Neo.CLI
LEVEL_DB_PATH=$WORKSPACE_ROOT_PATH/src/Plugins/LevelDBStore

cd $LEVEL_DB_PATH

dotnet publish -c Release -f net8.0 -o $NEO_CLI_PATH/Plugins/LevelDBStore

cd $NEO_CLI_PATH

dotnet publish -c Release -f net8.0 --output ./

expect ./test-neo-cli.expect

if [ $? -eq 0 ]; then
    echo "Expect script executed successfully."
else
    echo "Expect script failed." 
fi

rm test-wallet*
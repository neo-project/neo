#!/bin/bash
# Run tests in the tests directory

echo "$(date) - Running tests..."

groups=(
    "basics"
)

# The environment variables in env.py, the default value is localnet testing network.
# if you want to use other network, the default values should be reset.
# For testing:
# 1. Start a privatenet/localnet.
# 2. The RpcServer, DBFT and ApplicationLog plugins must be installed.
# NOTE: don't run the tests in mainnet.
for group in ${groups[@]}; do
    echo "$(date) - Running $group tests..."
    for file in testcases/$group/*.py; do
        basename=$(basename $file .py) # remove the .py extension
        echo "$(date) - Run $basename test..."

        python3 -B -m testcases.$group.$basename
        if [ $? -ne 0 ]; then
            echo "$(date) - Failed to run $basename test"
        else
            echo "$(date) - Passed $basename test"
        fi
    done
    echo "$(date) - $group tests completed"
done

echo "$(date) - Tests completed"
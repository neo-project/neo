#!/bin/bash

# start neo-cli，output log into neo.log
screen -dmS neo bash -c "./neo-cli/neo-cli > neo.log 2>&1"

# wait for neo.log
while [ ! -f neo.log ]; do
  sleep 0.5
done

tail -f neo.log
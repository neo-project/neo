# Configurations
There is many ways to configure `neo-build`.
1. Environment Variables
1. JSON Files
1. Commandline

## Environment Variables
All variables are prefixed with `NEOBUILD_`. These variables can
be set at `system`, `user` and `program` levels.

### List of Variables

- Network
  - `NEOBUILD_NETWORK_LISTEN`: Node's listening IP address. _Default is `127.0.0.1` or `loopback`._
  - `NEOBUILD_NETWORK_PORT`: Node's listening internet port. _Default is a random number from `1-65535`._
  - `NEOBUILD_NETWORK_MINDESIREDCONNECTIONS`: Node's minimum connections it will accept. _Default is `10` active connections._
  - `NEOBUILD_NETWORK_MAXCONNECTIONS`: Node's maximum connections upto. _Default is `40` maximum active connections._
  - `NEOBUILD_NETWORK_ENABLECOMPRESSION`: Node network data is compressed. _Default is `true` for enable compression._
- Storage
  - `NEOBUILD_STORAGE_STOREROOT`: Full directory path to the location where you want the blockchain database to be. _Default is `Store_30564544` in the current directory where **Neo Build** is ran from. **Example**: `C:\projects\myContract\Store_30564544` where `C:\projects\myContract\` is the current directory._
  - `NEOBUILD_STORAGE_CHECKPOINTROOT`: Full directory path to the location where you want the blockchain snapshots to be. _Default is `Checkpoints_30564544` in the current directory where **Neo Build** is ran from. **Example**: `C:\projects\myContract\Checkpoints_30564544` where `C:\projects\myContract\` is the current directory._

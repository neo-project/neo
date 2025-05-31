# Neo Build Scripts with Plugin Integration

This directory contains scripts to build Neo.CLI and all its plugins, organizing them in framework-specific directories.

## Available Scripts

- `build-with-plugins.sh` - For Linux/macOS (Version 1.1.0)
- `build-with-plugins.bat` - For Windows (Version 1.1.0)

## Features

- Automatically builds Neo.CLI project
- Builds the entire Neo solution first to ensure all dependencies are resolved
- Detects the .NET version from the project file
- Builds all plugins in the src/Plugins directory
- Places each plugin in its own subdirectory under the Neo.CLI's framework-specific Plugins directory
- Copies plugin configuration files automatically
- Smart DLL detection that searches multiple locations:
  - Main output directory (bin/)
  - Project-specific folders under bin/ (e.g., bin/Neo.Plugins.ApplicationLogs/)
  - Framework-specific folders (e.g., bin/Neo.CLI/net9.0/)
  - Source directory build outputs
- Comprehensive logging to build.log
- Build time measurement
- Essential plugin verification
- .NET SDK version checking
- File copy verification
- Always performs a clean build to ensure up-to-date files

## Usage

### On Linux/macOS:

```bash
cd scripts
./build-with-plugins.sh

# View build log
tail -f ../build.log
```

### On Windows:

```cmd
cd scripts
build-with-plugins.bat

REM View build log
type ..\build.log
```

## Output Structure

After running the script, the bin/ directory will have the following structure:

```
bin/
└── Neo.CLI/
    └── net9.0/    (or other detected framework version)
        ├── neo-cli.dll (and other core files)
        └── Plugins/
            ├── DBFTPlugin/
            │   ├── DBFTPlugin.dll
            │   └── DBFTPlugin.json (if available)
            ├── RpcServer/
            │   ├── RpcServer.dll
            │   └── RpcServer.json (if available)
            └── ... (other plugin directories)
```

## Running Neo with Plugins

After the build is complete, you can run Neo with all its plugins using:

```bash
cd bin/Neo.CLI/net9.0  # Replace net9.0 with your framework version
dotnet neo-cli.dll
```

## Process Overview

1. Verify .NET SDK installation
2. Clean previous build artifacts
3. Detect .NET version from Neo.CLI.csproj
4. Build the entire Neo solution first to ensure all dependencies are resolved
5. Build Neo.CLI project
6. Copy Neo.CLI output to the bin directory
7. Create the framework-specific Plugins directory (bin/Neo.CLI/[framework]/Plugins)
8. Find all plugin projects within the src/Plugins directory (detecting all .csproj files)
9. Build each plugin project
10. Create a plugin-specific subdirectory for each plugin
11. Copy each plugin's DLL to its specific directory under Plugins
12. Copy each plugin's configuration file (if it exists) to the same directory
13. Verify essential plugins were installed
14. Measure and report build time
15. Generate detailed build log

## Plugin Detection

The scripts handle plugin detection by:

- Processing each subdirectory under src/Plugins
- Finding all .csproj files in each directory
- Building each project file found
- Creating a directory for each plugin under the Plugins directory
- Copying DLLs and config files to their respective plugin directories

This approach ensures all plugins are properly built and copied, even when:
- A plugin project name differs from its directory name
- Multiple plugin projects exist in a single directory
- Non-standard naming conventions are used

## Directory Structure After Build

```
/bin
  └── Neo.CLI/
      └── net9.0/    (or other detected framework version)
          ├── neo-cli.dll (and other core files)
          └── Plugins/
              ├── DBFTPlugin/
              │   ├── DBFTPlugin.dll
              │   └── DBFTPlugin.json
              ├── RpcServer/
              │   ├── RpcServer.dll
              │   └── RpcServer.json
              └── ... (other plugin directories)
```

## Troubleshooting

### Common Issues

1. **Missing .NET SDK**:
   - Error: `.NET SDK is not installed or not in PATH`
   - Solution: Install the correct .NET SDK version (see project requirements)

2. **Plugin not found**:
   - Error: `Could not find DLL for [PluginName]`
   - Solution: Verify the plugin project builds successfully

3. **File copy failures**:
   - Error: `Failed to copy [file] to [destination]`
   - Solution: Check file permissions and disk space

4. **Missing essential plugin**:
   - Warning: `Essential plugin [PluginName] is missing`
   - Solution: Ensure the essential plugin project exists and builds correctly

5. **Dependency resolution errors**:
   - Error: `Metadata file 'xxx.dll' could not be found`
   - Solution: This should be automatically resolved by building the entire solution first

### Checking Build Logs

Both scripts create a `build.log` file in the project root with detailed output:

```bash
# View last 20 lines of log
tail -20 build.log

# Search for errors
grep -i error build.log
```

## Notes

- The scripts will automatically detect the target .NET framework version from the Neo.CLI project file
- If the target framework cannot be detected, the scripts will fall back to a default of "net7.0"
- The scripts will overwrite existing files if they already exist in the destination directories
- If a plugin doesn't have a corresponding JSON configuration file, the script will skip copying the config but will still copy the DLL
- The output will be placed in the `/bin/Neo.CLI/[framework]` directory at the root of your Neo repository
- Each plugin will be placed in its own subdirectory under `/bin/Neo.CLI/[framework]/Plugins` with its DLL and config files
- Essential plugins (DBFTPlugin, ApplicationLogs, RpcServer) are verified after build
- Build time is measured and reported
- A complete build log is generated at the root of the repository

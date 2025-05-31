#!/bin/bash
# Neo build script with plugin integration v1.1.0
# This script builds Neo.CLI and all plugins, organizing them in framework-specific directories

# Start time measurement
START_TIME=$(date +%s)

# Set bash to exit on error
set -e

# Change to the root directory of the Neo project
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$ROOT_DIR"

# Colors for terminal output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Log file setup
LOG_FILE="$ROOT_DIR/build.log"
echo "Build started at $(date)" > "$LOG_FILE"

# Configuration
BUILD_CONFIG="Release"
OUTPUT_DIR="$ROOT_DIR/bin"
CLI_PROJECT="$ROOT_DIR/src/Neo.CLI/Neo.CLI.csproj"
NEO_SOLUTION="$ROOT_DIR/neo.sln"

# Extract target framework from Neo.CLI.csproj
echo -e "${YELLOW}Detecting .NET version from Neo.CLI project...${NC}"
echo "Detecting .NET version from Neo.CLI project..." >> "$LOG_FILE"

if [ ! -f "$CLI_PROJECT" ]; then
    echo -e "${RED}Error: Could not find Neo.CLI project at $CLI_PROJECT${NC}"
    echo "Error: Could not find Neo.CLI project at $CLI_PROJECT" >> "$LOG_FILE"
    exit 1
fi

TARGET_FRAMEWORK=$(grep -o '<TargetFramework>.*</TargetFramework>' "$CLI_PROJECT" | sed 's/<TargetFramework>\(.*\)<\/TargetFramework>/\1/')

if [ -z "$TARGET_FRAMEWORK" ]; then
    echo -e "${RED}Error: Could not detect target framework from Neo.CLI project${NC}"
    echo -e "${YELLOW}Falling back to default: net7.0${NC}"
    echo -e "${YELLOW}Falling back to default: net7.0${NC}" >> "$LOG_FILE"
    TARGET_FRAMEWORK="net7.0"
else
    echo -e "${GREEN}Detected target framework: $TARGET_FRAMEWORK${NC}"
    echo "Detected target framework: $TARGET_FRAMEWORK" >> "$LOG_FILE"
fi

echo -e "${GREEN}Neo Build Script with Plugin Integration${NC}"
echo "Root directory: $ROOT_DIR"
echo "Output directory: $OUTPUT_DIR"
echo "Target framework: $TARGET_FRAMEWORK"
echo

# Verify .NET SDK is installed
echo -e "${YELLOW}Verifying .NET SDK installation...${NC}"
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET SDK is not installed or not in PATH${NC}"
    echo "Error: .NET SDK is not installed or not in PATH" >> "$LOG_FILE"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo -e "${GREEN}Found .NET SDK version: $DOTNET_VERSION${NC}"
echo "Found .NET SDK version: $DOTNET_VERSION" >> "$LOG_FILE"

# Clean up old build artifacts
echo -e "${YELLOW}Cleaning previous build artifacts...${NC}"
echo "Cleaning previous build artifacts..." >> "$LOG_FILE"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"
echo -e "${GREEN}Clean complete${NC}"
echo "Clean complete" >> "$LOG_FILE"
echo

# First build the entire solution to ensure dependencies are built
if [ -f "$NEO_SOLUTION" ]; then
    echo -e "${YELLOW}Building entire Neo solution first to resolve dependencies...${NC}"
    echo "Building entire Neo solution first to resolve dependencies..." >> "$LOG_FILE"
    dotnet build "$NEO_SOLUTION" -c "$BUILD_CONFIG"
    if [ $? -ne 0 ]; then
        echo -e "${RED}Failed to build Neo solution${NC}"
        echo "Failed to build Neo solution" >> "$LOG_FILE"
        exit 1
    fi
    echo -e "${GREEN}Successfully built Neo solution${NC}"
    echo "Successfully built Neo solution" >> "$LOG_FILE"
    echo
else
    echo -e "${YELLOW}Neo solution file not found. Will attempt to build Neo.CLI directly.${NC}"
    echo "Neo solution file not found. Will attempt to build Neo.CLI directly." >> "$LOG_FILE"
fi

# Function to build a project
build_project() {
    local project_path="$1"
    local project_name="$(basename "${project_path%.*}")"

    echo -e "${YELLOW}Building project: ${project_name}${NC}"
    echo "Building project: ${project_name}" >> "$LOG_FILE"

    dotnet build "$project_path" -c "$BUILD_CONFIG"

    if [ $? -ne 0 ]; then
        echo -e "${RED}Failed to build ${project_name}${NC}"
        echo "Failed to build ${project_name}" >> "$LOG_FILE"
        exit 1
    fi

    echo -e "${GREEN}Successfully built ${project_name}${NC}"
    echo "Successfully built ${project_name}" >> "$LOG_FILE"
    echo
}

# Function to find a DLL file in the bin directory
find_dll() {
    local dll_name="$1"

    # First, check if the DLL is already in the main bin directory
    if [ -d "$OUTPUT_DIR" ]; then
        local root_bin_dll="$OUTPUT_DIR/$dll_name"
        if [ -f "$root_bin_dll" ]; then
            echo "$root_bin_dll"
            return
        fi

        # Check in direct project folders under bin
        for project_dir in "$OUTPUT_DIR"/* ; do
            if [ -d "$project_dir" ]; then
                local project_dll="$project_dir/$dll_name"
                if [ -f "$project_dll" ]; then
                    echo "$project_dll"
                    return
                fi

                # Also check for nested target framework folders
                for framework_dir in "$project_dir"/* ; do
                    if [ -d "$framework_dir" ]; then
                        local framework_dll="$framework_dir/$dll_name"
                        if [ -f "$framework_dll" ]; then
                            echo "$framework_dll"
                            return
                        fi
                    fi
                done
            fi
        done
    fi

    # If not found in bin, look in the src directory
    local base_dir="$ROOT_DIR/src"

    # List of common build output directories to search in order
    local search_dirs=(
        "${base_dir}/*/bin/${BUILD_CONFIG}/${TARGET_FRAMEWORK}"
        "${base_dir}/*/bin/${BUILD_CONFIG}"
        "${base_dir}/*/*/bin/${BUILD_CONFIG}/${TARGET_FRAMEWORK}"
        "${base_dir}/*/*/bin/${BUILD_CONFIG}"
    )

    # Try the common directories
    for dir_pattern in "${search_dirs[@]}"; do
        for dir in $dir_pattern; do
            if [ -d "$dir" ] && [ -f "${dir}/${dll_name}" ]; then
                echo "${dir}/${dll_name}"
                return
            fi
        done
    done

    # If not found, do a recursive search through src
    echo -e "${YELLOW}Searching for ${dll_name} in src directory...${NC}"
    local found_dll=$(find "${base_dir}" -name "${dll_name}" -type f 2>/dev/null | head -1)

    if [ -n "$found_dll" ]; then
        echo "$found_dll"
    else
        echo ""
    fi
}

# Build Neo.CLI
build_project "$CLI_PROJECT"

# Copy Neo.CLI output to bin directory
echo -e "${YELLOW}Copying Neo.CLI output to bin directory...${NC}"
echo "Copying Neo.CLI output to bin directory..." >> "$LOG_FILE"

# Find the Neo.CLI DLL
CLI_DLL=$(find_dll "neo-cli.dll")

if [ -n "$CLI_DLL" ] && [ -f "$CLI_DLL" ]; then
    CLI_DIR=$(dirname "$CLI_DLL")
    echo -e "${GREEN}Found neo-cli.dll at: $CLI_DIR${NC}"
    echo "Found neo-cli.dll at: $CLI_DIR" >> "$LOG_FILE"
    cp -r "$CLI_DIR/"* "$OUTPUT_DIR/"
    echo -e "${GREEN}Neo.CLI copied to output directory${NC}"
    echo "Neo.CLI copied to output directory" >> "$LOG_FILE"
else
    echo -e "${RED}Error: Could not find neo-cli.dll${NC}"
    echo -e "${YELLOW}Trying to find any DLLs in the output directories...${NC}"
    echo "Warning: Could not find neo-cli.dll, searching for any DLLs" >> "$LOG_FILE"

    # Try to find any DLL to determine the output location
    ANY_DLL=$(find "$ROOT_DIR" -path "*/bin/$BUILD_CONFIG/*" -name "*.dll" -type f | head -1)
    if [ -n "$ANY_DLL" ]; then
        ANY_DIR=$(dirname "$ANY_DLL")
        echo -e "${GREEN}Found DLLs at: $ANY_DIR${NC}"
        echo "Found DLLs at: $ANY_DIR" >> "$LOG_FILE"
        cp -r "$ANY_DIR/"* "$OUTPUT_DIR/"
        echo -e "${GREEN}Neo.CLI copied from found directory${NC}"
        echo "Neo.CLI copied from found directory" >> "$LOG_FILE"
    else
        echo -e "${RED}Failed to locate any build output. Make sure the build succeeded.${NC}"
        echo "Error: Failed to locate any build output" >> "$LOG_FILE"
        exit 1
    fi
fi
echo

# Create plugins directory in the Neo.CLI output directory
echo -e "${YELLOW}Setting up plugins directory...${NC}"
echo "Setting up plugins directory..." >> "$LOG_FILE"

# Create the specific Neo.CLI output directory structure
CLI_PLUGINS_DIR="$OUTPUT_DIR/Neo.CLI/$TARGET_FRAMEWORK/Plugins"
mkdir -p "$CLI_PLUGINS_DIR"

echo -e "${GREEN}Plugins directory created at: $CLI_PLUGINS_DIR${NC}"
echo "Plugins directory created at: $CLI_PLUGINS_DIR" >> "$LOG_FILE"
echo

# Build and copy all plugins
echo -e "${YELLOW}Building and installing plugins...${NC}"
echo "Building and installing plugins..." >> "$LOG_FILE"

# Get all plugin directories
PLUGIN_DIRS=$(find "$ROOT_DIR/src/Plugins" -maxdepth 1 -mindepth 1 -type d | sort)

# Count of plugins found and processed
PLUGINS_FOUND=0
PLUGINS_PROCESSED=0

# Process each plugin directory
for plugin_dir in $PLUGIN_DIRS; do
    plugin_dir_name="$(basename "$plugin_dir")"

    # Skip obj directory
    if [[ "$plugin_dir_name" == "obj" ]]; then
        continue
    fi

    # Find all .csproj files in the directory
    project_files=$(find "$plugin_dir" -maxdepth 1 -name "*.csproj")

    if [ -z "$project_files" ]; then
        echo "No project files found in $plugin_dir_name, skipping"
        echo "No project files found in $plugin_dir_name, skipping" >> "$LOG_FILE"
        continue
    fi

    PLUGINS_FOUND=$((PLUGINS_FOUND + 1))

    # Build and copy each project in the directory
    for project_file in $project_files; do
        # Build the plugin
        build_project "$project_file"

        plugin_name="$(basename "${project_file%.*}")"
        plugin_dll_name="${plugin_name}.dll"

        # Find the DLL file
        plugin_dll=$(find_dll "$plugin_dll_name")

        # Create a plugin-specific folder in the Neo.CLI's plugins directory
        plugin_specific_dir="$CLI_PLUGINS_DIR/$plugin_name"
        mkdir -p "$plugin_specific_dir"

        # Copy the plugin DLL to its specific directory
        if [ -n "$plugin_dll" ] && [ -f "$plugin_dll" ]; then
            echo "Copying $plugin_dll_name to $plugin_specific_dir from $plugin_dll"
            echo "Copying $plugin_dll_name to $plugin_specific_dir from $plugin_dll" >> "$LOG_FILE"
            cp "$plugin_dll" "$plugin_specific_dir/"

            # Verify the copy was successful
            if [ ! -f "$plugin_specific_dir/$plugin_dll_name" ]; then
                echo -e "${RED}Error: Failed to copy $plugin_dll_name to $plugin_specific_dir${NC}"
                echo "Error: Failed to copy $plugin_dll_name to $plugin_specific_dir" >> "$LOG_FILE"
                exit 1
            fi
        else
            echo -e "${RED}Warning: Could not find DLL for $plugin_name${NC}"
            echo "Warning: Could not find DLL for $plugin_name" >> "$LOG_FILE"
        fi

        # Look for the config file in multiple locations
        plugin_config="$plugin_dir/$plugin_name.json"
        if [ ! -f "$plugin_config" ]; then
            # Try alternative locations
            alt_config=$(find "$plugin_dir" -name "${plugin_name}.json" -type f | head -1)
            if [ -n "$alt_config" ]; then
                plugin_config="$alt_config"
            fi
        fi

        # Copy the plugin config if it exists
        if [ -f "$plugin_config" ]; then
            echo "Copying ${plugin_name}.json to $plugin_specific_dir from $plugin_config"
            echo "Copying ${plugin_name}.json to $plugin_specific_dir from $plugin_config" >> "$LOG_FILE"
            cp "$plugin_config" "$plugin_specific_dir/"

            # Verify the copy was successful
            if [ ! -f "$plugin_specific_dir/$plugin_name.json" ]; then
                echo -e "${RED}Error: Failed to copy $plugin_name.json to $plugin_specific_dir${NC}"
                echo "Error: Failed to copy $plugin_name.json to $plugin_specific_dir" >> "$LOG_FILE"
                exit 1
            fi
        else
            echo "No config file found for $plugin_name, skipping config copy"
            echo "No config file found for $plugin_name, skipping config copy" >> "$LOG_FILE"
        fi

        echo -e "${GREEN}Plugin $plugin_name installed${NC}"
        echo "Plugin $plugin_name installed" >> "$LOG_FILE"
        echo

        PLUGINS_PROCESSED=$((PLUGINS_PROCESSED + 1))
    done
done

echo -e "${GREEN}All plugins have been built and installed${NC}"
echo -e "Found $PLUGINS_FOUND plugin directories, processed $PLUGINS_PROCESSED plugin projects"
echo "All plugins have been built and installed" >> "$LOG_FILE"
echo "Found $PLUGINS_FOUND plugin directories, processed $PLUGINS_PROCESSED plugin projects" >> "$LOG_FILE"
echo

# Calculate build time
END_TIME=$(date +%s)
BUILD_TIME=$((END_TIME - START_TIME))
echo -e "${GREEN}Build completed successfully in ${BUILD_TIME} seconds!${NC}"
echo "Build completed successfully in ${BUILD_TIME} seconds!" >> "$LOG_FILE"

echo -e "${GREEN}Build completed successfully!${NC}"
echo "The Neo node with plugins is available at: $OUTPUT_DIR/Neo.CLI/$TARGET_FRAMEWORK"
echo "You can run it using: cd $OUTPUT_DIR/Neo.CLI/$TARGET_FRAMEWORK && dotnet neo-cli.dll"

# Verify essential plugins were installed
ESSENTIAL_PLUGINS=("DBFTPlugin" "ApplicationLogs" "RpcServer")
for plugin in "${ESSENTIAL_PLUGINS[@]}"; do
    if [ ! -f "$CLI_PLUGINS_DIR/$plugin/$plugin.dll" ]; then
        echo -e "${YELLOW}Warning: Essential plugin $plugin is missing${NC}"
        echo "Warning: Essential plugin $plugin is missing" >> "$LOG_FILE"
    fi
done

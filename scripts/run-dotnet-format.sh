#!/bin/bash
# Script to run dotnet format with the same settings as the GitHub workflow

VERIFY_ONLY=false
VERBOSITY="diagnostic"
FOLDER="."

# Process command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --verify-only)
      VERIFY_ONLY=true
      shift
      ;;
    --verbosity)
      VERBOSITY="$2"
      shift 2
      ;;
    --folder)
      FOLDER="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1"
      echo "Usage: $0 [--verify-only] [--verbosity LEVEL] [--folder PATH]"
      exit 1
      ;;
  esac
done

FORMAT_ARGS=()

if [ "$VERIFY_ONLY" = true ]; then
    FORMAT_ARGS+=("--verify-no-changes")
    echo -e "\033[33mRunning in verify-only mode (will not make changes)\033[0m"
else
    echo -e "\033[32mRunning in fix mode (will modify files)\033[0m"
fi

echo -e "\033[36mRunning dotnet format with verbosity: $VERBOSITY\033[0m"
echo -e "\033[36mTarget folder: $FOLDER\033[0m"
echo ""

# Configure the format command with the same parameters as the GitHub workflow
FORMAT_ARGS+=("--verbosity" "$VERBOSITY")

# Only add include if a specific folder is specified
if [ "$FOLDER" != "." ]; then
    FORMAT_ARGS+=("--include" "$FOLDER")
fi

# Execute the command
echo -e "\033[35mExecuting: dotnet format ${FORMAT_ARGS[*]}\033[0m"
echo -e "\033[35m---------------------------------------------------------------\033[0m"

dotnet format "${FORMAT_ARGS[@]}"
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo -e "\033[35m---------------------------------------------------------------\033[0m"
    echo -e "\033[32mFormat check passed successfully!\033[0m"
else
    echo -e "\033[35m---------------------------------------------------------------\033[0m"
    echo -e "\033[31mFormat check failed with exit code $EXIT_CODE\033[0m"
    
    if [ "$VERIFY_ONLY" = true ]; then
        echo -e "\033[33mRun the script without --verify-only to automatically fix formatting issues\033[0m"
    fi
fi

exit $EXIT_CODE

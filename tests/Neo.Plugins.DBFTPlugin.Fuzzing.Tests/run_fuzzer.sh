#!/bin/bash

# Comprehensive fuzzing script for Neo DBFT consensus
# This script handles all fuzzing operations: generating corpus, running fuzzer, analyzing results

# Set default values for configuration
CORPUS_DIR="corpus"
FINDINGS_DIR="findings"
CRASHES_DIR="crashes"
REPORTS_DIR="reports"
DICT_FILE="fuzzing.dict"
MAX_LEN=4096
TIMEOUT=5
WORKERS=4
JOBS=4
RUNS=100000
BUILD_CONFIG="Release"
CHECKPOINT_INTERVAL=3600 # 1 hour
TOTAL_RUNTIME=0 # 0 means run indefinitely
CORPUS_COUNT=50 # Number of corpus files to generate per message type

# Ensure we're in the right directory
cd "$(dirname "$0")"

# Function to display help message
show_help() {
    echo "Neo DBFT Consensus Fuzzing Tool"
    echo "=============================="
    echo ""
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  fuzz                Run continuous fuzzing (default)"
    echo "  generate-corpus     Generate corpus files"
    echo "  run-corpus          Run fuzzer on existing corpus"
    echo "  analyze <file>      Analyze a specific test case"
    echo "  report              Generate a report of findings"
    echo "  clean               Clean up temporary files"
    echo "  help                Show this help message"
    echo ""
    echo "Options:"
    echo "  --corpus-dir=DIR    Set corpus directory (default: $CORPUS_DIR)"
    echo "  --findings-dir=DIR  Set findings directory (default: $FINDINGS_DIR)"
    echo "  --crashes-dir=DIR   Set crashes directory (default: $CRASHES_DIR)"
    echo "  --dict=FILE         Set dictionary file (default: $DICT_FILE)"
    echo "  --max-len=N         Set maximum input length (default: $MAX_LEN)"
    echo "  --timeout=N         Set timeout in seconds (default: $TIMEOUT)"
    echo "  --workers=N         Set number of worker threads (default: $WORKERS)"
    echo "  --jobs=N            Set number of jobs (default: $JOBS)"
    echo "  --runs=N            Set number of runs (default: $RUNS)"
    echo "  --debug             Build in Debug mode instead of Release"
    echo "  --checkpoint=N      Set checkpoint interval in seconds (default: $CHECKPOINT_INTERVAL)"
    echo "  --runtime=N         Set total runtime in seconds, 0 for indefinite (default: $TOTAL_RUNTIME)"
    echo "  --corpus-count=N    Set number of corpus files to generate per type (default: $CORPUS_COUNT)"
    echo ""
    echo "Examples:"
    echo "  $0                  Run continuous fuzzing with default settings"
    echo "  $0 generate-corpus --corpus-dir=my_corpus --corpus-count=100"
    echo "  $0 analyze crashes/crash-123abc"
    echo "  $0 fuzz --workers=8 --timeout=10"
    echo "  $0 fuzz --runtime=86400 --checkpoint=1800"  # Run for 24 hours, checkpoint every 30 minutes
    echo "  $0 fuzz --runtime=0 --workers=16"           # Run indefinitely with 16 workers
}

# Function to parse command line arguments
parse_args() {
    # Parse command
    if [ $# -gt 0 ]; then
        COMMAND="$1"
        shift
    else
        COMMAND="fuzz"
    fi

    # Parse options
    while [ $# -gt 0 ]; do
        case "$1" in
            --corpus-dir=*)
                CORPUS_DIR="${1#*=}"
                ;;
            --findings-dir=*)
                FINDINGS_DIR="${1#*=}"
                ;;
            --crashes-dir=*)
                CRASHES_DIR="${1#*=}"
                ;;
            --dict=*)
                DICT_FILE="${1#*=}"
                ;;
            --max-len=*)
                MAX_LEN="${1#*=}"
                ;;
            --timeout=*)
                TIMEOUT="${1#*=}"
                ;;
            --workers=*)
                WORKERS="${1#*=}"
                ;;
            --jobs=*)
                JOBS="${1#*=}"
                ;;
            --runs=*)
                RUNS="${1#*=}"
                ;;
            --debug)
                BUILD_CONFIG="Debug"
                ;;
            --checkpoint=*)
                CHECKPOINT_INTERVAL="${1#*=}"
                ;;
            --runtime=*)
                TOTAL_RUNTIME="${1#*=}"
                ;;
            --corpus-count=*)
                CORPUS_COUNT="${1#*=}"
                ;;
            *)
                if [ "$COMMAND" = "analyze" ]; then
                    ANALYZE_FILE="$1"
                else
                    echo "Unknown option: $1"
                    show_help
                    exit 1
                fi
                ;;
        esac
        shift
    done
}

# Function to setup directories
setup_dirs() {
    mkdir -p "$CORPUS_DIR"
    mkdir -p "$FINDINGS_DIR"
    mkdir -p "$CRASHES_DIR"
    mkdir -p "$REPORTS_DIR"
}

# Function to build the project
build_project() {
    echo "Building project in $BUILD_CONFIG mode..."
    dotnet build -c "$BUILD_CONFIG"

    # Find the correct path to the DLL
    DLL_PATH="../../bin/tests/Neo.Plugins.DBFTPlugin.Fuzzing.Tests/net9.0/Neo.Plugins.DBFTPlugin.Fuzzing.Tests.dll"

    if [ ! -f "$DLL_PATH" ]; then
        echo "Could not find DLL at $DLL_PATH"
        echo "Searching for the DLL..."
        DLL_PATH=$(find ../../bin -name "Neo.Plugins.DBFTPlugin.Fuzzing.Tests.dll" | head -n 1)

        if [ -z "$DLL_PATH" ]; then
            echo "Could not find the DLL. Make sure the project is built correctly."
            exit 1
        else
            echo "Found DLL at $DLL_PATH"
        fi
    fi
}

# Function to check and install SharpFuzz
check_sharpfuzz() {
    if ! command -v sharpfuzz &> /dev/null; then
        echo "SharpFuzz not found. Installing..."
        dotnet tool install --global SharpFuzz.CommandLine

        # Verify installation
        if ! command -v sharpfuzz &> /dev/null; then
            echo "Failed to install SharpFuzz. Please install it manually:"
            echo "dotnet tool install --global SharpFuzz.CommandLine"
            exit 1
        fi
    fi
}

# Function to generate corpus
generate_corpus() {
    local count=${1:-$CORPUS_COUNT}
    echo "Generating corpus files in $CORPUS_DIR (count: $count)..."
    dotnet run -- generate-corpus "$CORPUS_DIR" "$count"
    echo "Corpus generation completed."

    # Create a backup of the initial corpus
    if [ ! -d "${CORPUS_DIR}_initial_backup" ]; then
        echo "Creating backup of initial corpus..."
        cp -r "$CORPUS_DIR" "${CORPUS_DIR}_initial_backup"
    fi
}

# Function to run corpus
run_corpus() {
    echo "Running fuzzer on corpus files in $CORPUS_DIR..."
    dotnet run -- run-corpus "$CORPUS_DIR"
    echo "Corpus run completed."
}

# Function to analyze a test case
analyze_testcase() {
    if [ -z "$ANALYZE_FILE" ]; then
        echo "Error: No file specified for analysis."
        show_help
        exit 1
    fi

    if [ ! -f "$ANALYZE_FILE" ]; then
        echo "Error: File '$ANALYZE_FILE' does not exist."
        exit 1
    fi

    echo "Analyzing test case: $ANALYZE_FILE"
    dotnet run -- analyze "$ANALYZE_FILE"
    echo "Analysis completed."
}

# Function to run continuous fuzzing
run_fuzzing() {
    # Set resource limits to prevent excessive resource consumption
    # Use -S for soft limit which won't fail if not supported on the platform
    ulimit -S -v 8000000 2>/dev/null || echo "Warning: Virtual memory limit could not be set"
    ulimit -S -t 86400 2>/dev/null || echo "Warning: CPU time limit could not be set"

    # Check if corpus is empty and generate initial corpus if needed
    if [ ! "$(ls -A $CORPUS_DIR)" ]; then
        echo "Corpus directory is empty. Generating initial corpus..."
        generate_corpus 100
    fi

    # Create a timestamp for this fuzzing session
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    SESSION_DIR="$REPORTS_DIR/session_$TIMESTAMP"
    mkdir -p "$SESSION_DIR"

    # Save configuration for this session
    CONFIG_FILE="$SESSION_DIR/config.txt"
    echo "DBFT Fuzzing Session Configuration" > "$CONFIG_FILE"
    echo "================================" >> "$CONFIG_FILE"
    echo "Started: $(date)" >> "$CONFIG_FILE"
    echo "Corpus directory: $CORPUS_DIR" >> "$CONFIG_FILE"
    echo "Findings directory: $FINDINGS_DIR" >> "$CONFIG_FILE"
    echo "Crashes directory: $CRASHES_DIR" >> "$CONFIG_FILE"
    echo "Dictionary file: $DICT_FILE" >> "$CONFIG_FILE"
    echo "Max input length: $MAX_LEN" >> "$CONFIG_FILE"
    echo "Timeout: $TIMEOUT seconds" >> "$CONFIG_FILE"
    echo "Workers: $WORKERS" >> "$CONFIG_FILE"
    echo "Jobs: $JOBS" >> "$CONFIG_FILE"
    echo "Runs: $RUNS" >> "$CONFIG_FILE"
    echo "Build configuration: $BUILD_CONFIG" >> "$CONFIG_FILE"
    echo "System: $(uname -a)" >> "$CONFIG_FILE"
    echo "" >> "$CONFIG_FILE"

    # Run the fuzzer with SharpFuzz
    echo "Starting DBFT consensus fuzzer..."
    echo "Configuration:"
    echo "  Corpus directory: $CORPUS_DIR"
    echo "  Findings directory: $FINDINGS_DIR"
    echo "  Crashes directory: $CRASHES_DIR"
    echo "  Dictionary file: $DICT_FILE"
    echo "  Max input length: $MAX_LEN"
    echo "  Timeout: $TIMEOUT seconds"
    echo "  Workers: $WORKERS"
    echo "  Jobs: $JOBS"
    echo "  Runs: $RUNS"
    echo "  Session directory: $SESSION_DIR"
    echo ""

    # For long-term fuzzing, we'll use a loop with checkpoints
    CHECKPOINT_INTERVAL=${CHECKPOINT_INTERVAL:-3600} # Default: 1 hour
    TOTAL_RUNTIME=${TOTAL_RUNTIME:-0} # 0 means run indefinitely
    START_TIME=$(date +%s)
    LAST_CHECKPOINT=$START_TIME
    RUN_COUNT=0

    # Create a PID file to track the fuzzing process
    echo $$ > "$SESSION_DIR/fuzzer.pid"

    # Trap SIGINT and SIGTERM to handle graceful shutdown
    trap cleanup_and_exit SIGINT SIGTERM

    # Function to handle cleanup on exit
    cleanup_and_exit() {
        echo "\nReceived termination signal. Cleaning up and exiting..."
        END_TIME=$(date +%s)
        TOTAL_RUNTIME=$((END_TIME - START_TIME))

        # Update the session report
        update_session_report "Terminated by user after $TOTAL_RUNTIME seconds"

        # Remove PID file
        rm -f "$SESSION_DIR/fuzzer.pid"

        echo "Fuzzing session terminated after running for $TOTAL_RUNTIME seconds."
        echo "Check '$SESSION_DIR/report.txt' for session summary."
        exit 0
    }

    # Function to update the session report
    update_session_report() {
        local status="$1"
        local report_file="$SESSION_DIR/report.txt"

        echo "DBFT Fuzzing Session Report" > "$report_file"
        echo "=========================" >> "$report_file"
        echo "Started: $(date -r $START_TIME)" >> "$report_file"
        echo "Last updated: $(date)" >> "$report_file"
        echo "Status: $status" >> "$report_file"
        echo "" >> "$report_file"

        echo "Runtime Statistics:" >> "$report_file"
        echo "------------------" >> "$report_file"
        local current_time=$(date +%s)
        local elapsed=$((current_time - START_TIME))
        echo "Total runtime: $elapsed seconds" >> "$report_file"
        echo "Checkpoints: $RUN_COUNT" >> "$report_file"
        echo "" >> "$report_file"

        echo "Corpus Statistics:" >> "$report_file"
        echo "------------------" >> "$report_file"
        echo "Initial corpus size: $(find "${CORPUS_DIR}_initial_backup" -type f 2>/dev/null | wc -l) files" >> "$report_file"
        echo "Current corpus size: $(find "$CORPUS_DIR" -type f | wc -l) files" >> "$report_file"
        echo "" >> "$report_file"

        echo "Findings:" >> "$report_file"
        echo "---------" >> "$report_file"
        echo "Total findings: $(find "$FINDINGS_DIR" -type f | wc -l) files" >> "$report_file"
        echo "" >> "$report_file"

        echo "Crashes:" >> "$report_file"
        echo "--------" >> "$report_file"
        local crash_count=$(find "$CRASHES_DIR" -type f | wc -l)
        echo "Total crashes: $crash_count" >> "$report_file"

        if [ "$crash_count" -gt 0 ]; then
            echo "" >> "$report_file"
            echo "Crash files:" >> "$report_file"
            find "$CRASHES_DIR" -type f -exec basename {} \; | sort >> "$report_file"
        fi
    }

    # Main fuzzing loop for long-term operation
    while true; do
        # Check if we've reached the total runtime limit
        if [ "$TOTAL_RUNTIME" -gt 0 ]; then
            CURRENT_TIME=$(date +%s)
            if [ $((CURRENT_TIME - START_TIME)) -ge "$TOTAL_RUNTIME" ]; then
                echo "Reached total runtime limit of $TOTAL_RUNTIME seconds."
                update_session_report "Completed (reached time limit)"
                break
            fi
        fi

        # Calculate the number of runs for this iteration
        # For long-term fuzzing, we use smaller run counts per iteration
        local iteration_runs=$((RUNS / 10))
        if [ "$iteration_runs" -lt 1000 ]; then
            iteration_runs=1000
        fi

        echo "Starting fuzzing iteration $((RUN_COUNT + 1)) with $iteration_runs runs..."

        # Run the fuzzer for this iteration
        sharpfuzz \
          "$DLL_PATH" \
          Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core.FuzzConsensus \
          Fuzz \
          -dict="$DICT_FILE" \
          -max_len="$MAX_LEN" \
          -timeout="$TIMEOUT" \
          -workers="$WORKERS" \
          -jobs="$JOBS" \
          -runs="$iteration_runs" \
          -artifact_prefix="$CRASHES_DIR/" \
          -i "$CORPUS_DIR" \
          -o "$FINDINGS_DIR" \
          -print_final_stats=1 \
          -use_value_profile=1 \
          -shrink=1 \
          -reduce_inputs=1 \
          -detect_leaks=1 \
          -reload=1 \
          -handle_segv=1 \
          -handle_bus=1 \
          -handle_abrt=1 \
          -handle_ill=1 \
          -handle_fpe=1 \
          -handle_int=1 \
          -handle_term=1 \
          -handle_xfsz=1 \
          -handle_usr1=1 \
          -handle_usr2=1 \
          2>&1 | tee -a "$SESSION_DIR/fuzzer_log_$RUN_COUNT.txt"

        # Increment run count
        RUN_COUNT=$((RUN_COUNT + 1))

        # Check if we need to create a checkpoint
        CURRENT_TIME=$(date +%s)
        if [ $((CURRENT_TIME - LAST_CHECKPOINT)) -ge "$CHECKPOINT_INTERVAL" ]; then
            echo "Creating checkpoint after $((CURRENT_TIME - START_TIME)) seconds of fuzzing..."

            # Create a backup of the current corpus
            CHECKPOINT_DIR="$SESSION_DIR/checkpoint_$RUN_COUNT"
            mkdir -p "$CHECKPOINT_DIR"
            cp -r "$CORPUS_DIR" "$CHECKPOINT_DIR/corpus"
            cp -r "$FINDINGS_DIR" "$CHECKPOINT_DIR/findings"

            # Update the session report
            update_session_report "Running (checkpoint $RUN_COUNT)"

            # Update the last checkpoint time
            LAST_CHECKPOINT=$CURRENT_TIME

            echo "Checkpoint created at $CHECKPOINT_DIR"
        fi

        # Brief pause between iterations to allow system to stabilize
        sleep 5
    done

    # Final update to the session report
    update_session_report "Completed"

    echo "Fuzzing completed. Check '$SESSION_DIR/report.txt' for session summary."
}

# Function to generate a report
generate_report() {
    REPORT_FILE="$REPORTS_DIR/report_$(date +%Y%m%d_%H%M%S).txt"

    echo "Generating fuzzing report..."
    echo "Neo DBFT Consensus Fuzzing Report" > "$REPORT_FILE"
    echo "Generated: $(date)" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"

    echo "Corpus Statistics:" >> "$REPORT_FILE"
    echo "----------------" >> "$REPORT_FILE"
    echo "Total corpus files: $(find "$CORPUS_DIR" -type f | wc -l)" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"

    echo "Findings:" >> "$REPORT_FILE"
    echo "---------" >> "$REPORT_FILE"
    echo "Total findings: $(find "$FINDINGS_DIR" -type f | wc -l)" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"

    echo "Crashes:" >> "$REPORT_FILE"
    echo "--------" >> "$REPORT_FILE"
    CRASH_COUNT=$(find "$CRASHES_DIR" -type f | wc -l)
    echo "Total crashes: $CRASH_COUNT" >> "$REPORT_FILE"

    if [ "$CRASH_COUNT" -gt 0 ]; then
        echo "" >> "$REPORT_FILE"
        echo "Crash details:" >> "$REPORT_FILE"
        for crash in "$CRASHES_DIR"/*; do
            if [ -f "$crash" ]; then
                echo "- $(basename "$crash")" >> "$REPORT_FILE"
                # Optionally analyze each crash and include results
                # dotnet run -- analyze "$crash" >> "$REPORT_FILE" 2>&1
            fi
        done
    fi

    echo "" >> "$REPORT_FILE"
    echo "Report saved to: $REPORT_FILE"
    cat "$REPORT_FILE"
}

# Function to clean up
clean_up() {
    echo "Cleaning up temporary files..."

    read -p "Do you want to delete all findings? (y/N) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -rf "$FINDINGS_DIR"/*
        echo "Findings deleted."
    fi

    read -p "Do you want to delete all crashes? (y/N) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -rf "$CRASHES_DIR"/*
        echo "Crashes deleted."
    fi

    read -p "Do you want to delete the corpus? (y/N) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -rf "$CORPUS_DIR"/*
        echo "Corpus deleted."
    fi

    read -p "Do you want to delete all reports? (y/N) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -rf "$REPORTS_DIR"/*
        echo "Reports deleted."
    fi

    echo "Cleanup completed."
}

# Main execution
parse_args "$@"
setup_dirs

case "$COMMAND" in
    help)
        show_help
        ;;
    generate-corpus)
        generate_corpus
        ;;
    run-corpus)
        run_corpus
        ;;
    analyze)
        analyze_testcase
        ;;
    report)
        generate_report
        ;;
    clean)
        clean_up
        ;;
    fuzz)
        build_project
        check_sharpfuzz
        run_fuzzing
        ;;
    *)
        echo "Unknown command: $COMMAND"
        show_help
        exit 1
        ;;
esac

exit 0

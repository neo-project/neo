#!/bin/bash

# Neo N3 VM OpCode Comprehensive Benchmark Suite Runner
# This script runs all opcode benchmarks and generates individual reports

set -e

echo "═══════════════════════════════════════════════════════"
echo "    Neo N3 VM OpCode Comprehensive Benchmark Suite"
echo "═══════════════════════════════════════════════════════"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: dotnet CLI not found. Please install .NET SDK."
    exit 1
fi

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "📁 Project root: $PROJECT_ROOT"
echo "📁 Benchmarks directory: $SCRIPT_DIR"
echo ""

# Build project in Release mode
echo "🔨 Building benchmark project in Release mode..."
cd "$PROJECT_ROOT"
dotnet build benchmarks/Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj -c Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please fix compilation errors."
    exit 1
fi

echo "✅ Build successful"
echo ""

# Run benchmarks
echo "🚀 Running comprehensive BenchmarkDotNet suite..."
echo "   This may take a while depending on your system..."
echo ""

cd "$SCRIPT_DIR"
dotnet run -c Release --framework net9.0 --project Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj

if [ $? -eq 0 ]; then
    echo ""
    echo "═══════════════════════════════════════════════════════"
    echo "✅ Benchmark suite completed successfully!"
    echo "═══════════════════════════════════════════════════════"
    echo ""
    echo "📊 Results location:"
    echo "   BenchmarkDotNet artifacts: $SCRIPT_DIR/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/"
    echo ""
    echo "To re-run a specific benchmark, pass '-- --filter <BenchmarkName>' after the project path."
    echo ""
else
    echo ""
    echo "❌ Benchmark suite failed. Check error messages above."
    exit 1
fi

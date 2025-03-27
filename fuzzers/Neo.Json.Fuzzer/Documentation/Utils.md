# Neo.Json.Fuzzer - Utility Components

## Overview

The Neo.Json.Fuzzer utility components provide essential supporting functionality for the fuzzing process. These utilities handle corpus management, coverage tracking, DOS detection, and statistics collection, forming the backbone of the fuzzing infrastructure.

## Key Components

### 1. CorpusManager

The `CorpusManager` handles the collection of JSON test cases used for fuzzing:

- **Corpus Management**: Loads, stores, and manages the corpus of JSON test cases
- **Minimal Corpus Generation**: Creates a basic corpus if none is provided
- **Interesting Case Detection**: Saves JSON inputs that find new coverage
- **Crash Reporting**: Saves JSON inputs that cause crashes
- **DOS Vector Tracking**: Saves JSON inputs that are potential DOS vectors

### 2. CoverageTracker

The `CoverageTracker` monitors and analyzes code coverage during fuzzing:

- **Coverage Point Tracking**: Records unique coverage points
- **Coverage Analysis**: Calculates coverage statistics and percentages
- **Report Generation**: Creates detailed coverage reports
- **Interesting Case Detection**: Identifies inputs that exercise new code paths

### 3. DOSDetector

The `DOSDetector` identifies potential Denial of Service vectors:

- **Performance Analysis**: Analyzes execution time and memory usage
- **Complexity Analysis**: Evaluates input structure and complexity
- **Threshold-Based Detection**: Uses configurable thresholds to identify DOS vectors
- **Detailed Analysis**: Provides comprehensive information about detected DOS vectors

### 4. FuzzingStatistics

The `FuzzingStatistics` tracks and reports on the fuzzing process:

- **Run Tracking**: Counts successful, crashed, and timed-out runs
- **Performance Monitoring**: Tracks execution time and memory usage
- **Exception Analysis**: Categorizes and counts exceptions
- **Progress Reporting**: Provides real-time progress updates
- **Report Generation**: Creates detailed statistical reports

## Implementation Details

### CorpusManager

```csharp
public class CorpusManager
{
    public CorpusManager(string outputDir, string? corpusDir = null);
    public void LoadCorpus();
    public string GetRandomJson();
    public void SaveCrash(string json, string? exceptionType);
    public void SaveInteresting(string json);
    public void SaveDOSVector(string json, DOSAnalysisResult analysis);
}
```

### CoverageTracker

```csharp
public class CoverageTracker
{
    public CoverageTracker(string outputDir, bool verbose = false, int estimatedTotalPoints = 100);
    public bool RecordCoverage(List<string> coveragePoints);
    public void UpdateCoveragePercentage(int estimatedTotalPoints);
    public void SaveCoverageReport();
    public void PrintSummary();
}
```

### DOSDetector

```csharp
public class DOSDetector
{
    public DOSDetector(double threshold = 0.8, bool trackMemory = false);
    public void Reset();
    public DOSAnalysisResult Analyze(Dictionary<string, double> metrics);
}

public class DOSAnalysisResult
{
    public double DOSScore { get; set; }
    public bool IsPotentialDOSVector { get; set; }
    public string? DetectionReason { get; set; }
    public Dictionary<string, double> Metrics { get; set; }
}
```

### FuzzingStatistics

```csharp
public class FuzzingStatistics
{
    public FuzzingStatistics(string outputDir, bool verbose = false);
    public void RecordResult(JsonExecutionResult result);
    public void PrintProgressReport();
    public void SaveStatistics();
}
```

## Usage Examples

### CorpusManager

```csharp
// Initialize corpus manager
var corpusManager = new CorpusManager(outputDir: "./output", corpusDir: "./corpus");

// Load initial corpus
corpusManager.LoadCorpus();

// Get a random JSON for fuzzing
string json = corpusManager.GetRandomJson();

// Save interesting cases
if (foundNewCoverage)
{
    corpusManager.SaveInteresting(json);
}

// Save crashes
if (result.Crashed)
{
    corpusManager.SaveCrash(json, result.ExceptionType);
}

// Save DOS vectors
if (result.DOSAnalysis?.IsPotentialDOSVector == true)
{
    corpusManager.SaveDOSVector(json, result.DOSAnalysis);
}
```

### CoverageTracker

```csharp
// Initialize coverage tracker
var coverageTracker = new CoverageTracker(outputDir: "./output", verbose: true);

// Record coverage from a run
bool foundNewCoverage = coverageTracker.RecordCoverage(result.Coverage);

// Update coverage percentage
coverageTracker.UpdateCoveragePercentage(estimatedTotalPoints: 150);

// Print coverage summary
coverageTracker.PrintSummary();

// Save coverage report
coverageTracker.SaveCoverageReport();
```

### DOSDetector

```csharp
// Initialize DOS detector
var dosDetector = new DOSDetector(threshold: 0.8, trackMemory: true);

// Analyze metrics for DOS detection
var metrics = new Dictionary<string, double>
{
    ["ExecutionTimeMs"] = result.ExecutionTimeMs,
    ["MemoryUsageBytes"] = result.MemoryUsageBytes,
    ["InputLength"] = json.Length,
    ["NestingDepth"] = CalculateNestingDepth(json)
};

var dosAnalysis = dosDetector.Analyze(metrics);

// Check if it's a potential DOS vector
if (dosAnalysis.IsPotentialDOSVector)
{
    Console.WriteLine($"Potential DOS vector detected: {dosAnalysis.DetectionReason}");
}
```

### FuzzingStatistics

```csharp
// Initialize fuzzing statistics
var statistics = new FuzzingStatistics(outputDir: "./output", verbose: true);

// Record result from a run
statistics.RecordResult(result);

// Print progress report
statistics.PrintProgressReport();

// Save statistics report
statistics.SaveStatistics();
```

## Integration with Other Components

- **Program**: Uses all utility components to manage the fuzzing process
- **JsonRunner**: Provides execution results for the utility components to analyze
- **JsonGenerator**: Creates inputs that are managed by the CorpusManager
- **MutationEngine**: Mutates inputs from the CorpusManager

## Future Enhancements

- **Advanced Corpus Management**: Implement genetic algorithms for corpus evolution
- **Detailed Coverage Analysis**: Add branch and path coverage tracking
- **Advanced DOS Detection**: Implement machine learning-based DOS detection
- **Real-Time Visualization**: Add real-time visualization of fuzzing progress

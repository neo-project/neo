# DOS Detection in Neo.Json.Fuzzer

## Overview

The Denial of Service (DOS) detection component identifies potential vulnerabilities in the Neo.Json library that could be exploited to cause excessive resource consumption or processing time. This document outlines the approach, metrics, and implementation details for DOS detection in the Neo.Json.Fuzzer.

## Detection Approach

The DOS detector monitors several key metrics during JSON parsing and processing:

1. **Execution Time**: Total time spent parsing and processing JSON input
2. **Memory Usage**: Peak memory consumption during parsing
3. **Parsing Complexity**: Metrics related to the complexity of parsing operations
4. **Resource Utilization**: CPU and other resource usage patterns

## Key Metrics

The following metrics are tracked and analyzed to identify potential DOS vectors:

### 1. Time-Based Metrics

- **Total Parsing Time**: Overall time to parse the JSON input
- **Operation Time Distribution**: Time spent in different parsing operations
- **Slow Operations**: Identification of particularly slow parsing operations

### 2. Memory-Based Metrics

- **Peak Memory Usage**: Maximum memory allocated during parsing
- **Memory Growth Rate**: How quickly memory usage increases during parsing
- **Object Count**: Number of objects created during parsing

### 3. Complexity Metrics

- **Nesting Depth**: Maximum depth of the JSON structure
- **Structure Size**: Number of elements in arrays and objects
- **String Length**: Length of string values
- **Property Count**: Number of properties in objects

## Detection Thresholds

DOS detection uses configurable thresholds for each metric:

- **Time Threshold**: Default 10ms (adjustable)
- **Memory Threshold**: Default 10MB (adjustable)
- **Nesting Threshold**: Default 32 levels (adjustable)
- **Size Threshold**: Default 1000 elements (adjustable)

## DOS Score Calculation

The DOS score is calculated using a weighted formula that considers all metrics:

```
DOSScore = (TimeScore * 0.4) + (MemoryScore * 0.3) + (ComplexityScore * 0.3)
```

Where each component score is normalized to a 0.0-1.0 range based on its threshold.

## Implementation Details

The DOS detector is implemented with the following components:

1. **Metric Collectors**: Gather raw data during parsing operations
2. **Analyzers**: Process raw data to identify patterns and anomalies
3. **Scorers**: Calculate DOS scores based on analyzed metrics
4. **Reporters**: Generate detailed reports on potential DOS vectors

## Integration with Neo.Json

The DOS detector instruments the Neo.Json parsing process by:

1. Wrapping the `JToken.Parse()` methods to measure execution time
2. Monitoring memory allocations during parsing
3. Tracking the structure of the parsed JSON
4. Analyzing the relationship between input characteristics and resource usage

## Example DOS Vectors

Known patterns that may trigger DOS conditions in JSON parsers:

1. **Deep Nesting**: Extremely deep nesting of arrays or objects
2. **Repetitive Structures**: Highly repetitive structures that may trigger inefficient parsing
3. **Large Numbers**: Very large numeric values that may cause precision issues
4. **Complex Unicode**: Complex Unicode sequences that may slow down string processing
5. **Pathological Structures**: Structures specifically designed to trigger worst-case performance

## Reporting

When a potential DOS vector is identified, the detector generates a detailed report including:

1. The input that triggered the detection
2. The specific metrics that exceeded thresholds
3. A breakdown of resource usage during parsing
4. Recommendations for mitigating the vulnerability

## Limitations

The current DOS detection has the following limitations:

1. May not detect all possible DOS vectors
2. Some false positives may occur on resource-constrained systems
3. Detection is focused on parsing operations, not subsequent JSON manipulation
4. Thresholds may need adjustment based on the deployment environment

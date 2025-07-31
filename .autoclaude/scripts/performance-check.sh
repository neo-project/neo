#!/bin/bash
# Performance Check
# Analyzes performance bottlenecks and optimization opportunities

RESULT_FILE="/tmp/performance_check_result.json"

# Initialize result
echo '{"passed": true, "errors": [], "warnings": []}' > "$RESULT_FILE"

# Function to add error
add_error() {
    local error="$1"
    python3 -c "
import json
with open('$RESULT_FILE', 'r') as f:
    result = json.load(f)
result['passed'] = False
result['errors'].append('$error')
with open('$RESULT_FILE', 'w') as f:
    json.dump(result, f)
"
}

# Function to add warning
add_warning() {
    local warning="$1"
    python3 -c "
import json
with open('$RESULT_FILE', 'r') as f:
    result = json.load(f)
result['warnings'].append('$warning')
with open('$RESULT_FILE', 'w') as f:
    json.dump(result, f)
"
}

# Check for nested loops (O(n²) complexity)
NESTED_LOOPS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -c "for.*{.*for\|while.*{.*while" | grep -v ":0" | wc -l)
if [ "$NESTED_LOOPS" -gt 5 ]; then
    add_warning "Multiple nested loops detected - may cause performance issues"
fi

# Check for synchronous file operations
SYNC_OPS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -c "readFileSync\|writeFileSync" | grep -v ":0" | wc -l)
if [ "$SYNC_OPS" -gt 0 ]; then
    add_error "Synchronous file operations found - use async alternatives"
fi

# Check for large bundle dependencies
if [ -f "package.json" ]; then
    LARGE_DEPS=$(grep -E "lodash|moment|react|vue|angular" package.json | wc -l)
    if [ "$LARGE_DEPS" -gt 0 ]; then
        add_warning "Large dependencies detected - consider bundle size optimization"
    fi
fi

# Check for missing caching
CACHE_USAGE=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -c "cache\|redis\|memcache" | grep -v ":0" | wc -l)
if [ "$CACHE_USAGE" -eq 0 ]; then
    add_warning "No caching implementation found - consider adding caching for performance"
fi

# Output result
cat "$RESULT_FILE"

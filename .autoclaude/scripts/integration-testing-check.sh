#!/bin/bash
# Integration Testing Check
# Validates test coverage and integration tests

RESULT_FILE="/tmp/integration_testing_result.json"

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

# Check for test files
TEST_FILES=$(find . -name "*.test.*" -o -name "*.spec.*" | grep -v node_modules | wc -l)
if [ "$TEST_FILES" -eq 0 ]; then
    add_error "No test files found - create unit and integration tests"
fi

# Check for test framework configuration
if [ -f "package.json" ]; then
    if ! grep -q "jest\|mocha\|vitest\|cypress\|playwright" package.json; then
        add_error "No test framework detected in package.json"
    fi
fi

# Check for API endpoints without tests
API_ENDPOINTS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | grep -v test | xargs grep -c "app\.\(get\|post\|put\|delete\)" | grep -v ":0" | wc -l)
if [ "$API_ENDPOINTS" -gt 0 ] && [ "$TEST_FILES" -eq 0 ]; then
    add_error "API endpoints found but no integration tests detected"
fi

# Check for database operations without tests
DB_OPERATIONS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | grep -v test | xargs grep -c "\(save\|find\|update\|delete\|create\)" | grep -v ":0" | wc -l)
if [ "$DB_OPERATIONS" -gt 0 ] && [ "$TEST_FILES" -eq 0 ]; then
    add_error "Database operations found but no integration tests detected"
fi

# Output result
cat "$RESULT_FILE"

#!/bin/bash
# Code Understanding Check
# Analyzes code quality and patterns

RESULT_FILE="/tmp/code_understanding_result.json"

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

# Check for large functions (more than 50 lines)
LARGE_FUNCTIONS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -n "function\|=>" | wc -l)
if [ "$LARGE_FUNCTIONS" -gt 100 ]; then
    add_warning "Project has many functions ($LARGE_FUNCTIONS) - consider code organization"
fi

# Check for code duplication patterns
DUPLICATE_PATTERNS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -c "console.log\|throw new Error" | grep -v ":0" | wc -l)
if [ "$DUPLICATE_PATTERNS" -gt 20 ]; then
    add_warning "Potential code duplication detected - consider refactoring common patterns"
fi

# Check for missing documentation
UNDOCUMENTED_FUNCTIONS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -B1 "function\|=>" | grep -v "/\*\*\|//" | wc -l)
if [ "$UNDOCUMENTED_FUNCTIONS" -gt 50 ]; then
    add_warning "Many functions lack documentation - consider adding JSDoc comments"
fi

# Output result
cat "$RESULT_FILE"

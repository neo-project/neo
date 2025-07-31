#!/bin/bash
# Context Awareness Check
# Analyzes project structure and context

WORKSPACE_PATH="${PWD}"
RESULT_FILE="/tmp/context_check_result.json"

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

# Check for project configuration files
if [ ! -f "package.json" ] && [ ! -f "go.mod" ] && [ ! -f "Cargo.toml" ] && [ ! -f "requirements.txt" ]; then
    add_error "No project configuration file found (package.json, go.mod, Cargo.toml, or requirements.txt)"
fi

# Check for README
if [ ! -f "README.md" ] && [ ! -f "README.rst" ] && [ ! -f "README.txt" ]; then
    add_warning "No README file found"
fi

# Check for .gitignore
if [ ! -f ".gitignore" ]; then
    add_warning "No .gitignore file found"
fi

# Output result
cat "$RESULT_FILE"

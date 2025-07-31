#!/bin/bash
# Dependency Resolution Check
# Checks for missing dependencies and security issues

RESULT_FILE="/tmp/dependency_check_result.json"

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

# Check for Node.js project
if [ -f "package.json" ]; then
    # Check if node_modules exists
    if [ ! -d "node_modules" ]; then
        add_error "node_modules directory not found - run npm install"
    fi
    
    # Check for package-lock.json
    if [ ! -f "package-lock.json" ] && [ ! -f "yarn.lock" ]; then
        add_error "No package-lock.json or yarn.lock found - dependencies may be inconsistent"
    fi
    
    # Check for security vulnerabilities (if npm is available)
    if command -v npm &> /dev/null; then
        if npm audit --json 2>/dev/null | grep -q '"vulnerabilities"'; then
            VULN_COUNT=$(npm audit --json 2>/dev/null | python3 -c "
import json, sys
try:
    data = json.load(sys.stdin)
    if 'metadata' in data and 'vulnerabilities' in data['metadata']:
        print(data['metadata']['vulnerabilities']['total'])
    else:
        print(0)
except:
    print(0)
")
            if [ "$VULN_COUNT" -gt 0 ]; then
                add_error "Found $VULN_COUNT security vulnerabilities in dependencies"
            fi
        fi
    fi
fi

# Check for Python project
if [ -f "requirements.txt" ]; then
    # Check for missing packages
    if command -v pip &> /dev/null; then
        pip check 2>/dev/null || add_error "Python dependency conflicts detected"
    fi
fi

# Check for Go project
if [ -f "go.mod" ]; then
    if command -v go &> /dev/null; then
        go mod verify 2>/dev/null || add_error "Go module verification failed"
    fi
fi

# Output result
cat "$RESULT_FILE"

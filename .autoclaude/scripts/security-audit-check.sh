#!/bin/bash
# Security Audit Check
# Scans for security vulnerabilities and best practices

RESULT_FILE="/tmp/security_audit_result.json"

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

# Check for hardcoded secrets
HARDCODED_SECRETS=$(find . -name "*.ts" -o -name "*.js" -o -name "*.py" | grep -v node_modules | xargs grep -i "password\s*=\s*['"]\|api[_-]\?key\s*=\s*['"]\|secret\s*=\s*['"]" | wc -l)
if [ "$HARDCODED_SECRETS" -gt 0 ]; then
    add_error "Hardcoded secrets detected - move to environment variables"
fi

# Check for SQL injection vulnerabilities
SQL_INJECTION=$(find . -name "*.ts" -o -name "*.js" -o -name "*.py" | grep -v node_modules | xargs grep -c "query.*+\|execute.*+" | grep -v ":0" | wc -l)
if [ "$SQL_INJECTION" -gt 0 ]; then
    add_error "Potential SQL injection vulnerabilities - use parameterized queries"
fi

# Check for XSS vulnerabilities
XSS_VULNS=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -c "innerHTML.*+\|document.write" | grep -v ":0" | wc -l)
if [ "$XSS_VULNS" -gt 0 ]; then
    add_error "Potential XSS vulnerabilities - sanitize user input"
fi

# Check for insecure HTTP usage
HTTP_USAGE=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -c "http://" | grep -v ":0" | wc -l)
if [ "$HTTP_USAGE" -gt 0 ]; then
    add_error "Insecure HTTP URLs found - use HTTPS"
fi

# Check for weak authentication
WEAK_AUTH=$(find . -name "*.ts" -o -name "*.js" | grep -v node_modules | xargs grep -c "password.*===\|token.*===" | grep -v ":0" | wc -l)
if [ "$WEAK_AUTH" -gt 0 ]; then
    add_error "Weak authentication patterns detected"
fi

# Check for npm audit (if available)
if [ -f "package.json" ] && command -v npm &> /dev/null; then
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

# Output result
cat "$RESULT_FILE"

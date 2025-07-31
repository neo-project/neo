# AutoClaude Scripts

This directory contains quality check scripts that can be run automatically by AutoClaude.

## Built-in Scripts

### production-readiness.sh
Checks for TODO comments, FIXME markers, placeholders, and other indicators of incomplete code.

### build-check.sh
Ensures the project can build successfully using detected build tools.

### test-check.sh
Runs the project's test suite and ensures all tests pass.

### format-check.sh
Checks code formatting using detected linters and formatters.

### github-actions.sh
Validates GitHub Actions workflow files for syntax and common issues.

## Creating Custom Scripts

You can create your own shell scripts in this directory. They must:

1. Be executable (chmod +x your-script.sh)
2. Output JSON in the following format:

```json
{
  "passed": true|false,
  "errors": ["error 1", "error 2"],
  "warnings": ["warning 1", "warning 2"],
  "fixInstructions": "Instructions for fixing the issues"
}
```

## Script Requirements

- Must be shell scripts (bash)
- Must be executable
- Must output valid JSON
- Should exit with code 0 (success) regardless of check results
- The JSON "passed" field determines if the check passed

## Example Custom Script

```bash
#!/bin/bash

# My custom check
errors=()

# Perform checks...
if [ some_condition ]; then
    errors+=("Found an issue")
fi

# Output JSON
echo "{"
echo "  \"passed\": $([ ${#errors[@]} -eq 0 ] && echo "true" || echo "false"),"
echo "  \"errors\": ["
if [ ${#errors[@]} -gt 0 ]; then
    printf '%s\n' "${errors[@]}" | jq -R . | jq -s .
fi
echo "  ],"
echo "  \"fixInstructions\": \"Fix the issues found\""
echo "}"
```

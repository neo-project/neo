#!/bin/bash
file="neo.UnitTests/bin/Debug/netcoreapp2.0/Neo.UnitTests.dll"
if [ -f "$file" ]
then
	coverlet $file --target "dotnet" --targetargs "test neo.UnitTests/ --no-build" -f opencover
	reportgenerator -reports:coverage.opencover.xml -targetdir:coveragereport/
else
	echo "$file not found, you may need to build the project or change the file location."
fi


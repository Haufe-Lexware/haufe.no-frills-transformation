#!/bin/bash

# This script is used to release a new version of the project.
set -e

# Make sure LEXBIZZ_PAT is set
if [ -z "$LEXBIZZ_PAT" ]; then
    # Attempt to read .env if it's there
    if [ -f .env ]; then
        source .env
    fi
fi

if [ -z "$LEXBIZZ_PAT" ]; then
    echo "LEXBIZZ_PAT is not set (and/or no .env file was found)"
    exit 1
fi

# Check the first parameter, if it's not set, output a usage and exit
if [ -z "$1" ]; then
    echo "Usage: $0 <version>"
    exit 1
fi

# Remember the version
version=$1

# Make sure our cwd is the directory of the script
pushd "$(dirname "$0")"

# Replace the Version in the Directory.Build.props file
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s|<Version>.*</Version>|<Version>$version</Version>|" Directory.Build.props
else
    # Linux
    sed -i "s|<Version>.*</Version>|<Version>$version</Version>|" Directory.Build.props
fi

dotnet clean
dotnet build

rm -rf ../nupkg

# Bump the version to the version given in the parameter
dotnet pack -c Release -p:PackageVersion=$version
#dotnet nuget push -s lexbizz -k $LEXBIZZ_PAT ../nupkg/*.nupkg

popd

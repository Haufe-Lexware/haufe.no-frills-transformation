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

# Make sure our cwd is the directory of the script
pushd "$(dirname "$0")"

dotnet clean
dotnet build

rm -rf ../nupkg

dotnet pack
dotnet nuget push -s lexbizz -k $LEXBIZZ_PAT ../nupkg/*.nupkg

popd

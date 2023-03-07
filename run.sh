#!/usr/bin/env bash
set -e
dotnet run --no-launch-profile -f net7.0 --project "$(dirname "$0")/src" -- "$@"

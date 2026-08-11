#!/usr/bin/env sh

set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$SCRIPT_DIR"

if [ ! -x ./Asv.Drones.Gbs ]; then
    printf '%s\n' "Asv.Drones.Gbs executable not found in $SCRIPT_DIR" >&2
    exit 1
fi

DOTNET_ENVIRONMENT=Virtual
export DOTNET_ENVIRONMENT

for config in appsettings.json appsettings.Virtual.json; do
    if [ ! -r "$config" ]; then
        printf '%s\n' "$config not found or not readable in $SCRIPT_DIR" >&2
        exit 1
    fi
done

: "${DOTNET_BUNDLE_EXTRACT_BASE_DIR:=$SCRIPT_DIR/.dotnet-bundle}"
export DOTNET_BUNDLE_EXTRACT_BASE_DIR
mkdir -p "$DOTNET_BUNDLE_EXTRACT_BASE_DIR"

exec ./Asv.Drones.Gbs "$@"

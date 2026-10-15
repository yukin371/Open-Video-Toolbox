#!/usr/bin/env bash

if [ -n "${OVT_GITHOOKS_SHARED_LOADED:-}" ]; then
    return 0
fi

OVT_GITHOOKS_SHARED_LOADED=1

if [ -t 1 ] && [ -z "${NO_COLOR:-}" ]; then
    RED='\033[0;31m'
    YELLOW='\033[1;33m'
    GREEN='\033[0;32m'
    NC='\033[0m'
else
    RED=''
    YELLOW=''
    GREEN=''
    NC=''
fi

: "${HAS_ERROR:=0}"
: "${HAS_WARNING:=0}"

info()  { echo -e "${GREEN}[OK]${NC} $1"; }
warn()  { echo -e "${YELLOW}[!!]${NC} $1"; HAS_WARNING=1; }
error() { echo -e "${RED}[XX]${NC} $1"; HAS_ERROR=1; }

strip_utf8_bom() {
    local value="$1"
    printf '%s' "${value#$'\357\273\277'}"
}

is_text_file() {
    local file_path="$1"

    [ -f "$file_path" ] || return 1
    file "$file_path" 2>/dev/null | grep -q "text"
}

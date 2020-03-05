#!/usr/bin/env bash
set -e
scriptdir=`dirname "$BASH_SOURCE"`
source $scriptdir/secrets.sh
/usr/bin/curl --connect-timeout 1 --request GET --url http://127.0.0.1:7878/v2/server/off?confirm=true\&token=$resttoken

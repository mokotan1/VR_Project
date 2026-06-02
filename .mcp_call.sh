#!/usr/bin/env bash
# Usage: mcp_call.sh <method> '<params-json>'
URL="http://127.0.0.1:8080/mcp"
METHOD="$1"
PARAMS="${2:-{}}"
curl -s -m 15 -D /tmp/h -o /dev/null -X POST "$URL" -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"cc","version":"1.0"}}}'
SID=$(tr -d '\r' < /tmp/h | awk -F': ' 'tolower($1)=="mcp-session-id"{print $2}')
curl -s -m 15 -o /dev/null -X POST "$URL" -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -H "mcp-session-id: $SID" -d '{"jsonrpc":"2.0","method":"notifications/initialized"}'
curl -s -m 90 -N -X POST "$URL" -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -H "mcp-session-id: $SID" -d "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"$METHOD\",\"params\":$PARAMS}" | sed -n 's/^data: //p' | grep '"id":2'

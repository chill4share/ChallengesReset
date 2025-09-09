#!/bin/bash

# lay command line tien trinh
cmdline=$(ps ax | grep '[L]eagueClientUx.exe' | head -n 1)

if [ -z "$cmdline" ]; then
  echo "Loi: khong tim thay tien trinh LeagueClientUx.exe"
  exit 1
fi

# tach port va token (BSD sed cua macOS khong ho tro \+ nen dung *)
port=$(echo "$cmdline" | sed -E 's/.*--app-port=([0-9]*).*/\1/')
token=$(echo "$cmdline" | sed -E 's/.*--remoting-auth-token=([^ ]*).*/\1/')

if [ -z "$port" ] || [ -z "$token" ]; then
  echo "Loi: khong the lay port hoac token"
  exit 1
fi

# ma hoa auth
auth=$(printf "riot:%s" "$token" | base64)

echo "Dang gui yeu cau xoa token..."

# gui request
curl --silent --insecure \
  -H "Authorization: Basic $auth" \
  -H "Content-Type: application/json" \
  -d '{"challengeIds":[]}' \
  "https://127.0.0.1:$port/lol-challenges/v1/update-player-preferences" \
  && echo "Thanh cong! Token thu thach da duoc xoa." \
  || echo "Loi khi gui yeu cau."

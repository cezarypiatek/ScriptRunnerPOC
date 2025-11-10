#!/bin/bash
set -euo pipefail
TEXT_ARG="$1"
FILE_PATH="$2"

echo "[hello] Hello from Bash!"
echo "[info] Text argument : ${TEXT_ARG}"
echo "[info] File argument : ${FILE_PATH}"

if [ ! -f "$FILE_PATH" ]; then
  echo "File '$FILE_PATH' does not exist." >&2
  exit 1
fi

echo "QUESTION: Display the file content? (Yes/No/Maybe)"
read -r answer
answer=$(echo "$answer" | tr -d '\r\n' | tr '[:lower:]' '[:upper:]')

case "$answer" in
  YES)
    echo "[content]"
    while IFS= read -r line; do
      printf '  %s
' "$line"
    done < "$FILE_PATH"
    ;;
  NO)
    echo "[skip] Content display skipped."
    ;;
  MAYBE)
    echo "MAYBE_SELECTED - user could not decide."
    exit 2
    ;;
  *)
    echo "[warn] Unexpected answer '$answer'. Treating as NO."
    ;;
 esac

import argparse
from pathlib import Path
import sys

parser = argparse.ArgumentParser(description="Demo prompt script")
parser.add_argument("text")
parser.add_argument("file_path")
args = parser.parse_args()

print("[hello] Hello from Python!")
print(f"[info] Text argument : {args.text}")
print(f"[info] File argument : {args.file_path}")

file_path = Path(args.file_path)
if not file_path.exists():
    print(f"File '{file_path}' does not exist.", file=sys.stderr)
    sys.exit(1)

print("QUESTION: Display the file content? (Yes/No/Maybe)")
answer = sys.stdin.readline().strip().upper()

if answer == "YES":
    print("[content]")
    for line in file_path.read_text().splitlines():
        print(f"  {line}")
elif answer == "NO":
    print("[skip] Content display skipped.")
elif answer == "MAYBE":
    print("MAYBE_SELECTED - user could not decide.")
    sys.exit(2)
else:
    print(f"[warn] Unexpected answer '{answer}'. Treating as NO.")

import { readFileSync, existsSync } from "fs";

const [textArg, filePath] = process.argv.slice(2);
if (!textArg || !filePath) {
  console.error("Usage: ts-node hello_prompt.ts <text> <filePath>");
  process.exit(1);
}

console.log("[hello] Hello from TypeScript!");
console.log(`[info] Text argument : ${textArg}`);
console.log(`[info] File argument : ${filePath}`);

if (!existsSync(filePath)) {
  console.error(`File '${filePath}' does not exist.`);
  process.exit(1);
}

console.log("QUESTION: Display the file content? (Yes/No/Maybe)");

process.stdin.setEncoding("utf-8");
process.stdin.once("data", (chunk) => {
  const answer = chunk.trim().toUpperCase();
  if (answer === "YES") {
    console.log("[content]");
    readFileSync(filePath, "utf-8")
      .split(/\r?\n/)
      .forEach((line) => console.log(`  ${line}`));
       process.exit(0);
  } else if (answer === "NO") {
    console.log("[skip] Content display skipped.");
    process.exit(1);
  } else if (answer === "MAYBE") {
    console.log("MAYBE_SELECTED - user could not decide.");
    process.exit(0);
  } else {
    console.log(`[warn] Unexpected answer '${answer}'. Treating as NO.`);
  }
});

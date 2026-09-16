# ScriptRunner Prompt Examples

Import `examples/ScriptRunnerExamples.json` in ScriptRunner to get four actions (PowerShell, Python, WSL Bash, and C#). Each action accepts the same parameters:

- `textArgument` – default value `ScriptRunner` shown as regular text input.
- `filePath` – defaults to `examples/sample-data/hello.txt` and is collected via the file picker.

When the script runs it prints `QUESTION: Display the file content? (Yes/No/Maybe)`. ScriptRunner detects this line and renders three interactive buttons so you can respond without typing. Choosing **Maybe** causes every implementation to print `MAYBE_SELECTED`, which our manifest flags with the `troubleshooting` feature so you get an inline warning banner.

## VT output showcase

Import `examples/ScriptRunnerVtFeatures.json` to add the **PowerShell VT Feature Showcase** action. It runs `powershell/VtFeatures.ps1` and demonstrates the supported SGR attributes, standard/bright/indexed/RGB colors, smooth true-color gradients, UTF-8 box-drawing and multilingual glyphs, OSC 8 hyperlinks, automatic URL and local-path links, horizontal cursor editing, erase operations, tabs, backspaces, carriage returns, and safely ignored control strings.

The same manifest also adds **PowerShell Color Output Methods**. It runs `powershell/ColorOutputMethods.ps1` and compares host-managed `Write-Host -ForegroundColor` output with explicit SGR sequences written through both `Write-Host` and `[Console]::WriteLine`.

Use these samples as templates for your own automations—swap the command, keep the interactive inputs and troubleshooting definitions, and you have a fully instrumented action.

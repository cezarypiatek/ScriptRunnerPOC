# ScriptRunner Prompt Types & Settings

Every action parameter in `ScriptRunnerSchema.json` declares a `prompt` that drives which control is rendered in the GUI. This guide explains each prompt type, the available `promptSettings`, and how ScriptRunner interprets the collected values. Use it as a cookbook when authoring new script configurations.

> Cross-cutting tips
>
> * All parameters support `default`, `valueGeneratorCommand`, `valueGeneratorLabel`, `autoParameterBuilderPattern`, and `skipFromAutoParameterBuilder` in addition to the prompt-specific settings below.
> * Relative paths (commands, `valueGeneratorCommand`, `filePicker`, `directoryPicker`, `fileContent`, `docs`, etc.) are resolved against the JSON file’s folder before execution.
> * Password parameters can reference secrets stored in the Vault by saving defaults as `!!vault:MySecret`.

---

## `text`

Free-form single-line input, ideal for names, IDs, or short flags.

**Prompt settings**: _none_

**Example**

```json
{
  "name": "branchName",
  "description": "Git branch to create",
  "default": "feature/",
  "prompt": "text"
}
```

---

## `password`

Masked single-line input with optional Vault binding. When the user picks a Vault entry, future defaults are set to this specific entry.

**Prompt settings**: _none_

**Example**

```json
{
  "name": "githubToken",
  "description": "Github personal access token",
  "prompt": "password"
}
```

---

## `dropdown`

Single-select dropdown. Options can be static or generated on demand.

| Setting | Type | Description |
| --- | --- | --- |
| `options` | string \| string[] \| {label,value}[] | Comma-separated string, array of raw values, or array of `{ "label": "...", "value": "..." }`. |
| `searchable` | boolean | Renders an autocomplete combo box instead of a standard dropdown. |
| `optionsGeneratorCommand` | string | Command executed when the refresh button is clicked (or when the searchable control receives focus). Output is split by newline/`delimiter`. |
| `delimiter` | string | Overrides the comma delimiter when parsing the `options` string or generator output. |

**Examples**

*Comma-separated string*

```json
{
  "name": "awsRegion",
  "description": "Region to deploy into",
  "prompt": "dropdown",
  "promptSettings": {
    "options": "us-east-1,us-west-2,eu-central-1"
  }
}
```

*Array of strings*

```json
{
  "name": "runtime",
  "description": ".NET target framework",
  "default": "net8.0",
  "prompt": "dropdown",
  "promptSettings": {
    "options": [ "net8.0", "net7.0", "net6.0" ]
  }
}
```

*Array of label/value objects*

```json
{
  "name": "subscription",
  "description": "Azure subscription to deploy into",
  "prompt": "dropdown",
  "promptSettings": {
    "options": [
      { "label": "Prod Subscription", "value": "1234-5678-ABCD" },
      { "label": "QA Subscription", "value": "9999-9999-TEST" }
    ]
  }
}
```

*Dynamic options via `optionsGeneratorCommand`*

```json
{
  "name": "featureBranch",
  "description": "Pick a remote feature branch",
  "prompt": "dropdown",
  "promptSettings": {
    "searchable": true,
    "optionsGeneratorCommand": "pwsh ./scripts/list-branches.ps1",
    "delimiter": "\n"
  }
}
```

When `optionsGeneratorCommand` is present ScriptRunner renders a refresh button next to the input (and auto-refreshes when a searchable dropdown receives focus for the first time). The command should emit one option per line (or per delimiter entry). Each token becomes both the label and value; to provide separate labels/values, output JSON via `options` instead. This is ideal for querying live resources (git branches, available clusters, etc.).

---

## `multiSelect`

Checkbox list for selecting multiple values. Stored value is a single string joined with `delimiter`.

| Setting | Type | Description |
| --- | --- | --- |
| `options` | string \| string[] \| {label,value}[] | Same formats as `dropdown`. |
| `delimiter` | string | Separator used when persisting multiple selections (default `,`). |

**Examples**

*Comma-separated string*

```json
{
  "name": "featureFlags",
  "default": "telemetry",
  "prompt": "multiSelect",
  "promptSettings": {
    "options": [
      { "label": "Enable telemetry", "value": "telemetry" },
      { "label": "Verbose logging", "value": "verbose" },
      { "label": "Dry run", "value": "whatif" }
    ]
  }
}
```

---

## `checkbox`

Binary toggle rendered as a checkbox.

| Setting | Type | Description |
| --- | --- | --- |
| `checkedValue` | string | Value written when the box is checked (defaults to `$true` for PowerShell auto-parameters, otherwise `true`). |
| `uncheckedValue` | string | Value written when the box is unchecked (defaults to `$false`/`false`). |

**Example**

```json
{
  "name": "whatIf",
  "description": "Simulate the action without making changes",
  "prompt": "checkbox",
  "promptSettings": {
    "checkedValue": "-WhatIf",
    "uncheckedValue": ""
  }
}
```

If you just need a boolean flag without custom values you can omit `promptSettings` entirely:

```json
{
  "name": "enableTelemetry",
  "description": "Send anonymized usage data",
  "default": "true",
  "prompt": "checkbox"
}
```

---

## `numeric`

Spinner control for whole numbers/decimals.

| Setting | Type | Description |
| --- | --- | --- |
| `min` | string | Minimum allowed value. |
| `max` | string | Maximum allowed value. |
| `step` | string | Increment/decrement step. |

**Example**

```json
{
  "name": "retryCount",
  "default": "3",
  "prompt": "numeric",
  "promptSettings": {
    "min": "0",
    "max": "10",
    "step": "1"
  }
}
```

---

## `datePicker`

Calendar/date input. Values are serialized as strings using the optional format/culture.

| Setting | Type | Description |
| --- | --- | --- |
| `format` | string | .NET date format applied when saving/loading. |
| `yearVisible` | string (`"true"`/`"false"`) | Show or hide the year selector. |
| `monthVisible` | string | Show or hide the month selector. |
| `dayVisible` | string | Show or hide the day selector. |
| `todayAsDefault` | string | `"true"` pre-fills today when no value exists. |
| `culture` | string | Culture name used for parsing/formatting (e.g. `en-US`). |

**Example**

```json
{
  "name": "deploymentDate",
  "description": "Target production release date",
  "prompt": "datePicker",
  "promptSettings": {
    "format": "yyyy-MM-dd",
    "todayAsDefault": "true",
    "culture": "en-GB"
  }
}
```

---

## `timePicker`

Clock selector returning a `HH:mm:ss` style value.

| Setting | Type | Description |
| --- | --- | --- |
| `format` | string | Custom time format (defaults to 24h `HH:mm`). |

**Example**

```json
{
  "name": "maintenanceWindowStart",
  "default": "22:00",
  "prompt": "timePicker",
  "promptSettings": {
    "format": "HH:mm"
  }
}
```

---

## `multilineText`

Resizable text editor with optional syntax highlighting.

| Setting | Type | Description |
| --- | --- | --- |
| `syntax` | string | TextMate grammar extension (e.g. `json`, `ps1`, `sql`). Defaults to `txt`. |

**Example**

```json
{
  "name": "releaseNotes",
  "description": "Changelog that will be appended to the ticket",
  "prompt": "multilineText",
  "promptSettings": {
    "syntax": "markdown"
  }
}
```

---

## `fileContent`

Opens an inline code editor backed by an on-disk file. When the default value is a relative path, ScriptRunner resolves it using the action’s working directory and use it content as a template. If the file is missing, `templateText` seeds the editor. When the action executes, the current editor text is written to a deterministic temp file and the parameter value becomes that temp path (see `FileContent` control), allowing scripts to treat the argument as a file they can read from disk.

How it works internally:

1. The user edits text in the embedded Avalonia editor (`TextEditor`).
2. On execute, ScriptRunner hashes the content (SHA256, first 10 chars) and creates `<temp>\<hash>.extension`.
3. The edited text is saved to that file, and `{paramName}` resolves to the temp file path.
4. Your script receives the path and can pass it directly to CLI tools that expect a file argument (kubectl, azure deploy, etc.).

Because the hash is content-based, repeated runs with the same text reuse the same temp filename, which keeps CLI arguments stable for diffing/logging.

| Setting | Type | Description |
| --- | --- | --- |
| `extension` | string | File extension (without dot) used for syntax highlighting and downloads. |
| `templateText` | string | Initial text when the file does not exist. |

**Example**

```json
{
  "name": "kubeManifest",
  "default": "manifests/staging.yaml",
  "prompt": "fileContent",
  "promptSettings": {
    "extension": "yaml",
    "templateText": "# Paste your Kubernetes manifest here"
  }
}
```

Selecting a predefined argument set can point to different manifest files, and ScriptRunner will hydrate each with its contents.

---

## `filePicker`

Browse for a file path. Relative selections are rewritten as full paths rooted at the action’s working directory.

**Prompt settings**: _none_

**Example**

```json
{
  "name": "artifactPath",
  "description": "Zip or MSI to upload to the release bucket",
  "prompt": "filePicker"
}
```

---

## `directoryPicker`

Folder browser. Paths are expanded relative to the action’s working directory.

**Prompt settings**: _none_

**Example**

```json
{
  "name": "workingTree",
  "description": "Folder that contains the React app",
  "default": "../client-app",
  "prompt": "directoryPicker"
}
```

---

## `timePicker` + `datePicker` combo example

Want a full timestamp? Combine two parameters and reference both placeholders in your command:

```json
{
  "params": [
    {
      "name": "deployDate",
      "prompt": "datePicker",
      "promptSettings": { "format": "yyyy-MM-dd" }
    },
    {
      "name": "deployTime",
      "prompt": "timePicker",
      "promptSettings": { "format": "HH:mm" }
    }
  ],
  "command": "./scripts/schedule.sh --at {deployDate}T{deployTime}:00Z"
}
```

---

## Using `valueGeneratorCommand` with any prompt

Attach `valueGeneratorCommand` to any parameter to fetch a default from an external tool. ScriptRunner will run the command inside the action’s working directory and populate the control.

```json
{
  "name": "latestTag",
  "prompt": "text",
  "valueGeneratorCommand": "git describe --tags --abbrev=0",
  "valueGeneratorLabel": "Use most recent git tag"
}
```

This pattern works equally well with dropdowns (`optionsGeneratorCommand`) and file content (e.g., generating templates before editing).

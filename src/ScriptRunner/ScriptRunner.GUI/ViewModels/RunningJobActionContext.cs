using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ScriptRunner.GUI.ViewModels;

/// <summary>The action and form state from which a running-job tab was created.</summary>
public sealed class RunningJobActionContext
{
    public RunningJobActionContext(
        string? sourceName,
        string actionName,
        IReadOnlyDictionary<string, string> inputValues,
        IReadOnlyDictionary<string, RunningJobPasswordInput> passwordInputs)
    {
        SourceName = sourceName;
        ActionName = actionName;
        InputValues = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(inputValues, StringComparer.OrdinalIgnoreCase));
        PasswordInputs = new ReadOnlyDictionary<string, RunningJobPasswordInput>(
            new Dictionary<string, RunningJobPasswordInput>(passwordInputs, StringComparer.OrdinalIgnoreCase));
    }

    public string? SourceName { get; }
    public string ActionName { get; }
    public IReadOnlyDictionary<string, string> InputValues { get; }
    public IReadOnlyDictionary<string, RunningJobPasswordInput> PasswordInputs { get; }
}

/// <summary>
/// Password state intentionally excludes a resolved Vault secret. A replay keeps the Vault key
/// and resolves its current value when the action is opened or run again.
/// </summary>
public sealed record RunningJobPasswordInput(
    string? ManualValue,
    string? VaultKey,
    bool HasInvalidVaultReference);

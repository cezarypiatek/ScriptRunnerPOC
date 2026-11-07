using Avalonia.Controls;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class PasswordControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((PasswordBox)Control).Password;
    }

    public bool IsNotEmpty() => !string.IsNullOrEmpty(((PasswordBox)Control).Password);

    public void SetValueFromString(string value)
    {
        var passwordBox = (PasswordBox)Control;
        var reference = VaultReference.Parse(value);
        if (reference is { Status: VaultReferenceStatus.Valid, VaultKey: { } vaultKey })
        {
            // Keep the reference as binding state. The secret is resolved only when needed.
            passwordBox.SetVaultValue(vaultKey, null, null);
            return;
        }

        if (reference.Status == VaultReferenceStatus.Invalid)
        {
            passwordBox.SetInvalidVaultReference(value);
            return;
        }

        passwordBox.SetManualValue(value);
    }

    public void SetManualValueFromString(string? value)
    {
        ((PasswordBox)Control).SetManualValue(value);
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public bool Required { get; set; }
}

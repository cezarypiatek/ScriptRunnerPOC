using Microsoft.AspNetCore.DataProtection;

namespace ScriptRunner.GUI.Infrastructure.DataProtection;

internal class MacDataProtector : IDataProtector
{
    private readonly Microsoft.AspNetCore.DataProtection.IDataProtector _dataProtector;

    public MacDataProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("MacOsEncryption");
    }

    public byte[] Protect(byte[] data)
    {
        return _dataProtector.Protect(data);
    }

    public byte[] Unprotect(byte[] data)
    {
        return _dataProtector.Unprotect(data);
    }
}
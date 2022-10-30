using System.Security.Cryptography;
using System.Text;

namespace ScriptRunner.GUI.Infrastructure.DataProtection;

internal class WindowsDataProtector : IDataProtector
{
    private static readonly byte[] EntropyKey = Encoding.ASCII.GetBytes("80CD0C6D-74D3-4E6D-9E4F-ECA485E69FC7");

    public byte[] Protect(byte[] userData)
    {
        return ProtectedData.Protect(userData, EntropyKey, DataProtectionScope.CurrentUser);
    }

    public byte[] Unprotect(byte[] encryptedData)
    {
        return ProtectedData.Unprotect(encryptedData, EntropyKey, DataProtectionScope.CurrentUser);
    }
}
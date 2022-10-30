namespace ScriptRunner.GUI.Infrastructure.DataProtection;

internal class NullDataProtector : IDataProtector
{
    public byte[] Protect(byte[] data)
    {
        return data;
    }

    public byte[] Unprotect(byte[] data)
    {
        return data;
    }
}
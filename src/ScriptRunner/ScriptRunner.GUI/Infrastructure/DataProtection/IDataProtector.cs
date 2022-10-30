namespace ScriptRunner.GUI.Infrastructure.DataProtection;

public interface IDataProtector
{
    public byte[] Protect(byte[] data);
    public byte[] Unprotect(byte[] data);
}
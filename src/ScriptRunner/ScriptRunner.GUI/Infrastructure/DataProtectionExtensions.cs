using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace ScriptRunner.GUI.Infrastructure;

public static class DataProtectionExtensions
{
    public static void AddDataProtectionConfigured(this IServiceCollection services)
    {
        var dataProtectionKeysDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacOsEncryption-Keys");
        var dataProtectionCertificate = SetupDataProtectionCertificate();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory))
            .ProtectKeysWithCertificate(dataProtectionCertificate);
    }
    
    private static X509Certificate2 SetupDataProtectionCertificate()
    {
        string subjectName = "Script Runner Data Protection Certificate";
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
        var certificateCollection = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, validOnly: false);
        if (certificateCollection.Count > 0)
        {
            return certificateCollection[0];
        }

        var certificate = CreateSelfSignedDataProtectionCertificate($"CN={subjectName}");
        InstallCertificate(certificate);
        return certificate;
    }

    private static X509Certificate2 CreateSelfSignedDataProtectionCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddYears(1));
        return certificate;
    }

    static void InstallCertificate(X509Certificate2 certificate)
    {
        var rawData = certificate.Export(X509ContentType.Pkcs12, password: "abc");
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        store.Certificates.Import(rawData, password: "abc", keyStorageFlags: X509KeyStorageFlags.PersistKeySet);
    }
}
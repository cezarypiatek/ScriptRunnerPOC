using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Controls;
using AvaloniaEdit;

namespace ScriptRunner.GUI;

public class FileContent : IControlRecord
{
    private readonly string _extension;
    private readonly bool _useWslPath;
    public Control Control { get; set; }
    public string FileName { get; set; }

    public FileContent(string extension, bool useWslPath)
    {
        _extension = extension;
        _useWslPath = useWslPath;
        FileName = Path.GetTempFileName() + "." + extension;
    }

    public string GetFormattedValue()
    {
        var fileContent =  Control switch
        {
            TextBox textBox => textBox.Text,
            TextEditor textEditor => textEditor.Text,
            _ => ((TextBox)Control).Text
        };
        var hash = string.IsNullOrWhiteSpace(fileContent)? "EMPTY" : ComputeSHA256(fileContent).Substring(0,10);
        FileName = Path.Combine(Path.GetTempPath(), hash + "." + _extension);
        File.WriteAllText(FileName, fileContent, Encoding.UTF8);
        return _useWslPath ? WslPathConverter.ConvertToWslPath(FileName) : FileName;
    }

   

    static string ComputeSHA256(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public static class WslPathConverter
{
    public static string ConvertToWslPath(string fileName)
    {
        var driveLetter = char.ToLower(fileName[0]);
        var pathWithoutDrive = fileName.Substring(2).Replace('\\', '/');
        return $"/mnt/host/{driveLetter}{pathWithoutDrive}";
    }
}
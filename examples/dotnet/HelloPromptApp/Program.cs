using System;
using System.IO;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run -- <text> <filePath>");
    return 1;
}

var textArg = args[0];
var filePath = args[1];
Console.WriteLine("[hello] Hello from C#!");
Console.WriteLine($"[info] Text argument : {textArg}");
Console.WriteLine($"[info] File argument : {filePath}");

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"File '{filePath}' does not exist.");
    return 1;
}

Console.WriteLine("QUESTION: Display the file content? (Yes/No/Maybe)");
var answer = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;

if (answer == "YES")
{
    Console.WriteLine("[content]");
    foreach (var line in File.ReadLines(filePath))
    {
        Console.WriteLine($"  {line}");
    }
}
else if (answer == "NO")
{
    Console.WriteLine("[skip] Content display skipped.");
}
else if (answer == "MAYBE")
{
    Console.WriteLine("MAYBE_SELECTED - user could not decide.");
    return 2;
}
else
{
    Console.WriteLine($"[warn] Unexpected answer '{answer}'. Treating as NO.");
}

return 0;

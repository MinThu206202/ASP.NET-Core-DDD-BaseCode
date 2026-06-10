using System.Diagnostics;

namespace UserApp.Infrastructure.Services.CodeGeneration.Shared;

public class CommandRunner
{
    public static void Run(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);

        if (process == null)
            throw new InvalidOperationException(
                $"Unable to start process {fileName}");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"""
Command failed

Command:
{fileName} {arguments}

Output:
{output}

Error:
{error}
""");
        }
    }
}
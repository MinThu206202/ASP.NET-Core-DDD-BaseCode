using System;
using System.IO;
using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services.CodeGeneration;

public class DbContextUpdater
{
    private readonly FileManager _files;
    private readonly PathProvider _paths;

    public DbContextUpdater(FileManager files, PathProvider paths)
    {
        _files = files;
        _paths = paths;
    }

    public void Update(string name)
    {
        var file = Path.Combine(_paths.SrcRoot, "UserApp.Infrastructure", "Persistence", "AppDbContext.cs");

        EnsureUsing(file, $"using UserApp.Domain.{name}s;");

        var dbSetLine = $"public DbSet<{name}> {name}s => Set<{name}>();";

        if (_files.FileContains(file, dbSetLine))
            return;

        InsertIntoBlock(file,
            "// <AUTO-DBSETS-START>",
            "// <AUTO-DBSETS-END>",
            dbSetLine);
    }

    public void ApplyConfiguration(string name)
    {
        var file = Path.Combine(_paths.SrcRoot, "UserApp.Infrastructure", "Persistence", "AppDbContext.cs");

        var configLine = $"        modelBuilder.ApplyConfiguration(new {name}Configuration());";

        InsertIntoBlock(file,
            "// <AUTO-CONFIG-START>",
            "// <AUTO-CONFIG-END>",
            configLine);
    }

    private void EnsureUsing(string filePath, string usingLine)
    {
        var lines = File.ReadAllLines(filePath);
        if (Array.Exists(lines, x => x.Trim() == usingLine))
            return;

        var lastUsing = Array.FindLastIndex(lines, x => x.Trim().StartsWith("using"));
        if (lastUsing == -1)
            throw new InvalidOperationException("No using statement found in AppDbContext.cs");

        var updated = new string[lines.Length + 1];
        Array.Copy(lines, updated, lastUsing + 1);
        updated[lastUsing + 1] = usingLine;
        Array.Copy(lines, lastUsing + 1, updated, lastUsing + 2, lines.Length - lastUsing - 1);

        File.WriteAllLines(filePath, updated);
    }

    private void InsertIntoBlock(string filePath, string start, string end, string content)
    {
        var lines = File.ReadAllLines(filePath).ToList();

        var startIndex = lines.FindIndex(x => x.Contains(start));
        var endIndex = lines.FindIndex(x => x.Contains(end));

        if (startIndex == -1 || endIndex == -1)
            throw new InvalidOperationException("Block markers not found in AppDbContext.cs");

        if (lines.Skip(startIndex + 1).Take(endIndex - startIndex - 1).Any(x => x.Trim() == content.Trim()))
            return;

        lines.Insert(endIndex, content);
        File.WriteAllLines(filePath, lines);
    }
}

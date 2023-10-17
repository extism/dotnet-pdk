using Extism.Pdk.MSBuild;

using Microsoft.Build.Framework;

using Mono.Cecil;

namespace Extism.Pdk.MsBuild;

public class GenerateFFITask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string AssemblyPath { get; set; }

    [Required]
    public string OutputPath { get; set; }

    [Required]
    public string EnvPath { get; set; }

    [Required]
    public ITaskItem[] UnmanagedEntryPointsAssemblies { get; set; }

    public override bool Execute()
    {
        try
        {
            var assemblyFileName = Path.GetFileName(AssemblyPath);
            var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath);

            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
            else
            {
                foreach (var file in Directory.GetFiles(OutputPath, "*.c"))
                {
                    File.Delete(file);
                }
            }

            var generator = new FFIGenerator(File.ReadAllText(EnvPath), (string message) => Log.LogError(message));

            var references = new HashSet<string>(
                    UnmanagedEntryPointsAssemblies
                        .Select(a => a.GetMetadata("Identity"))
                        .Where(x => x is not null));

            foreach (var file in generator.GenerateGlueCode(assembly, Path.GetDirectoryName(AssemblyPath), references))
            {
                File.WriteAllText(Path.Combine(OutputPath, file.Name), file.Content);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }

}
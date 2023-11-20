using Extism.Pdk.MSBuild;

using Microsoft.Build.Framework;

using Mono.Cecil;

namespace Extism.Pdk.MsBuild;

/// <summary>
/// An MSBuild task that generates the necessary glue code for importing/exporting .NET functions.
/// </summary>
public class GenerateFFITask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Path of the WASI app assembly
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = default!;

    /// <summary>
    /// Path of the generated c files
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = default!;

    /// <summary>
    /// Path of extism.c
    /// </summary>
    [Required]
    public string ExtismPath { get; set; } = default!;

    /// <summary>
    /// Run the task
    /// </summary>
    /// <returns></returns>
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

            var generator = new FFIGenerator(File.ReadAllText(ExtismPath), (string message) => Log.LogError(message));

            foreach (var file in generator.GenerateGlueCode(assembly, Path.GetDirectoryName(AssemblyPath)))
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
using Microsoft.Build.Framework;

using System.Text;

namespace Extism.Pdk.MsBuild
{
    public class ExtismExportGenerator : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            try
            {
                GenerateGlueCode();
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        private void GenerateGlueCode()
        {
            var assemblyFileName = Path.GetFileName(AssemblyPath);
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(AssemblyPath);
            var exportedMethods = assembly.MainModule.Types
                .SelectMany(t => t.Methods)
                .Where(m => m.IsStatic && m.CustomAttributes.Any(a => a.AttributeType.Name == "ExtismExportAttribute"))
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("#pragma once");
            sb.AppendLine("#define NDEBUG");
            sb.AppendLine("#include <string.h>");
            sb.AppendLine("#include <mono-wasi/driver.h>");
            sb.AppendLine("#include <mono/metadata/exception.h>");
            sb.AppendLine("#include <assert.h>");

            foreach (var method in exportedMethods)
            {
                var attribute = method.CustomAttributes.First(a => a.AttributeType.Name == "ExtismExportAttribute");
                var functionName = attribute.ConstructorArguments[0].Value.ToString();
                var methodName = method.Name;
                var parameterCount = method.Parameters.Count;
                var methodParams = string.Join(", ", Enumerable.Repeat("NULL", parameterCount));
                var returnType = method.ReturnType.FullName;

                sb.AppendLine();
                sb.AppendLine($@"
MonoMethod* method_{functionName};
__attribute__((export_name(""{functionName}""))) int {functionName}()
{{
    if (!method_{functionName})
    {{
        method_{functionName} = lookup_dotnet_method(""{assemblyFileName}"", ""{method.DeclaringType.Namespace}"", ""{method.DeclaringType.Name}"", ""{methodName}"", -1);
        assert(method_{functionName});
    }}

    void* method_params[] = {{ }};
    MonoObject* exception;
    MonoObject* result = mono_wasm_invoke_method(method_{functionName}, NULL, method_params, &exception);
    assert(!exception);

    int int_result = *(int*)mono_object_unbox(result);
    return int_result;
}}
");
            }

            File.WriteAllText(OutputPath, sb.ToString());
        }
    }
}
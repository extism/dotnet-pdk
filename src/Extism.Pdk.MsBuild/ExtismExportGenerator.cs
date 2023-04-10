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
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(AssemblyPath);
            var exportedMethods = assembly.MainModule.Types
                .SelectMany(t => t.Methods)
                .Where(m => m.IsStatic && m.CustomAttributes.Any(a => a.AttributeType.Name == "ExtismExportAttribute"));

            var assemblyName = Path.GetFileNameWithoutExtension(assembly.FullName);

            var sb = new StringBuilder();
            sb.AppendLine("#pragma once");
            sb.AppendLine("#define NDEBUG");
            sb.AppendLine("#include <string.h>");
            sb.AppendLine("#include <mono-wasi/driver.h>");
            sb.AppendLine("#include <mono/metadata/exception.h>");
            sb.AppendLine("#include <assert.h>");

            foreach (var method in exportedMethods)
            {
                var methodName = method.Name;
                var parameterCount = method.Parameters.Count;
                var methodParams = string.Join(", ", Enumerable.Repeat("NULL", parameterCount));
                var returnType = method.ReturnType.FullName;

                sb.AppendLine();
                sb.AppendLine($@"
#pragma once
#define NDEBUG
// https://github.com/dotnet/runtime/blob/v7.0.0/src/mono/wasi/mono-wasi-driver/driver.c
#include <string.h>

#include <mono-wasi/driver.h>
#include <mono/metadata/exception.h>
#include <assert.h>

MonoMethod* method;
__attribute__((export_name(""
                {assemblyName.ToLower()}_export""))) int {assemblyName.ToLower()}_export()
{{
    if (!method)
    {{
        method = lookup_dotnet_method(""{assemblyName}.dll"", ""{assemblyName}"", ""Functions"", ""{{0}}"", -1);
        assert(method);
    }}

    void* method_params[] = {{ }};
    MonoObject* exception;
    MonoObject* result = mono_wasm_invoke_method(method, NULL, method_params, &exception);
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
using Microsoft.Build.Framework;

using System.Text;

namespace Extism.Pdk.MsBuild
{
    public class ExtismFFIGenerator : Microsoft.Build.Utilities.Task
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

            var importedMethods = assembly.MainModule.Types
                .SelectMany(t => t.Methods)
                .Where(m => m.IsStatic && m.CustomAttributes.Any(a => a.AttributeType.Name == "ExtismImportAttribute"))
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("#pragma once");
            sb.AppendLine("#define NDEBUG");
            sb.AppendLine("#include <string.h>");
            sb.AppendLine("#include <mono-wasi/driver.h>");
            sb.AppendLine("#include <mono/metadata/exception.h>");
            sb.AppendLine("#include <assert.h>");
            sb.AppendLine("#define IMPORT(a, b) __attribute__((import_module(a), import_name(b)))");

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

            sb.AppendLine();

            var internalCalls = new List<string>();
            var imports = new List<string>();

            foreach (var method in importedMethods)
            {
                var attribute = method.CustomAttributes.First(a => a.AttributeType.Name == "ExtismImportAttribute");
                var moduleName = attribute.ConstructorArguments[0].Value.ToString();
                var functionName = attribute.ConstructorArguments[1].Value.ToString();

                if (!_types.ContainsKey(method.ReturnType.Name))
                {
                    Log.LogError("Unsupported return type: {0} on {1} method.", method.ReturnType.FullName, method.FullName);
                    continue;
                }

                var p = method.Parameters.FirstOrDefault(p => !_types.ContainsKey(p.ParameterType.Name));
                if (p != null)
                {
                    sb.AppendLine($"\\\\ Unrecognized type: ${p.ParameterType.Name} => '{p.ParameterType.FullName}'.");
                    Log.LogError("Unsupported parameter type: {0} ({1}) on {2} method.", p.Name, p.ParameterType.FullName, method.FullName);
                    continue;
                }

                var parameters = string.Join(", ", method.Parameters.Select(p => $"{_types[p.ParameterType.Name]} {p.Name}"));

                internalCalls.Add($"""  mono_add_internal_call("{method.DeclaringType.FullName}::{method.Name}", {functionName});""");
                imports.Add($"""IMPORT("{moduleName}", "{functionName}") extern {_types[method.ReturnType.Name]} {functionName}({parameters});""");
            }

            foreach (var import in imports)
            {
                sb.AppendLine(import);
            }

            sb.AppendLine("void extism_pdk_attach_imports()");
            sb.AppendLine("{");

            foreach (var call in internalCalls)
            {
                sb.AppendLine(call);
            }

            sb.AppendLine("}");

            File.WriteAllText(OutputPath, sb.ToString());
        }

        private static readonly Dictionary<string, string> _types = new Dictionary<string, string>
        {
            { nameof(SByte), "int8_t" },
            { nameof(Int16), "int16_t" },
            { nameof(Int32), "int32_t" },
            { nameof(Int64), "int64_t" },

            { nameof(Byte), "uint8_t" },
            { nameof(UInt16), "uint16_t" },
            { nameof(UInt32), "uint32_t" },
            { nameof(UInt64), "uint64_t" },

            { nameof(Single), "float" },
            { nameof(Double), "double" },
        };
    }
}
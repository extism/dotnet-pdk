using Microsoft.Build.Framework;
using Mono.Cecil;
using System.Text;

namespace Extism.Pdk.MsBuild
{
    public class ExtismFFIGenerator : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string EnvPath { get; set; }

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

            var exportedMethods = assembly.MainModule.Types
                .SelectMany(t => t.Methods)
                .Where(m => m.IsStatic && m.CustomAttributes.Any(a => a.AttributeType.Name == "UnmanagedCallersOnlyAttribute"))
                .ToArray();

            // TODO: also find F# module functions
            var importedMethods = assembly.MainModule.Types
                .SelectMany(t => t.Methods)
                .Where(m => m.HasPInvokeInfo)
                .ToArray();

            GenerateExports(assemblyFileName, exportedMethods);
            GenerateImports(importedMethods);
        }

        private void GenerateImports(MethodDefinition[] importedMethods)
        {
            var modules = importedMethods.GroupBy(m => m.PInvokeInfo.Module.Name)
                            .Select(g => new
                            {
                                Name = g.Key,
                                Imports = g.Select(m => ToImportStatement(m)).ToArray(),
                            })
                            .ToList();

            var envImports = File.ReadAllText(EnvPath);
            var envWritten = false;

            // For DllImport to work with wasm, the name of the file has to match
            // the name of the module that the function is imported from
            foreach (var module in modules)
            {
                var builder = new StringBuilder();

                if (module.Name == "env")
                {
                    envWritten = true;
                    builder.AppendLine(envImports);
                }
                else
                {
                    builder.AppendLine(Preamble);
                }

                foreach (var import in module.Imports)
                {
                    builder.AppendLine(import);
                }

                File.WriteAllText(Path.Combine(OutputPath, $"{module.Name}.c"), builder.ToString());
            }

            if (!envWritten)
            {
                File.WriteAllText(Path.Combine(OutputPath, $"env.c"), envImports);
            }
        }

        private void GenerateExports(string assemblyFileName, MethodDefinition[] exportedMethods)
        {
            var sb = new StringBuilder();

            if (exportedMethods.Length > 0)
            {
                sb.AppendLine(Preamble);
                sb.AppendLine("""          
            // _initialize
            void mono_wasm_load_runtime(const char* unused, int debug_level);

            #ifdef WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
            // This is supplied from the MSBuild itemgroup @(WasiAfterRuntimeLoaded)
            WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
            #endif

            __attribute__((export_name("_initialize"))) void initialize() {
                mono_wasm_load_runtime("", 0);
            }

            // end of _initialize   
            """);
                sb.AppendLine("extern void mono_wasm_invoke_method_ref(MonoMethod* method, MonoObject** this_arg_in, void* params[], MonoObject** _out_exc, MonoObject** out_result);");

                foreach (var method in exportedMethods)
                {
                    var attribute = method.CustomAttributes.First(a => a.AttributeType.Name == "UnmanagedCallersOnlyAttribute");

                    var exportName = attribute.Fields.FirstOrDefault(p => p.Name == "EntryPoint").Argument.Value?.ToString() ?? method.Name;
                    var parameterCount = method.Parameters.Count;
                    var methodParams = string.Join(", ", Enumerable.Repeat("NULL", parameterCount));
                    var returnType = method.ReturnType.FullName;

                    sb.AppendLine();
                    sb.AppendLine($@"
MonoMethod* method_{exportName};
__attribute__((export_name(""{exportName}""))) int {exportName}()
{{
    if (!method_{exportName})
    {{
        method_{exportName} = lookup_dotnet_method(""{assemblyFileName}"", ""{method.DeclaringType.Namespace}"", ""{method.DeclaringType.Name}"", ""{method.Name}"", -1);
        assert(method_{exportName});
    }}

    void* method_params[] = {{ }};
    MonoObject* exception = NULL;
    MonoObject* result = NULL;
    mono_wasm_invoke_method_ref(method_{exportName}, NULL, method_params, &exception, &result);
    assert(!exception);
    
    int int_result = 0;  // Default value

    if (result != NULL) {{
        int_result = *(int*)mono_object_unbox(result);
    }}
    
    return int_result;
}}
");
                }
            }

            sb.AppendLine();
            File.WriteAllText(Path.Combine(OutputPath, "exports.c"), sb.ToString());
        }

        private string ToImportStatement(MethodDefinition method)
        {
            var moduleName = method.PInvokeInfo.Module.Name;
            var functionName = method.PInvokeInfo.EntryPoint ?? method.Name;

            if (!_types.ContainsKey(method.ReturnType.Name))
            {
                Log.LogError("Unsupported return type: {0} on {1} method.", method.ReturnType.FullName, method.FullName);
                return "";
            }

            var sb = new StringBuilder();
            var p = method.Parameters.FirstOrDefault(p => !_types.ContainsKey(p.ParameterType.Name));
            if (p != null)
            {
                Log.LogError("Unsupported parameter type: {0} ({1}) on {2} method.", p.Name, p.ParameterType.FullName, method.FullName);

                return $"\\\\ Unrecognized type: ${p.ParameterType.Name} => '{p.ParameterType.FullName}'.";
            }

            var parameters = string.Join(", ", method.Parameters.Select(p => $"{_types[p.ParameterType.Name]} {p.Name}"));
            var parameterNames = string.Join(", ", method.Parameters.Select(p => p.Name));

            var returnKeyword = _types[method.ReturnType.Name] == "void" ? "" : "return ";

            return $$"""
            IMPORT("{{moduleName}}", "{{functionName}}") extern {{_types[method.ReturnType.Name]}} {{functionName}}_import({{parameters}});
            
            {{_types[method.ReturnType.Name]}} {{functionName}}({{parameters}}) {
                {{returnKeyword}}{{functionName}}_import({{parameterNames}});
            }
            """;
        }

        private const string Preamble = """
#include <string.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>

// https://github.com/dotnet/runtime/blob/v7.0.0/src/mono/wasi/mono-wasi-driver/driver.c
#include <string.h>

#include "driver.h"

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <stdbool.h>
#include <stdio.h>

#define IMPORT(a, b) __attribute__((import_module(a), import_name(b)))

typedef uint64_t ExtismPointer;
""";

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

            { "Void", "void"},
        };
    }
}
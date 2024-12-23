﻿using Mono.Cecil;

using System.Text;

namespace Extism.Pdk.MSBuild
{
    /// <summary>
    /// Generate the necessary glue code to export/import .NET functions
    /// </summary>
    public class FFIGenerator
    {
        private readonly Action<string> _logError;
        private readonly string _extism;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="extism"></param>
        /// <param name="logError"></param>
        public FFIGenerator(string extism, Action<string> logError)
        {
            _logError = logError;
            _extism = extism;
        }

        /// <summary>
        /// Generate glue code for the given assembly and referenced assemblies
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public IEnumerable<FileEntry> GenerateGlueCode(AssemblyDefinition assembly, string directory)
        {
            var assemblies = assembly.MainModule.AssemblyReferences
                .Where(r => !r.Name.StartsWith("System") && !r.Name.StartsWith("Microsoft") && r.Name != "Extism.Pdk")
                .Select(r => AssemblyDefinition.ReadAssembly(Path.Combine(directory, r.Name + ".dll")))
                .ToList();

            assemblies.Add(assembly);

            var types = assemblies.SelectMany(a => a.MainModule.Types).ToArray();

            var exportedMethods = types
                    .SelectMany(t => t.Methods)
                    .Where(m => m.IsStatic && m.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"))
                .ToArray();

            // TODO: also find F# module functions
            var importedMethods = types
                .SelectMany(t => t.Methods)
                .Where(m => m.HasPInvokeInfo)
                .ToArray();

            var files = GenerateImports(importedMethods, _extism);
            files.Add(GenerateExports(exportedMethods));

            return files;
        }

        private List<FileEntry> GenerateImports(MethodDefinition[] importedMethods, string extism)
        {
            var modules = importedMethods.GroupBy(m => m.PInvokeInfo.Module.Name)
                            .Select(g => new
                            {
                                Name = g.Key,
                                Imports = g.Select(m => ToImportStatement(m)).ToArray(),
                            })
                            .ToList();

            var extismWritten = false;

            var files = new List<FileEntry>();

            // For DllImport to work with wasm, the name of the file has to match
            // the name of the module that the function is imported from
            foreach (var module in modules)
            {
                var builder = new StringBuilder();

                if (module.Name == "extism")
                {
                    extismWritten = true;
                    builder.AppendLine(extism);
                }
                else
                {
                    builder.AppendLine(Preamble);
                }

                foreach (var import in module.Imports)
                {
                    builder.AppendLine(import);
                }

                files.Add(new FileEntry { Name = $"{module.Name}.c", Content = builder.ToString() });
            }

            if (!extismWritten)
            {
                files.Add(new FileEntry { Name = $"extism.c", Content = extism });
            }

            return files;
        }

        private FileEntry GenerateExports(MethodDefinition[] exportedMethods)
        {
            var sb = new StringBuilder();

            if (exportedMethods.Length > 0)
            {
                sb.AppendLine(Preamble);
                sb.AppendLine(
                """          
                // _initialize
                void mono_wasm_load_runtime(const char* unused, int debug_level);

                #ifdef WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
                // This is supplied from the MSBuild itemgroup @(WasiAfterRuntimeLoaded)
                WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
                #endif

                void initialize_runtime() {
                    mono_wasm_load_runtime("", 0);
                }

                // end of _initialize
                """);

                sb.AppendLine(
                """

                void mono_wasm_invoke_method_ref(MonoMethod* method, MonoObject** this_arg_in, void* params[], MonoObject** _out_exc, MonoObject** out_result);
                MonoString* mono_object_try_to_string (MonoObject *obj, MonoObject **exc, MonoError *error);
                void mono_print_unhandled_exception(MonoObject *exc);

                MonoMethod* method_extism_print_exception;
                void extism_print_exception(MonoObject* exc)
                {
                    if (!method_extism_print_exception)
                    {
                        method_extism_print_exception = lookup_dotnet_method("Extism.Pdk.dll", "Extism", "Native", "PrintException", -1);

                        if (method_extism_print_exception == NULL) {
                            printf("Fatal: Failed to find Extism.Native.PrintException");
                        }

                        assert(method_extism_print_exception);
                    }

                    void* method_params[] = { exc };
                    MonoObject* exception = NULL;
                    MonoObject* result = NULL;
                    mono_wasm_invoke_method_ref(method_extism_print_exception, NULL, method_params, &exception, &result);
                
                    if (exception != NULL) {
                        const char* message = "An exception was thrown while trying to print the previous exception. Please check stderr for details.";
                        mono_print_unhandled_exception(exception);
                    }
                }

                """);

                foreach (var method in exportedMethods)
                {
                    if (method.Parameters.Count > 0)
                    {
                        var parameterNames = string.Join(",", method.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        _logError($"Extism doesn't support exporting functions that have parameters: {method.DeclaringType.FullName}.{method.Name}({parameterNames})");
                    }

                    var assemblyFileName = method.Module.Assembly.Name.Name + ".dll";
                    var attribute = method.CustomAttributes.First(a => a.AttributeType.Name == "UnmanagedCallersOnlyAttribute");

                    var exportName = attribute.Fields.FirstOrDefault(p => p.Name == "EntryPoint").Argument.Value?.ToString() ?? method.Name;
                    var parameterCount = method.Parameters.Count;
                    var methodParams = string.Join(", ", Enumerable.Repeat("NULL", parameterCount));
                    var returnType = method.ReturnType.FullName;

                    sb.AppendLine($$"""
MonoMethod* method_{{exportName}};
__attribute__((export_name("{{exportName}}"))) int {{exportName}}()
{
    initialize_runtime();

    if (!method_{{exportName}})
    {
        method_{{exportName}} = lookup_dotnet_method("{{assemblyFileName}}", "{{method.DeclaringType.Namespace}}", "{{method.DeclaringType.Name}}", "{{method.Name}}", -1);
        assert(method_{{exportName}});
    }

    void* method_params[] = { };
    MonoObject* exception = NULL;
    MonoObject* result = NULL;
    mono_wasm_invoke_method_ref(method_{{exportName}}, NULL, method_params, &exception, &result);
   
    if (exception != NULL) {
        const char* message = "An exception was thrown when calling {{exportName}}. Please check stderr for details.";
        mono_print_unhandled_exception(exception);

        extism_print_exception(exception);
        return 1;
    }
    
    int int_result = 0;  // Default value

    if (result != NULL) {
        int_result = *(int*)mono_object_unbox(result);
    }
    
    return int_result;
}
""");
                }
            }

            sb.AppendLine();
            return new FileEntry { Name = "exports.c", Content = sb.ToString() };
        }

        private string ToImportStatement(MethodDefinition method)
        {
            var moduleName = method.PInvokeInfo.Module.Name;
            if (moduleName == "extism")
            {
                // Redirect imported host functions to extism:host/user
                // The PDK functions don't use this generator, so we can safely assume
                // every `extism` import is a user host function
                moduleName = "extism:host/user";
            }

            var functionName = string.IsNullOrEmpty(method.PInvokeInfo.EntryPoint) ? method.Name : method.PInvokeInfo.EntryPoint;

            if (!_types.ContainsKey(method.ReturnType.Name))
            {
                _logError($"Unsupported return type: {method.ReturnType.FullName} on {method.FullName} method.");
                return "";
            }

            var sb = new StringBuilder();
            var p = method.Parameters.FirstOrDefault(p => !_types.ContainsKey(p.ParameterType.Name));
            if (p != null)
            {
                _logError($"Unsupported parameter type: {p.Name} ({p.ParameterType.FullName}) on {method.FullName} method.");

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

    /// <summary>
    /// A file generated by the task
    /// </summary>
    public class FileEntry
    {
        /// <summary>
        /// Name of the file
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Content of the file
        /// </summary>
        public string Content { get; set; } = default!;
    }
}
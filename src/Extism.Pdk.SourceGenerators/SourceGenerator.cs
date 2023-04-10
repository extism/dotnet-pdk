using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Extism.Pdk.SourceGenerators;

[Generator]
public class ExtismGlueCodeGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var syntaxTrees = context.Compilation.SyntaxTrees;
        foreach (var syntaxTree in syntaxTrees)
        {
            var root = syntaxTree.GetCompilationUnitRoot();
            var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classes)
            {
                var methods = cls.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var exportAttr = method.AttributeLists.SelectMany(l => l.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "ExtismExport");
                    if (exportAttr == null)
                    {
                        continue;
                    }

                    var methodName = method.Identifier.ToString();
                    var className = cls.Identifier.ToString();
                    var assemblyName = context.Compilation.AssemblyName;

                    var code = $@"
#pragma once
#define NDEBUG
// https://github.com/dotnet/runtime/blob/v7.0.0/src/mono/wasi/mono-wasi-driver/driver.c
#include <string.h>

#include <mono-wasi/driver.h>
#include <mono/metadata/exception.h>
#include <assert.h>

MonoMethod* method_{methodName};
__attribute__((export_name(""{methodName}""))) int {methodName}()
{{
	if (!method_{methodName})
	{{
		method_{methodName} = lookup_dotnet_method(""{assemblyName}.dll"", ""{className}"", ""{methodName}"", -1);
		assert(method_{methodName});
	}}

	void* method_params[] = {{ }};
	MonoObject* exception;
	MonoObject* result = mono_wasm_invoke_method(method_{methodName}, NULL, method_params, &exception);
	assert(!exception);

	int int_result = *(int*)mono_object_unbox(result);
	return int_result;
}}";

                    //var sourceText = SourceText.From(code, Encoding.UTF8);
                    //var fileName = $"{className}_{methodName}_glue.c";
                    //context.AddSource(fileName, sourceText);

                  //context.Compilation.assembl
                }
            }
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}

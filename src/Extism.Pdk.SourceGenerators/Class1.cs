using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

using System.Collections.Generic;
using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace Extism.SourceGenerators;

[Generator]
public class WasmFunctionGenerator : ISourceGenerator
{

    const string AttributeSource =
        """
        namespace Extism 
        {
            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            sealed class WasmFunctionAttribute : Attribute
            {
                public WasmFunctionAttribute(string name)
                {
                    Name = name;
                }

                public string Name { get; set; }
            }
        }
        """;

    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is WasmFunctionSyntaxReceiver receiver))
        {
            throw new InvalidOperationException($"Context Receiver not registered!: {context.SyntaxContextReceiver}");
        }

        var builder = new StringBuilder();

        builder.AppendLine($$"""
            using Extism;
            using System;
            using System.Runtime.InteropServices;
            using System.Text.Json.Serialization;

            namespace Extism.Generated
            {
                public class GeneratedExports
                {

            """);

        foreach (var function in receiver.CandidateMethods)
        {
            var semanticModel = context.Compilation.GetSemanticModel(function.method.SyntaxTree);
            var exportName = semanticModel.GetConstantValue(function.attribute.ArgumentList.Arguments[0].Expression);
            var methodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(function.method);

            var attributes = methodSymbol.GetAttributes();
            var jsonAttribute = attributes.FirstOrDefault(a => a.AttributeClass.Name == "JsonInputOutputAttribute");

            var methodFullyQualifiedName = $"{methodSymbol.ContainingType.ContainingNamespace}.{methodSymbol.ContainingType.Name}.{methodSymbol.Name}";

            var isVoid = methodSymbol.ReturnType.Name == "Void";
            var variableAssignment = isVoid ? "" : "var result = ";

            string methodCall;
            if (methodSymbol.Parameters.Length == 0)
            {
                methodCall = $$"""
                    {{variableAssignment}}{{methodFullyQualifiedName}}();
                    """;
            }
            else if (methodSymbol.Parameters.Length == 1)
            {
                if (methodSymbol.Parameters[0].Type.Name == "String")
                {
                    methodCall = $$"""
                    var input = global::Extism.Pdk.GetInputString();
                    {{variableAssignment}}{{methodFullyQualifiedName}}(input);
                 """;
                }
                else
                {
                    var contextType = (ITypeSymbol)jsonAttribute.ConstructorArguments[0].Value;

                    methodCall = $$"""
                    var typeInfo = global::{{contextType.ContainingNamespace}}.{{contextType.Name}}.Default.{{methodSymbol.Parameters[0].Type.Name}};
                    var json = global::Extism.Pdk.GetInput();
                    var serializer = new global::Extism.JsonExtismSerializer();
                    var input = serializer.Deserialize<{{methodSymbol.Parameters[0].Type.ContainingNamespace.Name}}.{{methodSymbol.Parameters[0].Type.Name}}>(json, typeInfo);
                    {{variableAssignment}}{{methodFullyQualifiedName}}(input);
                    """;
                }

            }
            else
            {
                // TODO: turn into diagnostic error
                throw new NotImplementedException("Method can only have up to 1 parameter");
            }

            string serialization;
            if (isVoid)
            {
                serialization = "";
            } else if (methodSymbol.ReturnType.Name == "String")
            {
                serialization = "global::Extism.Pdk.SetOutput(result);";
            } else
            {
                var typeName = $"{methodSymbol.ReturnType.ContainingNamespace.Name}.{methodSymbol.ReturnType.Name}";
                var contextType = (ITypeSymbol)jsonAttribute.ConstructorArguments[0].Value;
                var typeInfo = $"global::{contextType.ContainingNamespace}.{contextType.Name}.{methodSymbol.ReturnType.Name}";
                serialization =
                    $$"""
                    var typeInfo2 = global::{{contextType.ContainingNamespace}}.{{contextType.Name}}.Default.{{methodSymbol.ReturnType.Name}};
                    var serializer2 = new global::Extism.JsonExtismSerializer();
                    var json2 = serializer2.Serialize(result, typeInfo2);
                    global::Extism.Pdk.SetOutput(json2);
                    """;
            }

            var source =
                 $$"""
                        [UnmanagedCallersOnly(EntryPoint = "{{exportName}}")]
                        public static void {{methodSymbol.Name}}()
                        {
                            {{methodCall}}
                            {{serialization}}
                        }
                """;

            builder.AppendLine(source);
        }

        builder.AppendLine(
            """
                } // class
            } // namespace
            """);

        context.AddSource("extism_exports.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization((pi) => pi.AddSource("WasmFunction__Attributes.g.cs", AttributeSource));
        context.RegisterForSyntaxNotifications(() => new WasmFunctionSyntaxReceiver());
    }
}

class WasmFunctionSyntaxReceiver : ISyntaxContextReceiver
{
    public List<(MethodDeclarationSyntax method, AttributeSyntax attribute)> CandidateMethods { get; } = new List<(MethodDeclarationSyntax method, AttributeSyntax attribute)>();

    /// <summary>
    /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
    /// </summary>
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        var syntaxNode = context.Node;

        if (!(syntaxNode is MethodDeclarationSyntax method))
        {
            return;
        }

        var attribute = method.AttributeLists
            .SelectMany(l => l.Attributes)
            .FirstOrDefault(a => context.SemanticModel.GetTypeInfo(a).Type?.ToDisplayString()?.Contains("WasmFunction") == true);

        if (attribute is null)
        {
            return;
        }

        CandidateMethods.Add((method, attribute));
    }
}
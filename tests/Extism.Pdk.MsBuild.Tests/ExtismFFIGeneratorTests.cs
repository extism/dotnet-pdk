using Extism.Pdk.MSBuild;

using Mono.Cecil;

using Shouldly;

using System.Runtime.InteropServices;

namespace Extism.Pdk.MsBuild.Tests
{
    public class ExtismFFIGeneratorTests
    {
        [Fact]
        public void CanHandleEmptyAssemblies()
        {
            var extism = "";
            var generator = new FFIGenerator(extism, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var files = generator.GenerateGlueCode(assembly);
        }

        [Fact]
        public void CanImportFromExtism()
        {
            var extism = "// extism stuff";
            var generator = new FFIGenerator(extism, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var type = assembly.MainModule.CreateType("MyNamespace", "MyClass");

            _ = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                .AddImport("extism", "do_something");

            var files = generator.GenerateGlueCode(assembly);

            var extismFile = files.Single(f => f.Name == "extism.c");
            extismFile.Content.Trim().ShouldBe(
                """
                // extism stuff
                IMPORT("extism", "do_something") extern void do_something_import(int32_t p1, uint8_t p2, int64_t p3);

                void do_something(int32_t p1, uint8_t p2, int64_t p3) {
                    do_something_import(p1, p2, p3);
                }
                """.Trim(), StringCompareShould.IgnoreLineEndings);

            files.ShouldNotContain(f => f.Name == "export.c");
        }

        [Fact]
        public void CanImportFromCustomModules()
        {
            var extism = "// extism stuff";
            var generator = new FFIGenerator(extism, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var type = assembly.MainModule.CreateType("MyNamespace", "MyClass");

            _ = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                .AddImport("env", "do_something");

            _ = type.CreateMethod("GetLength", typeof(int), ("p1", typeof(float)))
                .AddImport("env", null);

            var files = generator.GenerateGlueCode(assembly);

            var envFile = files.Single(f => f.Name == "env.c");
            var expected = File.ReadAllText("snapshots/import-custom-module.txt");
            envFile.Content.Trim().ShouldBe(expected, StringCompareShould.IgnoreLineEndings);

            AssertContent(extism, files, "extism.c");
            files.ShouldNotContain(f => f.Name == "export.c");
        }

        private static void AssertContent(string content, IEnumerable<FileEntry> files, string fileName)
        {
            var extismFile = files.Single(f => f.Name == fileName);
            extismFile.Content.Trim().ShouldBe(content.Trim(), StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void CanExportMethods()
        {
            var extism = "// extism stuff";
            var generator = new FFIGenerator(extism, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var type = assembly.MainModule.CreateType("MyNamespace", "MyClass");

            _ = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                    .AddExport();

            _ = type.CreateMethod("DoSomeOtherStuff", typeof(int), ("longParameterNameHere", typeof(double)))
                  .AddExport("fancy_name");

            var files = generator.GenerateGlueCode(assembly);

            var file = files.Single(f => f.Name == "exports.c");
            var expected = File.ReadAllText("snapshots/exports.txt");
            file.Content.Trim().ShouldBe(expected, StringCompareShould.IgnoreLineEndings);

            AssertContent(extism, files, "extism.c");
        }
    }

    public static class CecilExtensions
    {
        public static AssemblyDefinition CreateSampleAssembly(string name)
        {
            // Create an assembly
            var assembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition(name, new Version(1, 0, 0, 0)),
                name,
                ModuleKind.Dll);

            return assembly;
        }

        public static TypeDefinition CreateType(this ModuleDefinition module, string ns, string name)
        {
            var type = new TypeDefinition(
                "SampleNamespace",
                "SampleType",
                TypeAttributes.Public | TypeAttributes.Class);

            module.Types.Add(type);

            return type;
        }

        public static MethodDefinition CreateMethod(this TypeDefinition type, string name, Type returnType, params (string, Type)[] arguments)
        {
            var method = new MethodDefinition(
                name,
                MethodAttributes.Public | MethodAttributes.Static,
                type.Module.ImportReference(returnType));

            foreach (var argument in arguments)
            {
                method.Parameters.Add(new ParameterDefinition(argument.Item1, ParameterAttributes.None, type.Module.ImportReference(argument.Item2)));
            }

            type.Methods.Add(method);

            return method;
        }

        public static MethodDefinition AddImport(this MethodDefinition method, string moduleName, string? entryPoint = null)
        {
            var pinvokeInfo = new PInvokeInfo(PInvokeAttributes.CallConvCdecl, entryPoint, new ModuleReference(moduleName));
            method.PInvokeInfo = pinvokeInfo;
            method.Attributes |= MethodAttributes.PInvokeImpl;
            method.ImplAttributes |= MethodImplAttributes.PreserveSig | (MethodImplAttributes)pinvokeInfo.Attributes;

            return method;
        }

        public static MethodDefinition AddExport(this MethodDefinition method, string? entryPoint = null)
        {
            var attributeConstructor = method.Module.ImportReference(typeof(UnmanagedCallersOnlyAttribute).GetConstructor(Type.EmptyTypes));

            var customAttribute = new CustomAttribute(attributeConstructor);

            if (entryPoint != null)
            {
                var argument = new CustomAttributeArgument(method.Module.ImportReference(typeof(string)), entryPoint);
                customAttribute.Fields.Add(new CustomAttributeNamedArgument(nameof(DllImportAttribute.EntryPoint), argument));
            }

            method.CustomAttributes.Add(customAttribute);

            return method;
        }
    }
}
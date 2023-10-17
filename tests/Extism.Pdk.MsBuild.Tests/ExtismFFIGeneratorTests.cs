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
            var env = "";
            var generator = new FFIGenerator(env, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var files = generator.GenerateGlueCode(assembly, Directory.GetCurrentDirectory(), new HashSet<string>());
        }

        [Fact]
        public void CanImportFromEnv()
        {
            var env = "// env stuff";
            var generator = new FFIGenerator(env, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var type = assembly.MainModule.CreateType("MyNamespace", "MyClass");

            _ = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                .AddImport("env", "do_something");

            var files = generator.GenerateGlueCode(assembly, Directory.GetCurrentDirectory(), new HashSet<string>());

            var envFile = files.Single(f => f.Name == "env.c");
            envFile.Content.Trim().ShouldBe(
                """
                // env stuff
                IMPORT("env", "do_something") extern void do_something_import(int32_t p1, uint8_t p2, int64_t p3);

                void do_something(int32_t p1, uint8_t p2, int64_t p3) {
                    do_something_import(p1, p2, p3);
                }
                """.Trim(), StringCompareShould.IgnoreLineEndings);

            files.ShouldNotContain(f => f.Name == "export.c");
        }

        [Fact]
        public void CanImportFromCustomModules()
        {
            var env = "// env stuff";
            var generator = new FFIGenerator(env, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var type = assembly.MainModule.CreateType("MyNamespace", "MyClass");

            _ = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                .AddImport("host", "do_something");

            _ = type.CreateMethod("GetLength", typeof(int), ("p1", typeof(float)))
                .AddImport("host", null);

            var files = generator.GenerateGlueCode(assembly, Directory.GetCurrentDirectory(), new HashSet<string>());

            var hostFile = files.Single(f => f.Name == "host.c");
            var expected = File.ReadAllText("snapshots/import-custom-module.txt");
            hostFile.Content.Trim().ShouldBe(expected, StringCompareShould.IgnoreLineEndings);

            AssertContent(env, files, "env.c");
            files.ShouldNotContain(f => f.Name == "export.c");
        }

        private static void AssertContent(string content, IEnumerable<FileEntry> files, string fileName)
        {
            var envFile = files.Single(f => f.Name == fileName);
            envFile.Content.Trim().ShouldBe(content.Trim(), StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void CanExportMethods()
        {
            var env = "// env stuff";
            var generator = new FFIGenerator(env, (m) => { });

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp");

            var type = assembly.MainModule.CreateType("MyNamespace", "MyClass");

            _ = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                    .AddExport();

            _ = type.CreateMethod("DoSomeOtherStuff", typeof(int), ("longParameterNameHere", typeof(double)))
                  .AddExport("fancy_name");

            var files = generator.GenerateGlueCode(assembly, Directory.GetCurrentDirectory(), new HashSet<string>());

            var file = files.Single(f => f.Name == "exports.c");
            var expected = File.ReadAllText("snapshots/exports.txt");
            file.Content.Trim().ShouldBe(expected, StringCompareShould.IgnoreLineEndings);

            AssertContent(env, files, "env.c");
        }

        [Fact]
        public void CanExportMethodFromReferences()
        {
            var env = "// env stuff";
            var generator = new FFIGenerator(env, (m) => { });

            var lib = CecilExtensions.CreateSampleAssembly("SampleLib");

            var type = lib.MainModule.CreateType("MyNamespace", "MyClass");

            _ = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                    .AddExport();

            _ = type.CreateMethod("DoSomeOtherStuff", typeof(int), ("longParameterNameHere", typeof(double)))
                  .AddExport("fancy_name");

            lib.Write("SampleLib.dll");

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp")
                .WithReferenceTo(lib);

            var files = generator.GenerateGlueCode(assembly, Directory.GetCurrentDirectory(), new HashSet<string> { "SampleLib" });

            var file = files.Single(f => f.Name == "exports.c");
            var expected = File.ReadAllText("snapshots/reference-exports.txt");
            file.Content.Trim().ShouldBe(expected, StringCompareShould.IgnoreLineEndings);

            AssertContent(env, files, "env.c");
        }

        // This breaks Cecil, see: https://github.com/jbevain/cecil/issues/926
        //[Fact]
        public void CanImportFromReferences()
        {
            var env = "// env stuff";
            var generator = new FFIGenerator(env, (m) => { });

            var lib = CecilExtensions.CreateSampleAssembly("SampleLib2");
            var type = lib.MainModule.CreateType("MyNamespace", "MyClass");

            var m1 = type.CreateMethod("DoSomething", typeof(void), ("p1", typeof(int)), ("p2", typeof(byte)), ("p3", typeof(long)))
                .AddImport("host", "do_something");

            var m2 = type.CreateMethod("GetLength", typeof(int), ("p1", typeof(float)))
                .AddImport("host", null);

            lib.Write("SampleLib2.dll");

            var assembly = CecilExtensions.CreateSampleAssembly("SampleApp")
                .WithReferenceTo(lib);

            var files = generator.GenerateGlueCode(assembly, Directory.GetCurrentDirectory(), new HashSet<string> { "SampleLib2" });

            var hostFile = files.Single(f => f.Name == "host.c");
            var expected = File.ReadAllText("snapshots/import-references.txt");
            hostFile.Content.Trim().ShouldBe(expected, StringCompareShould.IgnoreLineEndings);

            AssertContent(env, files, "env.c");
            files.ShouldNotContain(f => f.Name == "export.c");
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

        public static AssemblyDefinition WithReferenceTo(this AssemblyDefinition main, AssemblyDefinition reference)
        {
            main.MainModule.AssemblyReferences.Add(reference.Name);
            return main;
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

            method.IsPInvokeImpl = true;
            method.IsPreserveSig = true;

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
namespace Extism.Pdk;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ExtismExportAttribute : Attribute
{
    public ExtismExportAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ExtismImportAttribute : Attribute
{
    public ExtismImportAttribute(string module, string name)
    {
        Module = module;
        Name = name;
    }

    public string Module { get; }
    public string Name { get; }
}

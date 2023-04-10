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

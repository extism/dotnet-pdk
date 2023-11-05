using System.Runtime.InteropServices;

namespace SampleLib;
public class Class1
{
    [DllImport("env", EntryPoint = "samplelib_import")]
    public static extern void samplelib_import();

    [UnmanagedCallersOnly(EntryPoint = "samplelib_export")]
    public static void samplelib_export()
    {
        samplelib_import();
    }

    public static void noop()
    {

    }
}

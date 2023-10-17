using System.Runtime.InteropServices;

namespace SampleLib;
public class Class1
{
    [DllImport("env", EntryPoint = "do_something")]
    public static extern void do_something();

    [UnmanagedCallersOnly(EntryPoint = "useful_method")]
    public static void useful_method()
    {

    }

    public static void noop()
    {

    }
}

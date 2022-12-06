using System.Runtime.CompilerServices;
namespace csharp_pdk;

internal class Interop
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern ulong InputLength();

    public static int count_vowels()
    {
        return 2;
    }

 
}
// https://github.com/emepetres/dotnet-wasm-sample

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pdk;

public class Interop
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern int CountVowelsNative();

    public static int count_vowels()
    {
        return 442;
    }
}
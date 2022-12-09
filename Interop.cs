// https://github.com/emepetres/dotnet-wasm-sample

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Pdk;

public class Interop
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern int CountVowelsNative();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern byte* GetInput();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern long extism_input_length();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern long extism_input_load_u64(long index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern byte extism_input_load_u8(int index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void load_input(byte* buffer, long length);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern int do_something(ulong index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public extern static int native_power(int number);

    public unsafe static int count_vowels()
    {
        var length = extism_input_length();
        if (length == 0)
        {
            return 0;
            // return native_power(0);
            //var b = do_something(0);
        }

        //fixed (byte* bytesPtr = bytes)
        //{
        //    load_input(bytesPtr, length);
        //}

        //   return 92;

        var buffer = new byte[length];

        //extism_input_load_u8(0);

        for (int i = 0; i < length; i++)
        {
            //if (length - i >= 8)
            //{
            //    var x = extism_input_load_u64(i);
            //    var bytes = bitconverter.getbytes(x);
            //    array.copy(bytes, 0, buffer, i, bytes.length);
            //    i += 7;
            //}
            //else
            //{
            buffer[i] = extism_input_load_u8(i);
            // }
        }

        var text = Encoding.UTF8.GetString(buffer);

        var count = 0;
        foreach (var c in text)
        {
            var lower = char.ToLowerInvariant(c);
            switch (lower)
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                    count++;
                    break;
            }
        }

        return count;
    }
}
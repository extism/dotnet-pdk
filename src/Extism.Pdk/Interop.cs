// https://github.com/emepetres/dotnet-wasm-sample

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Extism.Pdk.Native;

public class Interop
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern long extism_input_length();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern ulong extism_input_load_u64(int index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern byte extism_input_load_u8(int index);
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void set_output(byte* buffer, int length);

    public static unsafe int count_vowels()
    {
        byte[] buffer = GetInput();

        var text = Encoding.UTF8.GetString(buffer);

        var count = 0;
        foreach (var c in text)
        {
            // Something really weird, char.ToLower() throws a `wasm trap: indirect call type mismatch`
            // exception unless in Main we call CountVowelsNative inside Console.WriteLine in Main.
            // See: https://github.com/mhmd-azeez/csharp-pdk/blob/baa6dda079fbc8bea0319b494fa359d10707bd63/Program.cs#L7

            switch (c)
            {
                case 'a':
                case 'A':
                case 'e': 
                case 'E':
                case 'i':
                case 'I':
                case 'o':
                case 'O':
                case 'u':
                case 'U':
                    count++;
                    break;
            }
        }

        var result = "{ \"count\": " + count + " }";
        var resultBytes = Encoding.UTF8.GetBytes(result);

        fixed (byte* ptr = resultBytes)
        {
            set_output(ptr, resultBytes.Length);
        }

        return 0;
    }

    public static byte[] GetInput()
    {
        var length = extism_input_length();
        if (length == 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[length];

        for (int i = 0; i < length; i++)
        {
            if (length - i >= 8)
            {
                var x = extism_input_load_u64(i);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(i), x);
                i += 7;
            }
            else
            {
                buffer[i] = extism_input_load_u8(i);
            }
        }

        return buffer;
    }
}
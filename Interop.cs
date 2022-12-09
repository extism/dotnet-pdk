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
    public static extern long extism_input_length();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern ulong extism_input_load_u64(int index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern byte extism_input_load_u8(int index);

    public static int count_vowels()
    {
        byte[] buffer = GetInput();

        var text = Encoding.UTF8.GetString(buffer);

        var count = 0;
        foreach (var c in text)
        {
            var lower = char.ToLower(c);
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

        return count;
    }

    private static byte[] GetInput()
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
                var bytes = BitConverter.GetBytes(x);
                Array.Copy(bytes, 0, buffer, i, bytes.Length);
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
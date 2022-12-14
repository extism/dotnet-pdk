using Extism.Pdk.Native;

using System.Text;

namespace SamplePlugin
{
    public class Functions
    {
        public static unsafe int CountVowels()
        {
            byte[] buffer = Interop.GetInput();

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
                Interop.set_output(ptr, resultBytes.Length);
            }

            return 0;
        }

    }
}

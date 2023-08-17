using Extism.Pdk;

using System.Runtime.CompilerServices;
using System.Text;

namespace SampleCSharpPlugin
{
    public class Functions
    {
        //[ExtismImport("host", "is_vowel")]
        //[MethodImpl(MethodImplOptions.InternalCall)]
        //public static extern int IsVowel(int vowel);

        [ExtismExport("count_vowels")]
        public static unsafe int CountVowels()
        {
            //var text = Pdk.GetInputString();

            //var count = 0;
            //foreach (var c in text)
            //{
            //    // Something really weird, char.ToLower() throws a `wasm trap: indirect call type mismatch`
            //    // exception unless in Main we call CountVowelsNative inside Console.WriteLine in Main.
            //    // See: https://github.com/mhmd-azeez/csharp-pdk/blob/baa6dda079fbc8bea0319b494fa359d10707bd63/Program.cs#L7

            //    var bytes = Encoding.UTF8.GetBytes(new char[] { c });
            //    var block = Pdk.Allocate(bytes);

            //    //if (IsVowel(block.Offset) > 1)
            //    //{
            //    //    count++;
            //    //}

            //    switch (c)
            //    {
            //        case 'a':
            //        case 'A':
            //        case 'e':
            //        case 'E':
            //        case 'i':
            //        case 'I':
            //        case 'o':
            //        case 'O':
            //        case 'u':
            //        case 'U':
            //            count++;
            //            break;
            //    }
            //}

            //var result = "{ \"count\": " + count + " }";

            //Pdk.SetOutput(result);
            return 0;
        }

    }
}

using Extism.Pdk;

using System.Runtime.CompilerServices;
using System.Text;

namespace SampleCSharpPlugin
{
    public class Functions
    {
        [ExtismImport("host", "is_vowel")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int IsVowel(int c);

        [ExtismExport("count_vowels")]
        public static unsafe int CountVowels()
        {
            var text = Pdk.GetInputString();

            var count = 0;
            foreach (var c in text)
            {
                if (IsVowel((byte)c) > 0)
                {
                    count++;
                }
            }

            var result = "{ \"count\": " + count + " }";

            Pdk.SetOutput(result);
            return 0;
        }

    }
}

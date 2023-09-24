using Extism.Pdk;
using System.Runtime.InteropServices;

namespace SampleCSharpPlugin
{
    public static class Functions
    {
        [DllImport("host", EntryPoint = "is_vowel")]
        public static extern int IsVowel(int c);

        [UnmanagedCallersOnly(EntryPoint = "count_vowels")]
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

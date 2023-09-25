using System.Runtime.InteropServices;
using System;
using Extism;
using System.Text.Json.Serialization;
using System.Text.Json;

Console.WriteLine("Hello world!");

namespace Functions
{
    public class Functions
    {
        [UnmanagedCallersOnly(EntryPoint = "len")]
        public static int Length()
        {
            var text = Pdk.GetInputString();

            Pdk.SetOutput(text.Length.ToString());
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "concat")]
        public static int Concat()
        {
            try
            {
                var json = Pdk.GetInput();
                var payload = JsonSerializer.Deserialize<ConcatInput>(json);

                if (payload is null)
                {
                    Pdk.Log(LogLevel.Error, "Failed to deserialize input.");
                    return 3;
                }

                Pdk.SetOutput(string.Join(payload.Separator, payload.Parts));
                return 0;
            }
            catch (Exception ex)
            {
                Pdk.Log(LogLevel.Error, ex.Message);
                return 2;
            }
        }
    }

    public class ConcatInput
    {
        public string[] Parts { get; set; }
        public string? Separator { get; set; }
    }
}

using System;
using System.Runtime.InteropServices;

namespace Extism;

internal static class Program
{
    internal static void Main() {
        // load-bearing dummy function
    }
}

internal class Native
{

    [DllImport("extism")]
    internal static unsafe extern ulong extism_input_length();

    [DllImport("extism")]
    internal static unsafe extern ulong extism_length(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_alloc(ulong n);

    [DllImport("extism")]
    internal static unsafe extern void extism_free(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern byte extism_input_load_u8(ulong index);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_input_load_u64(ulong index);

    [DllImport("extism")]
    internal static unsafe extern void extism_output_set(ulong offset, ulong n);

    [DllImport("extism")]
    internal static unsafe extern void extism_error_set(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_config_get(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_var_get(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_var_set(ulong keyOffset, ulong valueOffset);

    [DllImport("extism")]
    internal static unsafe extern void extism_store_u8(ulong offset, byte value);

    [DllImport("extism")]
    internal static unsafe extern byte extism_load_u8(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_store_u64(ulong offset, ulong value);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_load_u64(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_http_request(ulong requestOffset, ulong bodyOffset);

    [DllImport("extism")]
    internal static unsafe extern ushort extism_http_status_code();

    [DllImport("extism")]
    internal static unsafe extern void extism_log_info(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_log_debug(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_log_warn(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_log_error(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_store(ulong offset, byte* buffer, ulong n);
    [DllImport("extism")]
    internal static unsafe extern void extism_load(ulong offset, byte* buffer, ulong n);
    [DllImport("extism")]
    internal static unsafe extern void extism_load_input(byte* buffer, ulong n);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_http_headers();

    // Logging functions with levels
    [DllImport("extism")]
    internal static unsafe extern int extism_get_log_level();

    [DllImport("extism")]
    internal static unsafe extern void extism_log_trace(ulong offset);

    internal unsafe static void PrintException(Exception ex)
    {
        var message = ex.ToString();
        var block = Pdk.Allocate(message);
        extism_error_set(block.Offset);
    }
}

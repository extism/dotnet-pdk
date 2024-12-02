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
    // Input handling functions
    [DllImport("extism")]
    internal static unsafe extern ulong extism_input_offset();

    [DllImport("extism")]
    internal static unsafe extern ulong extism_input_length();

    // Memory management functions
    [DllImport("extism")]
    internal static unsafe extern ulong extism_length(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_alloc(ulong n);

    [DllImport("extism")]
    internal static unsafe extern void extism_free(ulong offset);

    // Memory operations with better efficiency
    [DllImport("extism")]
    internal static unsafe extern bool extism_load_from_handle(ulong src, ulong src_offset, byte* dest, ulong n);

    [DllImport("extism")]
    internal static unsafe extern bool extism_store_to_handle(ulong dest, ulong dest_offset, byte* buffer, ulong n);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_alloc_buf(byte* src, ulong n);

    // Input loading with verification
    [DllImport("extism")]
    internal static unsafe extern bool extism_load_input(ulong src_offset, byte* dest, ulong n);

    [DllImport("extism")]
    internal static unsafe extern bool extism_load_sz(ulong src, ulong src_offset, byte* dest, ulong n);

    // Output functions
    [DllImport("extism")]
    internal static unsafe extern void extism_output_set(ulong offset, ulong n);

    [DllImport("extism")]
    internal static unsafe extern bool extism_output_set_from_handle(ulong handle, ulong offset, ulong n);

    [DllImport("extism")]
    internal static unsafe extern void extism_output_handle(ulong handle);

    [DllImport("extism")]
    internal static unsafe extern void extism_output_buf(byte* src, ulong n);

    // Error handling
    [DllImport("extism")]
    internal static unsafe extern void extism_error_set(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_error_set_buf(byte* message, ulong messageLen);

    // Configuration and variables
    [DllImport("extism")]
    internal static unsafe extern ulong extism_config_get(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern ulong extism_var_get(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_var_set(ulong keyOffset, ulong valueOffset);

    // HTTP functions
    [DllImport("extism")]
    internal static unsafe extern ulong extism_http_request(ulong requestOffset, ulong bodyOffset);

    [DllImport("extism")]
    internal static unsafe extern ushort extism_http_status_code();

    [DllImport("extism")]
    internal static unsafe extern ulong extism_http_headers();

    // Logging functions with levels
    [DllImport("extism")]
    internal static unsafe extern int extism_get_log_level();

    [DllImport("extism")]
    internal static unsafe extern void extism_log_trace(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_log_debug(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_log_info(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_log_warn(ulong offset);

    [DllImport("extism")]
    internal static unsafe extern void extism_log_error(ulong offset);
    internal unsafe static void PrintException(Exception ex)
    {
        var message = ex.ToString();
        var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        fixed (byte* ptr = messageBytes)
        {
            extism_error_set_buf(ptr, (ulong)messageBytes.Length);
        }
    }
}

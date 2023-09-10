// https://github.com/emepetres/dotnet-wasm-sample

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Extism.Pdk;

public static class Program
{
    public static void Main() {
        // load-bearing dummy function
    }
}

public class Native
{

    [DllImport("env")]
    public static unsafe extern long extism_input_length();

    [DllImport("env")]
    public static unsafe extern long extism_length(long offset);

    [DllImport("env")]
    public static unsafe extern int extism_alloc(int n);

    [DllImport("env")]
    public static unsafe extern void extism_free(long offset);

    [DllImport("env")]
    public static unsafe extern byte extism_input_load_u8(int index);

    [DllImport("env")]
    public static unsafe extern ulong extism_input_load_u64(int index);

    [DllImport("env")]
    public static unsafe extern void extism_output_set(int offset, int n);

    [DllImport("env")]
    public static unsafe extern void extism_error_set(long offset);

    [DllImport("env")]
    public static unsafe extern long extism_config_get(long offset);

    [DllImport("env")]
    public static unsafe extern long extism_var_get(long offset);

    [DllImport("env")]
    public static unsafe extern void extism_var_set(long keyOffset, long valueOffset);

    [DllImport("env")]
    public static unsafe extern void extism_store_u8(long offset, byte value);

    [DllImport("env")]
    public static unsafe extern byte extism_load_u8(long offset);

    [DllImport("env")]
    public static unsafe extern void extism_store_u64(long offset, ulong value);

    [DllImport("env")]
    public static unsafe extern ulong extism_load_u64(long offset);

    [DllImport("env")]
    public static unsafe extern long extism_http_request(long requestOffset, long bodyOffset);

    [DllImport("env")]
    public static unsafe extern ushort extism_http_status_code();

    [DllImport("env")]
    public static unsafe extern void extism_log_info(long offset);

    [DllImport("env")]
    public static unsafe extern void extism_log_debug(long offset);

    [DllImport("env")]
    public static unsafe extern void extism_log_warn(long offset);

    [DllImport("env")]
    public static unsafe extern void extism_log_error(long offset);

    [DllImport("env")]
    public static unsafe extern void extism_store(long offset, byte* buffer, int n);
    [DllImport("env")]
    public static unsafe extern void extism_load(long offset, byte* buffer, int n);
    [DllImport("env")]
    public static unsafe extern void extism_load_input(byte* buffer, long n);
}

// https://github.com/emepetres/dotnet-wasm-sample

using System.Runtime.CompilerServices;

namespace Extism.Pdk;

public class Native
{

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern long extism_input_length();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern long extism_length(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern int extism_alloc(int n);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_free(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern byte extism_input_load_u8(int index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern ulong extism_input_load_u64(int index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_output_set(int offset, int n);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_error_set(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern long extism_config_get(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern long extism_var_get(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_var_set(long keyOffset, long valueOffset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_store_u8(long offset, byte value);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern byte extism_load_u8(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_store_u64(long offset, ulong value);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern ulong extism_load_u64(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern long extism_http_request(long requestOffset, long bodyOffset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern ushort extism_http_status_code();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_log_info(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_log_debug(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_log_warn(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_log_error(long offset);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_store(int offset, byte* buffer, int n);
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_load(int offset, byte* buffer, int n);
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void extism_load_input(byte* buffer, long n);
}

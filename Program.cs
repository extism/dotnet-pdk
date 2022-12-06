using System;
using System.Runtime.InteropServices.JavaScript;

[JSImport("extism_input_length", "env")]
static extern ulong extism_input_length();
static extern ulong extism_input_load_u8(ulong offs);
static extern ulong extism_input_load_ulong(ulong offs);
static extern ulong extism_length(ulong a);
static extern ulong extism_alloc(ulong a);
static extern void extism_free(ulong a);
static extern void extism_output_set(ulong a, ulong b);
static extern void extism_error_set(ulong a);
static extern ulong extism_config_get(ulong a);
static extern ulong extism_var_get(ulong a);
static extern void extism_var_set(ulong a, ulong b);
static extern void extism_store_u8(ulong a, ushort b);
static extern ushort extism_load_u8(ulong a);
static extern void extism_store_uint(ulong a, uint b);
static extern uint extism_load_uint(ulong a);
static extern void extism_store_ulong(ulong a, ulong b);
static extern ulong extism_load_ulong(ulong a);
static extern ulong extism_http_request(ulong a, ulong b);
static extern void extism_log_warn(ulong offs);
static extern void extism_log_info(ulong offs);
static extern void extism_log_debug(ulong offs);
static extern void extism_log_error(ulong offs);


public partial class WhatClass {
  //[System.Runtime.InteropServices.UnmanagedCallersOnly(EntryPoint = "count_vowels")]
  [JSExport]
  internal static int count_vowels() {
    return 0;
  }

  [JSImport("window.location.href", "main.js")]
  internal static partial string GetHRef();
}


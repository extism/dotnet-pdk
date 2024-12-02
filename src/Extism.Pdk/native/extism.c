#include <string.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>

// https://github.com/dotnet/runtime/blob/v7.0.0/src/mono/wasi/mono-wasi-driver/driver.c
#include <string.h>

#include "driver.h"

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <stdbool.h>
#include <stdio.h>

#define IMPORT(a, b) __attribute__((import_module(a), import_name(b)))

typedef uint64_t ExtismPointer;

// Input handling functions
IMPORT("extism:host/env", "input_offset")
extern ExtismPointer extism_input_offset_import();

ExtismPointer extism_input_offset() {
    return extism_input_offset_import();
}

IMPORT("extism:host/env", "input_length")
extern uint64_t extism_input_length_import();

uint64_t extism_input_length() {
    return extism_input_length_import();
}

// Memory management
IMPORT("extism:host/env", "length")
extern uint64_t extism_length_import(ExtismPointer);

uint64_t extism_length(ExtismPointer p) {
    return extism_length_import(p);
}

IMPORT("extism:host/env", "alloc")
extern ExtismPointer extism_alloc_import(uint64_t size);

ExtismPointer extism_alloc(uint64_t size) {
    return extism_alloc_import(size);
}

IMPORT("extism:host/env", "free")
extern void extism_free_import(ExtismPointer p);

void extism_free(ExtismPointer p) {
    extism_free_import(p);
}

// Memory operations with verification
IMPORT("extism:host/env", "load_from_handle")
extern bool extism_load_from_handle_import(ExtismPointer src, uint64_t src_offset, uint8_t* dest, uint64_t n);

bool extism_load_from_handle(ExtismPointer src, uint64_t src_offset, uint8_t* dest, uint64_t n) {
    return extism_load_from_handle_import(src, src_offset, dest, n);
}

IMPORT("extism:host/env", "store_to_handle")
extern bool extism_store_to_handle_import(ExtismPointer dest, uint64_t dest_offset, const uint8_t* buffer, uint64_t n);

bool extism_store_to_handle(ExtismPointer dest, uint64_t dest_offset, const uint8_t* buffer, uint64_t n) {
    return extism_store_to_handle_import(dest, dest_offset, buffer, n);
}

// Buffer allocation and management
IMPORT("extism:host/env", "alloc_buf")
extern ExtismPointer extism_alloc_buf_import(const uint8_t* src, uint64_t n);

ExtismPointer extism_alloc_buf(const uint8_t* src, uint64_t n) {
    return extism_alloc_buf_import(src, n);
}

// Input loading with verification
IMPORT("extism:host/env", "load_input")
extern bool extism_load_input_import(uint64_t src_offset, uint8_t* dest, uint64_t n);

bool extism_load_input(uint64_t src_offset, uint8_t* dest, uint64_t n) {
    return extism_load_input_import(src_offset, dest, n);
}

IMPORT("extism:host/env", "load_sz")
extern bool extism_load_sz_import(ExtismPointer src, uint64_t src_offset, uint8_t* dest, uint64_t n);

bool extism_load_sz(ExtismPointer src, uint64_t src_offset, uint8_t* dest, uint64_t n) {
    return extism_load_sz_import(src, src_offset, dest, n);
}

// Output functions
IMPORT("extism:host/env", "output_set")
extern void extism_output_set_import(ExtismPointer p, uint64_t value);

void extism_output_set(ExtismPointer p, uint64_t value) {
    extism_output_set_import(p, value);
}

IMPORT("extism:host/env", "output_set_from_handle")
extern bool extism_output_set_from_handle_import(ExtismPointer handle, uint64_t offset, uint64_t n);

bool extism_output_set_from_handle(ExtismPointer handle, uint64_t offset, uint64_t n) {
    return extism_output_set_from_handle_import(handle, offset, n);
}

IMPORT("extism:host/env", "output_handle")
extern void extism_output_handle_import(ExtismPointer handle);

void extism_output_handle(ExtismPointer handle) {
    extism_output_handle_import(handle);
}

IMPORT("extism:host/env", "output_buf")
extern void extism_output_buf_import(const uint8_t* src, uint64_t n);

void extism_output_buf(const uint8_t* src, uint64_t n) {
    extism_output_buf_import(src, n);
}

// Error handling
IMPORT("extism:host/env", "error_set")
extern void extism_error_set_import(ExtismPointer p);

void extism_error_set(ExtismPointer p) {
    extism_error_set_import(p);
}

IMPORT("extism:host/env", "error_set_buf")
extern void extism_error_set_buf_import(const uint8_t* message, uint64_t messageLen);

void extism_error_set_buf(const uint8_t* message, uint64_t messageLen) {
    extism_error_set_buf_import(message, messageLen);
}

// Configuration and variables
IMPORT("extism:host/env", "config_get")
extern ExtismPointer extism_config_get_import(ExtismPointer p);

ExtismPointer extism_config_get(ExtismPointer p) {
    return extism_config_get_import(p);
}

IMPORT("extism:host/env", "var_get")
extern ExtismPointer extism_var_get_import(ExtismPointer p);

ExtismPointer extism_var_get(ExtismPointer p) {
    return extism_var_get_import(p);
}

IMPORT("extism:host/env", "var_set")
extern void extism_var_set_import(ExtismPointer p1, ExtismPointer p2);

void extism_var_set(ExtismPointer p1, ExtismPointer p2) {
    extism_var_set_import(p1, p2);
}

// HTTP functions
IMPORT("extism:host/env", "http_request")
extern ExtismPointer extism_http_request_import(ExtismPointer p1, ExtismPointer p2);

ExtismPointer extism_http_request(ExtismPointer p1, ExtismPointer p2) {
    return extism_http_request_import(p1, p2);
}

IMPORT("extism:host/env", "http_status_code")
extern int32_t extism_http_status_code_import();

int32_t extism_http_status_code() {
    return extism_http_status_code_import();
}

IMPORT("extism:host/env", "http_headers")
extern ExtismPointer extism_http_headers_import();

ExtismPointer extism_http_headers() {
    return extism_http_headers_import();
}

// Logging functions
IMPORT("extism:host/env", "get_log_level")
extern int32_t extism_get_log_level_import();

int32_t extism_get_log_level() {
    return extism_get_log_level_import();
}

IMPORT("extism:host/env", "log_trace")
extern void extism_log_trace_import(ExtismPointer p);

void extism_log_trace(ExtismPointer p) {
    extism_log_trace_import(p);
}

IMPORT("extism:host/env", "log_debug")
extern void extism_log_debug_import(ExtismPointer p);

void extism_log_debug(ExtismPointer p) {
    extism_log_debug_import(p);
}

IMPORT("extism:host/env", "log_info")
extern void extism_log_info_import(ExtismPointer p);

void extism_log_info(ExtismPointer p) {
    extism_log_info_import(p);
}

IMPORT("extism:host/env", "log_warn")
extern void extism_log_warn_import(ExtismPointer p);

void extism_log_warn(ExtismPointer p) {
    extism_log_warn_import(p);
}

IMPORT("extism:host/env", "log_error")
extern void extism_log_error_import(ExtismPointer p);

void extism_log_error(ExtismPointer p) {
    extism_log_error_import(p);
}

// Wrap mono_runtime_run_main so that we ensure at least one argument is passed in to Mono
// otherwise it crashes, we use the -Wl,--wrap flag to instruct the linker to replace mono_runtime_run_main with
// __wrap_mono_runtime_run_main everywhere. see:
// - build/Extism.Pdk.targets
// - https://gist.github.com/mlabbe/a0b7b14be652085341162321a0a08530
// - https://github.com/dotnet/runtime/blob/4101144c8dde177addfb93ac46425fd1a8604f7a/src/mono/mono/metadata/object.c#L4175
int __real_mono_runtime_run_main(MonoMethod* method, int argc, char* argv[], MonoObject** exc);

int __wrap_mono_runtime_run_main(MonoMethod* method, int argc, char* argv[], MonoObject** exc)
{
    if (argc == 0)
    {
        char* temp[] = { "extism", NULL };
        argv = temp;
        argc = 1;
    }
    return __real_mono_runtime_run_main(method, argc, argv, exc);
}

// Wrap mono_wasm_load_runtime to make sure we don't initialize mono more than once

void __real_mono_wasm_load_runtime(const char* unused, int debug_level);

bool mono_runtime_initialized = false;
void __wrap_mono_wasm_load_runtime(const char* unused, int debug_level) {
    if (mono_runtime_initialized) {
        return;
    }
    __real_mono_wasm_load_runtime(unused, debug_level);
    mono_runtime_initialized = true;
}
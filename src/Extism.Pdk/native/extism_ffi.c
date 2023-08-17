#pragma once
#define NDEBUG
// https://github.com/dotnet/runtime/blob/v7.0.0/src/mono/wasi/mono-wasi-driver/driver.c
#include <string.h>

#include <mono-wasi/driver.h>
#include <mono/metadata/exception.h>

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <stdbool.h>

// Convert two int32_t values to a single uint64_t value
uint64_t combine_int32s_to_uint64(int32_t high, int32_t low) {
	return ((uint64_t)high << 32) | (uint32_t)low;
}


#define IMPORT(a, b) __attribute__((import_module(a), import_name(b)))

typedef uint64_t ExtismPointer;

IMPORT("env", "extism_input_length") extern uint64_t extism_input_length();
IMPORT("env", "extism_length") extern uint64_t extism_length(ExtismPointer);
IMPORT("env", "extism_alloc") extern ExtismPointer extism_alloc(uint64_t);
IMPORT("env", "extism_free") extern void extism_free(ExtismPointer);

IMPORT("env", "extism_input_load_u8")
extern uint8_t extism_input_load_u8(ExtismPointer);

IMPORT("env", "extism_input_load_u64")
extern uint64_t extism_input_load_u64(ExtismPointer);

IMPORT("env", "extism_output_set")
extern void extism_output_set(ExtismPointer, uint64_t);

IMPORT("env", "extism_error_set")
extern void extism_error_set(ExtismPointer);

IMPORT("env", "extism_config_get")
extern ExtismPointer extism_config_get(ExtismPointer);

IMPORT("env", "extism_var_get")
extern ExtismPointer extism_var_get(ExtismPointer);

IMPORT("env", "extism_var_set")
extern void extism_var_set(ExtismPointer, ExtismPointer);

IMPORT("env", "extism_store_u8")
extern void extism_store_u8(ExtismPointer, uint8_t);

IMPORT("env", "extism_load_u8")
extern uint8_t extism_load_u8(ExtismPointer);

IMPORT("env", "extism_store_u64")
extern void extism_store_u64(ExtismPointer, uint64_t);

IMPORT("env", "extism_load_u64")
extern uint64_t extism_load_u64(ExtismPointer);

IMPORT("env", "extism_http_request")
extern ExtismPointer extism_http_request(ExtismPointer, ExtismPointer);

IMPORT("env", "extism_http_status_code")
extern int32_t extism_http_status_code();

IMPORT("env", "extism_log_info")
extern void extism_log_info(ExtismPointer);
IMPORT("env", "extism_log_debug")
extern void extism_log_debug(ExtismPointer);
IMPORT("env", "extism_log_warn")
extern void extism_log_warn(ExtismPointer);
IMPORT("env", "extism_log_error")
extern void extism_log_error(ExtismPointer);

static void extism_load(int offs, uint8_t* buffer, int length) {
	uint64_t n;
	uint64_t left = 0;

	for (uint64_t i = 0; i < length; i += 1) {
		left = length - i;
		if (left < 8) {
			buffer[i] = extism_load_u8(offs + i);
			continue;
		}

		n = extism_load_u64(offs + i);
		*((uint64_t*)buffer + (i / 8)) = n;
		i += 7;
	}
}

static void extism_load_input(uint8_t* buffer, int length) {
	uint64_t n;
	uint64_t left = 0;

	for (uint64_t i = 0; i < length; i += 1) {
		left = length - i;
		if (left < 8) {
			buffer[i] = extism_input_load_u8(i);
			continue;
		}

		n = extism_input_load_u64(i);
		*((uint64_t*)buffer + (i / 8)) = n;
		i += 7;
	}
}

static void extism_store(uint32_t offs, const uint8_t* buffer, uint32_t length) {
	uint64_t n;
	uint64_t left = 0;
	for (uint64_t i = 0; i < length; i++) {
		left = length - i;
		if (left < 8) {
			extism_store_u8(offs + i, buffer[i]);
			continue;
		}

		n = *((uint64_t*)buffer + (i / 8));
		extism_store_u64(offs + i, n);
		i += 7;
	}
}

void set_output(int offset, int n) {
	extism_output_set(offset, n);
}
//void set_output(int offset, int length) {
//	ExtismPointer ptr = extism_alloc(n);
//	extism_store(errorPointer, out, n);
//	extism_output_set(errorPointer, n);
//}

uint8_t input_load_byte(int index)
{
	return extism_input_load_u8(index);
}

uint64_t input_load_long(int index)
{
	return extism_input_load_u64(index);
}

int32_t alloc(int32_t n)
{
	return extism_alloc(n);
}

void extism_pdk_attach_internal_calls()
{
	mono_add_internal_call("Extism.Pdk.Native::extism_input_length", extism_input_length);
	mono_add_internal_call("Extism.Pdk.Native::extism_length", extism_length);
	mono_add_internal_call("Extism.Pdk.Native::extism_alloc", alloc);
	mono_add_internal_call("Extism.Pdk.Native::extism_free", extism_free);
	mono_add_internal_call("Extism.Pdk.Native::extism_input_load_u8", input_load_byte);
	mono_add_internal_call("Extism.Pdk.Native::extism_input_load_u64", input_load_long);
	mono_add_internal_call("Extism.Pdk.Native::extism_output_set", set_output);
	mono_add_internal_call("Extism.Pdk.Native::extism_error_set", extism_error_set);
	mono_add_internal_call("Extism.Pdk.Native::extism_config_get", extism_config_get);
	mono_add_internal_call("Extism.Pdk.Native::extism_var_get", extism_var_get);
	mono_add_internal_call("Extism.Pdk.Native::extism_var_set", extism_var_set);
	mono_add_internal_call("Extism.Pdk.Native::extism_store_u8", extism_store_u8);
	mono_add_internal_call("Extism.Pdk.Native::extism_load_u8", extism_load_u8);
	mono_add_internal_call("Extism.Pdk.Native::extism_store_u64", extism_store_u64);
	mono_add_internal_call("Extism.Pdk.Native::extism_load_u64", extism_load_u64);
	mono_add_internal_call("Extism.Pdk.Native::extism_http_request", extism_http_request);
	mono_add_internal_call("Extism.Pdk.Native::extism_http_status_code", extism_http_status_code);
	mono_add_internal_call("Extism.Pdk.Native::extism_log_info", extism_log_info);
	mono_add_internal_call("Extism.Pdk.Native::extism_log_debug", extism_log_debug);
	mono_add_internal_call("Extism.Pdk.Native::extism_log_warn", extism_log_warn);
	mono_add_internal_call("Extism.Pdk.Native::extism_log_error", extism_log_error);

	mono_add_internal_call("Extism.Pdk.Native::extism_store", extism_store);
	mono_add_internal_call("Extism.Pdk.Native::extism_load", extism_load);
	mono_add_internal_call("Extism.Pdk.Native::extism_load_input", extism_load_input);
}


// These are generated by EmitWasmBundleObjectFile
const char* dotnet_wasi_getbundledfile(const char* name, int* out_length);
void dotnet_wasi_registerbundledassemblies();

#ifdef WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
// This is supplied from the MSBuild itemgroup @(WasiAfterRuntimeLoaded)
WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
#endif

__attribute__((export_name("_initialize"))) void initialize() {
	dotnet_wasi_registerbundledassemblies();

	mono_wasm_load_runtime("", 0);

#ifdef WASI_AFTER_RUNTIME_LOADED_CALLS
	// This is supplied from the MSBuild itemgroup @(WasiAfterRuntimeLoaded)
	WASI_AFTER_RUNTIME_LOADED_CALLS
#endif

}
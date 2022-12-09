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

static void extism_load(ExtismPointer offs, uint8_t* buffer, uint64_t length) {
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

static void extism_load_input(uint8_t* buffer, uint64_t length) {
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

static void extism_store(ExtismPointer offs, const uint8_t* buffer,
	uint64_t length) {
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

void set_output(uint8_t* out, int n) {
	ExtismPointer errorPointer = extism_alloc(n);
	extism_store(errorPointer, out, n);
	extism_output_set(errorPointer, n);
}

MonoMethod* method_CountVowels;
__attribute__((export_name("count_vowels"))) int count_vowels()
{
	if (!method_CountVowels)
	{
		method_CountVowels = lookup_dotnet_method("CSharp.Pdk.dll", "Pdk", "Interop", "count_vowels", -1);
		assert(method_CountVowels);
	}

	void* method_params[] = { };
	MonoObject* exception;
	MonoObject* result = mono_wasm_invoke_method(method_CountVowels, NULL, method_params, &exception);
	assert(!exception);

	int int_result = *(int*)mono_object_unbox(result);
	return int_result;
}

uint8_t get_byte(int index)
{
	return extism_input_load_u8(index);
}

uint64_t get_long(int index)
{
	return extism_input_load_u64(index);
}

void attach_internal_calls()
{
	mono_add_internal_call("Pdk.Interop::extism_input_load_u64", get_long);
	mono_add_internal_call("Pdk.Interop::extism_input_length", extism_input_length);
	mono_add_internal_call("Pdk.Interop::extism_input_load_u8", get_byte);
}
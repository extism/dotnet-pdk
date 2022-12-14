#pragma once
#define NDEBUG
// https://github.com/dotnet/runtime/blob/v7.0.0/src/mono/wasi/mono-wasi-driver/driver.c
#include <string.h>

#include <mono-wasi/driver.h>
#include <mono/metadata/exception.h>
#include <assert.h>

MonoMethod* method_CountVowels;
__attribute__((export_name("count_vowels"))) int count_vowels()
{
	if (!method_CountVowels)
	{
		method_CountVowels = lookup_dotnet_method("SamplePlugin.dll", "SamplePlugin", "Functions", "CountVowels", -1);
		assert(method_CountVowels);
	}

	void* method_params[] = { };
	MonoObject* exception;
	MonoObject* result = mono_wasm_invoke_method(method_CountVowels, NULL, method_params, &exception);
	assert(!exception);

	int int_result = *(int*)mono_object_unbox(result);
	return int_result;
}
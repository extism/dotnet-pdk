#pragma once
#define NDEBUG
#include <string.h>
#include <mono-wasi/driver.h>
#include <mono/metadata/exception.h>
#include <assert.h>


MonoMethod* method_count_vowels;
__attribute__((export_name("count_vowels"))) int count_vowels()
{
    if (!method_count_vowels)
    {
        method_count_vowels = lookup_dotnet_method("SamplePlugin.dll", "SamplePlugin", "Functions", "CountVowels", -1);
        assert(method_count_vowels);
    }

    void* method_params[] = { };
    MonoObject* exception;
    MonoObject* result = mono_wasm_invoke_method(method_count_vowels, NULL, method_params, &exception);
    assert(!exception);

    int int_result = *(int*)mono_object_unbox(result);
    return int_result;
}


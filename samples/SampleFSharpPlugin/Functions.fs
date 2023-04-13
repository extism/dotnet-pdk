
module SampleFSharpPlugin.Functions

// Use of `fixed`.
// Warning FS0009 Uses of this construct may result in the generation of unverifiable .NET IL code.
#nowarn "0009"

open System.Text

open Extism.Pdk
open Extism.Pdk.Native

[<ExtismExport("count_vowels")>]
let countVowels () =
  let buffer = Interop.GetInput ()
  let text = Encoding.UTF8.GetString buffer

  let mutable count = 0
  for c in text do
    match c with
    | 'a' | 'e' | 'i' | 'o' | 'u' 
    | 'A' | 'E' | 'I' | 'O' | 'U' -> count <- count + 1
    | _ -> ()

  let result = Encoding.UTF8.GetBytes $"""{{ "count" = {count} }}"""
  use ptr = fixed result
  Interop.set_output (ptr, result.Length)
  0
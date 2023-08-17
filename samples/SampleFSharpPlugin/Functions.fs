
module SampleFSharpPlugin.Functions

// Use of `fixed`.
// Warning FS0009 Uses of this construct may result in the generation of unverifiable .NET IL code.
#nowarn "0009"

open System.Text

open Extism.Pdk

[<ExtismExport("count_vowels")>]
let countVowels () =
  let buffer = Pdk.GetInput ()
  let text = Encoding.UTF8.GetString buffer

  let mutable count = 0
  for c in text do
    match c with
    | 'a' | 'e' | 'i' | 'o' | 'u' 
    | 'A' | 'E' | 'I' | 'O' | 'U' ->
      count <- count + 1
    | _ -> ()

  Pdk.SetOutput ($"""{{ "count": {count} }}""")
  0
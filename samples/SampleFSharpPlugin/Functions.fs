
module SampleFSharpPlugin.Functions

// Use of `fixed`.
// Warning FS0009 Uses of this construct may result in the generation of unverifiable .NET IL code.
// 
// warning FS0202: This attribute is currently unsupported by the F# compiler.
// Applying it will not achieve its intended effect.
#nowarn "0009"
#nowarn "0202"

open System.Text
open System.Runtime.InteropServices
open Extism.Pdk

[<DllImport("host", EntryPoint = "is_vowel")>]
extern int IsVowel(int c)

[<UnmanagedCallersOnly>]
let count_vowels () =
  let buffer = Pdk.GetInput ()
  let text = Encoding.UTF8.GetString buffer

  let mutable count = 0
  for c in text do
    let x = byte c
    let i = int x
    if (IsVowel (i)) > 0 then
      count <- count + 1

  Pdk.SetOutput ($"""{{ "count": {count} }}""")
  0
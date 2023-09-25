open System.Text
open Extism

let countVowels (input: string) =
    input
    |> Seq.filter (fun c -> "aeiouAEIOU".Contains(c))
    |> Seq.length

// Read configuration from the host
let readConfig () =
    match Pdk.TryGetConfig("thing") with
    | true, thing -> thing
    | false, _ -> "<unset by the host>"

// Read a variable persisted by the host
let readTotal () =
    match Pdk.TryGetVar("total") with
    | true, totalBlock ->
        Encoding.UTF8.GetString(totalBlock.ReadBytes()) |> int
    | false, _ ->
        Pdk.Log(LogLevel.Info, "First time running, total is not set.")
        0

// Write a variable persisted by the host
let saveTotal total =
    let totalBlock = Pdk.Allocate(total.ToString())
    Pdk.SetVar("total", totalBlock)

[<EntryPoint>]
let main args =
    let input = Pdk.GetInputString()
    let count = countVowels input
    let thing = readConfig()
    let total = readTotal() + count
    saveTotal total

    let output = sprintf """{"count": %d, "config": "%s", "total": "%d" }""" count thing total
    Pdk.SetOutput(output)
    0
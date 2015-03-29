/// This file is used to generate a CSV file from a set of disparate HTML websites.



#r @"..\packages\FSharp.Data.2.2.0\lib\net40\FSharp.Data.dll"
open FSharp.Data
open System
open System.IO

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

/// Some helper methods to parse a row
let toCsvRow (year, home, away, score) = sprintf "%s,%s,%s,%s,%s,%s,%s" year (fst home) (snd home) (fst away) (snd away) (fst score) (snd score)
let (|Score|_|) (field:string) =
    if field.Contains "Penalty" then None
    elif field.Contains "-" then Some(Score(string field.[0], string field.[2]))
    else None
let (|SingleMatch|DoubleMatch|Unknown|) (item:string array) =
    match item.[4], item.[5] with
    | Score home, Score away -> DoubleMatch(home, away)
    | Score home, _ -> SingleMatch home
    | _ -> Unknown

let parseRow year row =
    match row with
    | Unknown -> []
    | SingleMatch score -> [ year, (row.[0], row.[1]), (row.[2], row.[3]), score ]
    | DoubleMatch (firstResult, secondResult) ->
        [ year, (row.[0], row.[1]), (row.[2], row.[3]), firstResult
          year, (row.[2], row.[3]), (row.[0], row.[1]), secondResult ]

/// Get the data into an array of strings
/// Unfortunately we can't reuse a single type provider instance, so you have to modify the year, and URI and Table property.
let year = "1999"
type ResultData = HtmlProvider< @"http://kassiesa.home.xs4all.nl/bert/uefa/data/method2/match1999.html">
let results =
    ResultData
        .GetSample()
        .Tables.``UEFA European Cup Matches 1998/1999``.Rows
        |> Seq.map(fun r -> [| r.``CHAMPIONS LEAGUE``; r.``CHAMPIONS LEAGUE 2``; r.``CHAMPIONS LEAGUE 3``; r.``CHAMPIONS LEAGUE 4``; r.``CHAMPIONS LEAGUE 5``; r.``CHAMPIONS LEAGUE 6`` |])
        |> Seq.collect (parseRow year)
        |> Seq.map toCsvRow
        |> Seq.toArray

// Write to file
File.WriteAllLines(year + ".csv", results)

// Once you've downloaded all years from 1999 to 2014, merge them into one.
let csvRows =
    "Year,Home,Home Country,Away,Away Country,Home Goals,Away Goals" ::
    ([ 1999 .. 2014 ]
     |> List.map (sprintf "%d.csv")
     |> Seq.collect File.ReadAllLines
     |> Seq.toList)
File.WriteAllLines("all.csv", csvRows)
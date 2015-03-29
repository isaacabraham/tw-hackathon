#r @"..\packages\FSharp.Data.2.2.0\lib\net40\FSharp.Data.dll"
#r @"..\packages\Moserware.Skills.1.0.0.1\lib\net35\Moserware.Skills.dll"
#load @"..\packages\FsPlot.0.6.6\FsPlotBootstrap.fsx"
open FSharp.Data
open FsPlot.Highcharts.Charting
open Moserware.Skills
open System.Collections.Generic

type Results = CsvProvider< @"..\data\all.csv">

module TrueSkill =    
    let calculateTrueSkills (ratings:Map<string, Rating>) =
        let calculatePositions (result:Results.Row) =
            match result.``Home Goals``, result.``Away Goals`` with
            | a, b when a = b -> [| 1; 1 |]
            | a, b when a > b -> [| 1; 2 |]
            | _ -> [| 2; 1 |]
        
        (ratings, Results.GetSample().Rows)
        ||> Seq.fold(fun ratings result ->
            let opponents = [ dict [ result.Home, ratings.[result.Home] ]
                              dict [ result.Away, ratings.[result.Away] ] ]
            let positions = calculatePositions result
            let newRatings = TrueSkillCalculator.CalculateNewRatings(GameInfo.DefaultGameInfo, opponents, positions)
            
            ratings
            |> Map.remove result.Home
            |> Map.remove result.Away
            |> Map.add result.Home newRatings.[result.Home]
            |> Map.add result.Away newRatings.[result.Away])
    let createDefaultRatings() =
        Results.GetSample().Rows
        |> Seq.map(fun row -> row.Home)
        |> Seq.distinct
        |> Seq.map(fun team -> team, GameInfo.DefaultGameInfo.DefaultRating)
        |> Map.ofSeq

// Create an default starting Map with equal ratings for all teams
let ratings = TrueSkill.createDefaultRatings()

ratings    
|> TrueSkill.calculateTrueSkills
|> Map.toList
|> List.map(fun (name, rating) -> name, rating.ConservativeRating)
|> List.sortBy(fun (name, rating) -> -rating)
|> Seq.take 20
|> Seq.toList
|> Chart.Column

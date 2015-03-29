#r @"..\packages\FSharp.Data.2.2.0\lib\net40\FSharp.Data.dll"
#load @"..\packages\FsPlot.0.6.6\FsPlotBootstrap.fsx"
open FSharp.Data
open System.Collections.Generic
open FsPlot.Highcharts.Charting

type Results = CsvProvider< @"..\data\all.csv">

/// Use ELO to rate every team in the Champions League since 1999.
module ELO =
    let createDefaultRatings() =
        Results.GetSample().Rows
        |> Seq.distinctBy(fun row -> row.Home)
        |> Seq.map(fun row -> row.Home, 0.)
        |> Map.ofSeq

    let winProb dr = 1.0 / (10.**(-dr/400.0) + 1.0)
    let calc (r, dr, w) =
        let k = 100.
        let we = winProb dr
        r + k * (w - we)    
    let getWinLossOrDraw (home, away) =
        if home = away then 0.5
        elif home > away then 1.
        else 0.

// Pipe every result through ELO
let ratings =
    (ELO.createDefaultRatings(), Results.GetSample().Rows)
    ||> Seq.fold(fun ratings row -> 
        let homeElo = ratings.[row.Home]
        let awayElo = ratings.[row.Away]
        let dr = homeElo - awayElo    
        let rn1 = ELO.calc(homeElo, dr, (ELO.getWinLossOrDraw(row.``Home Goals``, row.``Away Goals``)))
        let rn2 = ELO.calc(awayElo, dr, (ELO.getWinLossOrDraw(row.``Away Goals``, row.``Home Goals``)))
        ratings
        |> Map.remove row.Home
        |> Map.remove row.Away
        |> Map.add row.Home rn1
        |> Map.add row.Away rn2)
    
// Chart top 20 teams
ratings
|> Map.toSeq
|> Seq.sortBy(fun (_, rating) -> -rating)
|> Seq.take 20
|> Chart.Column

// Arbitrary prediction
ELO.winProb(ratings.["Bayern München"] - ratings.["Celtic"])

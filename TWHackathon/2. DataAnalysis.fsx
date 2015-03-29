#r @"..\packages\FSharp.Data.2.2.0\lib\net40\FSharp.Data.dll"
#load @"..\packages\FsPlot.0.6.6\FsPlotBootstrap.fsx"
open FSharp.Data
open FsPlot.Highcharts.Charting

type Results = CsvProvider< @"..\data\all.csv">


//---------------------------------------


/// Analyses the ratio of matches played by teams for a given country over time
let getForCountry country =
    Results.GetSample().Rows
    |> Seq.groupBy(fun row -> row.Year)
    |> Seq.map(fun (year, matches) ->
        let totalMatches = matches |> Seq.length |> float
        let matchesForCountry = matches |> Seq.filter(fun m -> m.``Away Country`` = country || m.``Home Country`` = country) |> Seq.length |> float
        year, (matchesForCountry / totalMatches) * 100.)
    |> Seq.toList

/// just for eng
getForCountry "Eng" |> Chart.Line

/// for several countries
let countries = [ "Eng"; "Esp"; "Fra"; "Ger"; "Ita" ]

countries
|> List.map getForCountry
|> Chart.Line
|> Chart.WithNames countries
|> Chart.WithLegend true

//---------------------------------------




/// get distribution of teams per nation
Results.GetSample().Rows
|> Seq.map(fun row -> row.Home, row.``Home Country``)
|> Seq.distinctBy id
|> Seq.countBy snd
|> Seq.sortBy snd
|> Chart.Column
|> Chart.WithLegend true

//---------------------------------------

/// Get involvement of particular countries over time
let countriesByYear =
    Results.GetSample().Rows
    |> Seq.distinctBy(fun row -> row.Home, row.Year)
    |> Seq.groupBy(fun row -> row.``Home Country``)
    |> Seq.map(fun (country, results) ->
        country,
        results |> Seq.countBy(fun r -> r.Year) |> Seq.toList)
    |> Seq.toList

countriesByYear
|> List.map snd
|> Chart.StackedColumn
|> Chart.WithNames (countriesByYear |> List.map fst)

// Filter out long tail countries
let longTailFiltered =
    countriesByYear
    |> Seq.map(fun (country, results) ->
        country,
        results |> Seq.filter(fun (_,count) -> count >= 2))
    |> Seq.filter(fun (country, results) -> results |> Seq.length > 0)
    |> Seq.toList

longTailFiltered
|> List.map snd
|> Chart.StackedColumn
|> Chart.WithNames (longTailFiltered |> List.map fst)
|> Chart.WithLegend true

// --------------------------------------------

// Which countries have teams that got to semi-finals over time?
let countrySemiFinalists = 
    Results.GetSample().Rows
    |> Seq.groupBy(fun row -> row.Year)
    |> Seq.collect(fun (year, results) ->
        results
        |> Seq.toArray
        |> Array.rev
        |> fun array -> array.[ 1 .. 4 ]
        |> Seq.map(fun r -> r.Year, r.``Home Country``))
    |> Seq.countBy id
    |> Seq.groupBy(fun ((_,country), _) -> country)
    |> Seq.map(fun (country, results) -> country, results |> Seq.map(fun ((year, country), count) -> year, count))
    |> Seq.toList

// Stacked data
countrySemiFinalists
|> List.map snd
|> Chart.StackedColumn
|> Chart.WithNames (countrySemiFinalists |> List.map fst)
|> Chart.WithLegend true

// As a line chart, with missing years inserted as 0.
let semiFinalistsNormalised =
    let allYears = [ 1999 .. 2014 ] |> Set.ofList
    countrySemiFinalists
    |> Seq.map(fun (country, years) ->
        let yearsForCountry = years |> Seq.map fst |> Set.ofSeq
        let missingYears = allYears - yearsForCountry
        country,
        missingYears
        |> Seq.map(fun year -> year, 0)
        |> Seq.append years
        |> Seq.sortBy fst)

semiFinalistsNormalised
|> Seq.map snd
|> Chart.Line
|> Chart.WithNames (semiFinalistsNormalised |> Seq.map fst)
|> Chart.WithLegend true
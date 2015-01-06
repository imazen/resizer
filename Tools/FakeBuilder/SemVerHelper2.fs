module FakeBuilder.SemVerHelper2

open System.Text.RegularExpressions

let SemVerPattern = "^(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?(?:\+[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?$"

let isValidSemVer input =
    let m = Regex.Match(input, SemVerPattern)
    if m.Success then true
    else false

// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
module FakeBuilder.SemVerHelper2

open System.Text.RegularExpressions

let SemVerPattern = "^(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?(?:\+[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?$"

let isValidSemVer input =
    let m = Regex.Match(input, SemVerPattern)
    if m.Success then true
    else false

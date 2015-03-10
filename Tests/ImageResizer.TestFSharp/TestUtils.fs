// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

module ImageResizer.TestUtils

open System.Resources
open System.Reflection

exception MissingResource of string

let get800x600 ()=
  let stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("800x600white.png")
  if stream = null then raise (MissingResource("Failed to load embedded resource"))
  stream

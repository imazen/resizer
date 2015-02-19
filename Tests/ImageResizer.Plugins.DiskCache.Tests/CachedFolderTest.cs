// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace ImageResizer.Plugins.DiskCache.Tests {
    [TestFixture]
    public class CachedFolderTest {

        CachedFolder f;

        [FixtureSetUp]
        public void start() {
            f = new CachedFolder();
        }
        [Test]
        public void Test() {
            //
            // TODO: Add test logic here
            //
        }
    }
}

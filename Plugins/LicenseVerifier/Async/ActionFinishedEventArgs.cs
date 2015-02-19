// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;

namespace ImageResizer.Plugins.LicenseVerifier.Async {
    public class ActionFinishedEventArgs : EventArgs {
        public ActionState State { get; set; }
        public Exception Exception { get; set; }
    }
}

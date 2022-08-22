// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿namespace ImageResizerGUI
{
    /// <summary>
    /// Enumerate the saving modes for the UI.
    /// </summary>
    public enum SaveMode
    {
        // Modify the existing image.
        ModifyExisting, 
        // Export the results to the selected folder.
        ExportResults,
        // Export the results to the selected .zip destination file.
        CreateZipFile
    }
}

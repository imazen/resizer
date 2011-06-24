namespace ImageResizerGUI
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

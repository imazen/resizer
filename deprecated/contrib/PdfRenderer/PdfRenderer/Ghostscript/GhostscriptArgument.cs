namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    /// <summary>
    ///   Ghostscript argument names
    ///  
    /// </summary>
    /// <see cref = "!:http://ghostscript.com/doc/current/Use.htm" />
    public static class GhostscriptArgument
    {
        #region Rendering

        /// <summary>
        ///   Control the use of subsample antialiasing; highly recommended for producing high quality rasterizations. 
        ///   The subsampling box size n should be 4 for optimum output, but smaller values can be used for faster rendering. 
        ///   Allowed values are 1, 2 or 4.
        /// </summary>
        public const string TextAlphaBits = "-dTextAlphaBits=";

        /// <summary>
        ///   Control the use of subsample antialiasing; highly recommended for producing high quality rasterizations. 
        ///   The subsampling box size n should be 4 for optimum output, but smaller values can be used for faster rendering. 
        ///   Allowed values are 1, 2 or 4.
        /// </summary>
        /// <remarks>
        ///   Because of the way antialiasing blends the edges of shapes into the background when they are drawn some files 
        ///   that rely on joining separate filled polygons together to cover an area may not render as expected with 
        ///   GraphicsAlphaBits at 2 or 4. 
        ///   If you encounter strange lines within solid areas, try rendering that file again with -dGraphicsAlphaBits=1.
        /// </remarks>
        public const string GraphicsAlphaBits = "-dGraphicsAlphaBits=";

        /// <summary>
        ///   Chooses glyph alignment to integral pixel boundaries (if set to the value 1) or to subpixels (value 0). Subpixels are  
        ///   a smaller raster grid which is used internally for text antialiasing. The number of subpixels in a pixel usually is 
        ///   2^TextAlphaBits, but this may be automatically reduced for big characters to save space in character cache.
        /// </summary>
        /// Setting -dAlignToPixels=0 can improve rendering of poorly hinted fonts, but may impair the appearance of well-hinted 
        /// fonts.
        /// <remarks>
        ///   The parameter has no effect if -dTextAlphaBits=1. Default value is 0.
        /// </remarks>
        /// <example>
        ///   -dAlignToPixels=0
        /// </example>
        public const string AlignToPixels = "-dAlignToPixels=";

        /// <summary>
        ///   This specifies the initial value for the implementation specific user parameter GridFitTT. It controls grid fitting of 
        ///   True Type fonts (Sometimes referred to as "hinting", but strictly speaking the latter is a feature of Type 1 fonts). 
        ///   Setting this to 2 enables automatic grid fitting for True Type glyphs. The value 0 disables grid fitting. 
        ///   The default value is 2. For more information see the description of the user parameter GridFitTT.
        /// </summary>
        /// <remarks>
        ///   The parameter has no effect if -dTextAlphaBits=1. Default value is 0.
        /// </remarks>
        /// <example>
        ///   -dGridFitTT=0
        /// </example>
        public const string GridFitTT = "-dGridFitTT=";

        #endregion

        #region Page

        /// <summary>
        ///   Causes the media size to be fixed after initialization, forcing pages of other sizes or orientations to be clipped. 
        ///   This may be useful when printing documents on a printer that can handle their requested paper size but whose default 
        ///   is some other size.
        /// </summary>
        /// <example>
        ///   -dFIXEDMEDIA
        /// </example>
        public const string FixedMedia = "-dFIXEDMEDIA";

        /// <summary>
        ///   Specifies the output height in pixels.
        /// </summary>
        /// <example>
        ///   -dDEVICEHEIGHT=640
        /// </example>
        public const string Height = "-dDEVICEHEIGHT=";

        /// <summary>
        ///   Specifies the output width in pixels.
        /// </summary>
        /// <example>
        ///   -dDEVICEWIDTH=800
        /// </example>
        public const string Width = "-dDEVICEWIDTH=";

        #endregion

        #region Interaction

        /// <summary>
        ///   Causes Ghostscript to exit after processing all files named on the command line, rather than going into an 
        ///   interactive loop reading PostScript commands. Equivalent to putting -c quit at the end of the command line.
        /// </summary>
        public const string Batch = "-dBATCH";

        /// <summary>
        ///   Disables the prompt and pause at the end of each page. Normally one should use this (along with -dBATCH) when 
        ///   producing output on a printer or to a file; it also may be desirable for applications where another program is 
        ///   "driving" Ghostscript."
        /// </summary>
        public const string NoPause = "-dNOPAUSE";

        /// <summary>
        ///   Suppresses routine information comments on standard output. 
        ///   This is currently necessary when redirecting device output to standard output.
        /// </summary>
        public const string Quiet = "-dQUIET";

        #endregion

        #region Device/Output

        /// <summary>
        ///   Sets input file parameter.
        /// </summary>
        /// <example>
        ///   -sFILE=pdf_info.ps
        /// </example>
        public const string File = "-sFile=";

        /// <summary>
        ///   Initializes Ghostscript with a null device (a device that discards the output image) rather than the default device or 
        ///   the device selected with -sDEVICE=. This is usually useful only when running PostScript code whose purpose is to 
        ///   compute something rather than to produce an output image.
        /// </summary>
        public const string NoDisplay = "-dNODISPLAY";

        /// <summary>
        ///   Output device.
        /// </summary>
        /// <example>
        ///   -sDEVICE=bmp32b
        /// </example>
        public const string OutputDevice = "-sDEVICE=";

        /// <summary>
        ///   Output file path.
        /// </summary>
        /// <remarks>
        ///   File names beginning with the character % have a special meaning in PostScript and are not allowed.
        /// </remarks>
        public const string OutputFile = "-sOutputFile=";

        #endregion

        #region EPS

        /// <summary>
        ///   Resize an EPS file to fit the page. This is useful for enlarging an EPS file to fit the paper size when printing.
        /// </summary>
        public const string EpsFitPage = "-dEPSFitPage";

        /// <summary>
        ///   Crop an EPS file to the bounding box. This is useful when converting an EPS file to a bitmap.
        /// </summary>
        public const string EpsCrop = "-dEPSCrop";

        #endregion

        #region PDF

        /// <summary>
        ///   Rather than selecting a PageSize given by the PDF MediaBox, TrimBox (see -dUseTrimBox), or CropBox (see -dUseCropBox), 
        ///   the PDF file will be scaled to fit the current device page size (usually the default page size). This is useful to avoid 
        ///   clipping information on a PDF document when sending to a printer that may have unprintable areas at the edge of the media 
        ///   larger than allowed for in the document. This is also useful for creating fixed size images of PDF files that may have a 
        ///   variety of page sizes, for example thumbnail images.
        /// </summary>
        public const string PdfFitPage = "-dPDFFitPage";

        /// <summary>
        ///   Begins interpreting on the designated page of the PDF document.
        /// </summary>
        /// <example>
        ///   -dFirstPage=1
        /// </example>
        public const string FirstPage = "-dFirstPage=";

        /// <summary>
        ///   Stops interpreting on the designated page of the PDF document.
        /// </summary>
        /// <example>
        ///   -dLastPage=1
        /// </example>
        public const string LastPage = "-dLastPage=";

        /// <summary>
        ///   Determines whether the file should be displayed or printed using the "screen" or "printer" options for annotations and 
        ///   images. With -dPrinted, the output will use the file's "print" options; with -dPrinted=false, the output will use the 
        ///   file's "screen" options. If neither of these is specified, the output will use the screen options for any output device 
        ///   that doesn't have an OutputFile parameter, and the printer options for devices that do have this parameter.
        /// </summary>
        public const string Printed = "-dPrinted";

        /// <summary>
        ///   Sets the page size to the CropBox rather than the MediaBox. Some files have a CropBox that is smaller than the MediaBox 
        ///   and may include white space, registration or cutting marks outside the CropBox. Using this option will set the page size 
        ///   appropriately for a viewer.
        /// </summary>
        public const string UseCropBox = "-dUseCropBox";

        /// <summary>
        ///   Sets the page size to the TrimBox rather than the MediaBox. The trim box defines the intended dimensions of the finished 
        ///   page after trimming. Some files have a TrimBox that is smaller than the MediaBox and may include white space, registration 
        ///   or cutting marks outside the CropBox. Using this option simulates appearance of the finished printed page.
        /// </summary>
        public const string UseTrimBox = "-dUseTrimBox";

        /// <summary>
        ///   Sets the user or owner password to be used in decoding encrypted PDF files. For files created with encryption method 4 
        ///   or earlier, the password is an arbitrary string of bytes; with encryption method 5 or later, it should be text in either 
        ///   UTF-8 or your locale's character set (Ghostscript tries both).
        /// </summary>
        /// <example>
        ///   -sPDFPassword=mypassword
        /// </example>
        public const string PdfPassword = "-sPDFPassword=";

        /// <summary>
        ///   Don't enumerate annotations associated with the page objects through Annots attribute. Annotations are shown by default.
        /// </summary>
        /// <example>
        ///   -dShowAnnots=false
        /// </example>
        public const string ShowAnnotations = "-dShowAnnots=";

        #endregion

        #region Other

        /// <summary>
        ///   Disables the deletefile and renamefile operators, and the ability to open piped commands (%pipe%cmd) at all. 
        ///   Only %stdout and %stderr can be opened for writing. Disables reading of files other than %stdin, those given as a 
        ///   command line argument, or those contained on one of the paths given by LIBPATH and FONTPATH and specified by the 
        ///   system params /FontResourceDir and /GenericResourceDir. Also prevents changing the /GenericResourceDir, 
        ///   /FontResourceDir and either the /SystemParamsPassword or the /StartJobPassword.
        /// </summary>
        public const string Safer = "-dSAFER";

        #endregion

        #region Performance

        /// <summary>
        ///   On a multi-core system where multiple threads can be dispatched to individual processors/cores, banding mode may 
        ///   provide higher performance since -dNumRenderingThreads=# can be used to take advantage of more than one CPU core 
        ///   when rendering the clist. The number of threads should generally be set to the number of available processor cores 
        ///   for best throughput.
        /// </summary>
        /// <example>
        ///   -dNumRenderingThreads=
        /// </example>
        public const string RenderingThreads = "-dNumRenderingThreads=";

        /// <summary>
        ///   If the amount of memory required to hold the pixmap for the window is no more than the value of MaxBitmap, the driver 
        ///   will draw to a pixmap in Ghostscript's address space (called a "client-side pixmap") and will copy it to the screen 
        ///   from time to time; if the amount of memory required for the pixmap exceeds the value of MaxBitmap, the driver will 
        ///   draw to a server pixmap. Using a client-side pixmap usually provides better performance -- for bitmap images, possibly
        ///   much better performance -- but since it may require quite a lot of RAM (e.g., about 2.2 Mb for a 24-bit 1024x768 window), 
        ///   the default value of MaxBitmap is 0.
        /// </summary>
        /// <example>
        ///   -dMaxBitmap=24000000
        /// </example>
        public const string MaxBitmap = "-dMaxBitmap=";

        #endregion
    }
}
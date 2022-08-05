namespace ImageResizer.Configuration.Performance
{
    internal struct SegmentPrecision
    {
        /// <summary>
        ///     Inclusive (microseconds, 1 millionth of a second)
        /// </summary>
        public long Above { get; set; }

        public long Loss { get; set; }
    }
}
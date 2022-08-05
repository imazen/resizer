namespace ImageResizer.Configuration.Performance
{
    /// <summary>
    ///     Min/Max/Avg values for an interval
    /// </summary>
    internal struct IntervalStat
    {
        public NamedInterval Interval { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public long Avg { get; set; }
    }
}
using System;


namespace ImageResizer.Configuration.Performance
{
    class PercentileStream
    {
        float percentile;
        Random r = new Random();
        public PercentileStream(float percentile)
        {
            if (percentile < 0.001 || percentile > 0.999) throw new ArgumentException("percentile must be between 0.001 and 0.009");
            this.percentile = percentile;
        }
        int median = 0;
        public void Add(int value)
        {
            var v = r.NextDouble();
            if (value < median && v > 1 - percentile)
            {
                median++;
            }else if (value > median && v > percentile)
            {
                median--;
            }
        }

        public int GetValue() { return median; }
    }
}

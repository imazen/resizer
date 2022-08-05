using System;

namespace ImageResizer.Configuration.Performance
{
    internal struct AddMulModHash : IHash
    {
        private static readonly ulong prime = (ulong)(Math.Pow(2, 32) - 5.0); //(ulong)(Math.Pow(2,33) - 355.0);

        /// <summary>
        ///     Seed the random number generator with a good prime, or you'll get poor distribution
        /// </summary>
        /// <param name="r"></param>
        public AddMulModHash(IRandomDoubleGenerator r)
        {
            this.r = r;
            a = (ulong)(r.NextDouble() * (prime - 2)) + 1;
            b = (ulong)(r.NextDouble() * (prime - 2)) + 1;
        }

        private readonly ulong a;
        private readonly ulong b;
        private readonly IRandomDoubleGenerator r;

        public uint ComputeHash(uint value)
        {
            return (uint)((a * value + b) % prime);
        }

        public IHash GetNext()
        {
            return new AddMulModHash(r);
        }

        public static AddMulModHash DeterministicDefault()
        {
            return new AddMulModHash(new MersenneTwister(1499840347));
        }
    }
}
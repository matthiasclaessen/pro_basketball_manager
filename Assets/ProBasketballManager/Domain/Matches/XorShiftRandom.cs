namespace ProBasketballManager.Domain.Matches
{
    public sealed class XorShiftRandom : IRandomSource
    {
        private const int WarmUpIterations = 16;

        private uint _state;

        public XorShiftRandom(uint seed)
        {
            // A raw xorshift32 state produces heavily biased output for its first
            // draws when seeded with a small number, and seeds are typically small
            // sequential values (fixture IDs, match numbers). Advancing the
            // generator before handing out any value removes that bias.
            _state = seed == 0 ? 2463534242u : seed;

            for (var iteration = 0; iteration < WarmUpIterations; iteration++)
            {
                NextUInt();
            }
        }

        private uint NextUInt()
        {
            var value = _state;

            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;

            _state = value;

            return value;
        }

        public double NextDouble()
        {
            return NextUInt() / 4294967296.0;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            var range = maxExclusive - minInclusive;

            return minInclusive + (int)(NextDouble() * range);
        }
    }
}
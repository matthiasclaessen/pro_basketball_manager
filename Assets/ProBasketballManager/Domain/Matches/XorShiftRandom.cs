namespace ProBasketballManager.Domain.Matches
{
    public sealed class XorShiftRandom : IRandomSource
    {
        private uint _state;

        public XorShiftRandom(uint seed)
        {
            _state = seed == 0
                ? 1u
                : seed;
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
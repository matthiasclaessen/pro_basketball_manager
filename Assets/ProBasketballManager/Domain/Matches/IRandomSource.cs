namespace ProBasketballManager.Domain.Matches
{
    public interface IRandomSource
    {
        double NextDouble();

        int NextInt(int minInclusive, int maxExclusive);
    }
}
namespace OneDesk.Helpers.Dicebear;

public class Prng
{
    private const int Min = -2147483648; // Int32.MinValue
    private const int Max = 2147483647;  // Int32.MaxValue
    private string Seed { get; }
    private int _value;

    public Prng(string seed)
    {
        Seed = seed;
        _value = HashSeed(seed);
        if (_value == 0) _value = 1;
    }

    public int Next()
    {
        _value = Xorshift(_value);
        return _value;
    }

    public int Integer(int min, int max)
    {
        return (int)Math.Floor(((long)Next() - Min) / (double)((long)Max - Min) * ((long)max + 1 - min) + min);
    }

    public List<T> Shuffle<T>(IEnumerable<T> arr)
    {
        var internalPrng = new Prng(Next().ToString());
        var workingArray = arr.ToList();
        for (var i = workingArray.Count - 1; i > 0; i--)
        {
            var j = internalPrng.Integer(0, i);

            (workingArray[i], workingArray[j]) = (workingArray[j], workingArray[i]);
        }

        return workingArray;
    }

    private static int Xorshift(int value)
    {
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;

        return value;
    }

    private static int HashSeed(string seed)
    {
        var hash = 0;

        foreach (var t in seed)
        {
            hash = ((hash << 5) - hash + t) | 0;
            hash = Xorshift(hash);
        }

        return hash;
    }
}
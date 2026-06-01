using System;

namespace BoxingCore
{
    public interface IRandom
    {
        double NextDouble();        // [0,1)
        int Next(int maxExclusive); // [0,maxExclusive)
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly Random _r;
        public SystemRandom(int seed) { _r = new Random(seed); }
        public double NextDouble() => _r.NextDouble();
        public int Next(int maxExclusive) => _r.Next(maxExclusive);
    }

    // テスト用：与えた値を順に返す（尽きたら先頭へ巡回）
    public sealed class FakeRandom : IRandom
    {
        private readonly double[] _d; private readonly int[] _i;
        private int _di, _ii;
        public FakeRandom(double[] doubles, int[] ints = null)
        {
            _d = (doubles != null && doubles.Length > 0) ? doubles : new double[] { 0.0 };
            _i = (ints != null && ints.Length > 0) ? ints : new int[] { 0 };
        }
        public double NextDouble() { var v = _d[_di % _d.Length]; _di++; return v; }
        public int Next(int maxExclusive) { var v = _i[_ii % _i.Length]; _ii++; return v % maxExclusive; }
    }
}

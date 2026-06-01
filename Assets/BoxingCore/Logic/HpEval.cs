namespace BoxingCore
{
    public static class HpEval
    {
        // 0:1(≤25%) 2(≤50%) 3(≤75%) 4(>75%)
        private static int Quart(int hp, int max)
        {
            double r = max <= 0 ? 0.0 : (double)hp / max;
            if (r <= 0.25) return 1;
            if (r <= 0.5) return 2;
            if (r <= 0.75) return 3;
            return 4;
        }

        // 頭/腹の象限を BAN の合成規則で 1..10 に
        public static int Evaluate(int headHP, int headMax, int bodyHP, int bodyMax)
        {
            int h = Quart(headHP, headMax);
            int b = Quart(bodyHP, bodyMax);
            if (h == 1 && b == 1) return 3;
            if (h == 1) return 1;
            if (b == 1) return 2;
            if (h == 2 && b == 2) return 6;
            if (h == 2) return 4;
            if (b == 2) return 5;
            if (h == 3 && b == 3) return 9;
            if (h == 3) return 7;
            if (b == 3) return 8;
            return 10;
        }
    }
}

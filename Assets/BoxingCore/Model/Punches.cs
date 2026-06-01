namespace BoxingCore
{
    public static class Punches
    {
        public static bool IsHead(PunchType p) => (int)p >= 1 && (int)p <= 4;
        public static bool IsBody(PunchType p) => (int)p >= 9 && (int)p <= 12;
        public static bool IsAttack(PunchType p) => IsHead(p) || IsBody(p);

        public static AttackKind Kind(PunchType p)
        {
            int n = (int)p;
            if (n == 1 || n == 9) return AttackKind.Jab;
            if (n == 2 || n == 10) return AttackKind.Hook;
            if (n == 3 || n == 11) return AttackKind.Straight;
            return AttackKind.Upper; // 4 or 12
        }
    }
}

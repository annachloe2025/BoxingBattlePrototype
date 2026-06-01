namespace BoxingCore
{
    public sealed class BoxerStats
    {
        public string Name = "";
        public int HpHead, HpBody, Power, Speed, Stamina, Tuffness, Counter;
        public int JabAtk, JabDef, HookAtk, HookDef, StraightAtk, StraightDef, UpperAtk, UpperDef;
        public int AtkLogic = 1, DefLogic = 1;
        public int[] Skills = new int[3];

        public int Atk(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Jab: return JabAtk;
                case AttackKind.Hook: return HookAtk;
                case AttackKind.Straight: return StraightAtk;
                default: return UpperAtk;
            }
        }

        public int Def(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Jab: return JabDef;
                case AttackKind.Hook: return HookDef;
                case AttackKind.Straight: return StraightDef;
                default: return UpperDef;
            }
        }
    }
}

namespace BoxingCore
{
    public static class DistanceSystem
    {
        public static void StepIn(MatchState s, int side)
        {
            if (side == 1)
            {
                if (s.Distance == Distance.Near) { if (s.Position <= 6) s.Position++; }
                else { s.Position++; s.Distance = (Distance)((int)s.Distance - 1); }
            }
            else // P2
            {
                if (s.Distance == Distance.Near) { if (s.Position >= 2) s.Position--; }
                else { s.Distance = (Distance)((int)s.Distance - 1); }
            }
        }

        public static void StepBack(MatchState s, int side)
        {
            if (side == 1)
            {
                if (s.Position == 1) { /* 自コーナー：下がれない */ }
                else if (s.Distance == Distance.Far) { s.Position--; }
                else { s.Distance = (Distance)((int)s.Distance + 1); s.Position--; }
            }
            else // P2
            {
                if (IsCornered(s, 2)) { /* 相手コーナー側：下がれない */ }
                else if (s.Distance == Distance.Far) { s.Position++; }
                else { s.Distance = (Distance)((int)s.Distance + 1); }
            }
        }

        // side のボクサーがコーナーに詰められているか
        public static bool IsCornered(MatchState s, int side)
        {
            if (side == 1) return s.Position == 1;
            // P2
            return (s.Position == 5 && s.Distance == Distance.Far)
                || (s.Position == 6 && s.Distance == Distance.Mid)
                || (s.Position == 7);
        }
    }
}

namespace CreateEnv.Ecosystem.Genetics
{
    // Short readable names, assigned at birth from a fixed word list.
    //
    // Names make lineage memorable in a way that identity numbers never do
    // (Design Document 4.4). "Nova, Tide and their six descendants all carry their
    // grandmother's camouflage variant" is a story; "individual 47" is not.
    public static class OctopusNames
    {
        // Sea and light words, one or two syllables, easy to read at a glance and
        // easy to tell apart from each other in a family tree.
        static readonly string[] Words =
        {
            "Nova",  "Tide",  "Pebble", "Pearl", "Inky",  "Wisp",  "Ridge", "Drift",
            "Kelp",  "Shell", "Reef",   "Onyx",  "Mist",  "Dune",  "Surf",  "Ember",
            "Slate", "Fern",  "Gull",   "Cove",  "Marl",  "Spire", "Foam",  "Bay",
            "Ash",   "Opal",  "Quill",  "Rill",  "Sable", "Thorn", "Vale",  "Wren",
        };

        // Names are reused once the list runs out, with a numeral appended, so a long
        // pedigree can never run out of names or start repeating ambiguously.
        public static string At(int index)
        {
            if (index < 0) index = 0;
            int word = index % Words.Length;
            int lap = index / Words.Length;
            return lap == 0 ? Words[word] : Words[word] + " " + (lap + 1);
        }

        public static int Count => Words.Length;
    }
}

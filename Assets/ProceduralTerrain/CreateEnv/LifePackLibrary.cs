using UnityEngine;

namespace CreateEnv
{
    // The "locked" life presets.
    [CreateAssetMenu(menuName = "CreateEnv/Life Pack Library", fileName = "LifePackLibrary")]
    public class LifePackLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Pack
        {
            public string name = "None";
            [Tooltip("Scatter rules for this pack. Leave empty for the 'None' pack.")]
            public TerrainDetailScatter.ScatterRule[] rules;
        }

        public Pack[] packs = new Pack[0];

        static LifePackLibrary _cached;
        static bool _tried;

        public static LifePackLibrary Load()
        {
            if (_tried) return _cached;
            _tried = true;
            _cached = Resources.Load<LifePackLibrary>("LifePackLibrary");
            return _cached;
        }

        public string[] PackNames()
        {
            if (packs == null || packs.Length == 0) return new[] { "None" };
            var names = new string[packs.Length];
            for (int i = 0; i < packs.Length; i++)
                names[i] = string.IsNullOrEmpty(packs[i].name) ? $"Pack {i}" : packs[i].name;
            return names;
        }

        public Pack GetPack(int index)
        {
            if (packs == null || index < 0 || index >= packs.Length) return null;
            return packs[index];
        }
    }
}

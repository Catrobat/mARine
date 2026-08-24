using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // Optional override for the built-in roster. Create one via
    // Assets > Create > Living Ecosystem > Species Library, put it in a Resources
    // folder as "SpeciesLibrary", and the simulation reads it instead of the code
    // defaults — so balancing needs no rebuild, which is what the milestone document
    // asks for. Same pattern as LifePackLibrary.
    //
    // Use Tools > Living Ecosystem > Create Species Library Asset to generate one
    // pre-filled with the built-in roster, then edit the numbers.
    [CreateAssetMenu(menuName = "Living Ecosystem/Species Library", fileName = "SpeciesLibrary")]
    public class SpeciesLibraryAsset : ScriptableObject
    {
        [Tooltip("Which version of the roster layout this asset was written against. " +
                 "If it does not match SpeciesLibrary.RosterVersion the asset is " +
                 "ignored, because fields added since would silently deserialize to " +
                 "zero and quietly override the code.")]
        public int rosterVersion;

        [Tooltip("Must contain exactly SpeciesLibrary.Count entries, in the same order. " +
                 "Order is the wire format for the organism picker; never reorder.")]
        public SpeciesDefinition[] species;

        [Tooltip("Predator/prey edges. Leave empty to use the built-in web.")]
        public FoodLink[] web;

        void OnValidate()
        {
            if (species != null && species.Length != SpeciesLibrary.Count)
                Debug.LogWarning($"[SpeciesLibrary] Expected {SpeciesLibrary.Count} species, found " +
                                 $"{species.Length}. The built-in roster will be used until this matches.", this);
            SpeciesLibrary.Invalidate();
        }
    }
}

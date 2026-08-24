using System;
using System.Collections.Generic;
using MarineAR.AISpawner.Models;

namespace MarineAR.AISpawner.Repository
{
    /// <summary>
    /// In-memory repository over the parsed manifest. Populated once per scene entry
    /// (the manifest is re-fetched every time the scene opens) and never persisted.
    /// Also retains downloaded facts JSON per organism for the current session, so a
    /// placed organism's facts stay readable even after its files are evicted from
    /// the LRU disk cache.
    /// </summary>
    public sealed class MarineRepository
    {
        readonly List<MarineOrganism> m_Organisms = new List<MarineOrganism>();
        readonly Dictionary<string, MarineOrganism> m_ById = new Dictionary<string, MarineOrganism>(StringComparer.Ordinal);
        readonly Dictionary<string, FactsDocument> m_FactsById = new Dictionary<string, FactsDocument>(StringComparer.Ordinal);

        public bool IsPopulated => m_Organisms.Count > 0;

        public int Count => m_Organisms.Count;

        public IReadOnlyList<MarineOrganism> All => m_Organisms;

        /// <summary>Replaces the repository contents from a freshly downloaded manifest.</summary>
        public void Populate(Manifest manifest)
        {
            m_Organisms.Clear();
            m_ById.Clear();

            if (manifest?.items == null)
                return;

            foreach (MarineOrganism organism in manifest.items)
            {
                if (organism == null || string.IsNullOrEmpty(organism.id) || m_ById.ContainsKey(organism.id))
                    continue;

                m_Organisms.Add(organism);
                m_ById.Add(organism.id, organism);
            }
        }

        public MarineOrganism GetById(string id)
        {
            return !string.IsNullOrEmpty(id) && m_ById.TryGetValue(id, out MarineOrganism organism)
                ? organism
                : null;
        }

        /// <summary>
        /// Case-insensitive substring search over common and scientific names.
        /// An empty query returns every organism.
        /// </summary>
        public List<MarineOrganism> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<MarineOrganism>(m_Organisms);

            string needle = query.Trim();
            var results = new List<MarineOrganism>();
            foreach (MarineOrganism organism in m_Organisms)
            {
                if (Matches(organism.common_name, needle) ||
                    Matches(organism.scientific_name, needle) ||
                    Matches(organism.id, needle))
                {
                    results.Add(organism);
                }
            }

            return results;
        }

        /// <summary>Stores parsed facts for the session (survives disk-cache eviction).</summary>
        public void StoreFacts(string organismId, FactsDocument facts)
        {
            if (!string.IsNullOrEmpty(organismId) && facts != null)
                m_FactsById[organismId] = facts;
        }

        public FactsDocument GetFacts(string organismId)
        {
            return !string.IsNullOrEmpty(organismId) && m_FactsById.TryGetValue(organismId, out FactsDocument facts)
                ? facts
                : null;
        }

        static bool Matches(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) &&
                   haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

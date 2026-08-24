using UnityEngine;

namespace CreateEnv.Ecosystem.Genetics
{
    // The visible half of one octopus.
    //
    // Body size and camouflage are read straight off the genome, so genetic
    // differences are visible in AR rather than only in a panel: a large octopus is
    // physically bigger, and a well-camouflaged one fades toward the colour of the
    // sand it is sitting on while a poorly camouflaged one stays conspicuous. That
    // second one is also the animal the shark is likelier to take, so what the
    // learner can see and what the simulation acts on are the same thing.
    //
    // Per-individual colour goes through a MaterialPropertyBlock rather than a
    // material of its own. Giving every octopus its own Material broke batching for
    // the whole reef; a property block changes only what this renderer draws with.
    public class OctopusAgentView : MonoBehaviour
    {
        public int agentId = -1;

        Renderer _renderer;
        MaterialPropertyBlock _block;
        Color _baseTint;

        float _shownCamouflage = -1f;
        float _shownSize = -1f;
        float _shownGlow = -1f;
        bool _dirty;

        // The colour a well-camouflaged octopus disappears into: pale Cabo Verde
        // carbonate sand.
        static readonly Color Seabed = new Color(0.74f, 0.70f, 0.60f);

        public void Bind(Renderer targetRenderer, Color baseTint)
        {
            _renderer = targetRenderer;
            _baseTint = baseTint;
            _block = new MaterialPropertyBlock();
        }

        public void Apply(OctopusAgent agent, float baseScale)
        {
            if (agent == null || _renderer == null) return;
            agentId = agent.id;

            // Size: the additive gene, straight onto the model.
            if (!Mathf.Approximately(_shownSize, agent.traits.bodySize))
            {
                _shownSize = agent.traits.bodySize;
                transform.localScale = Vector3.one * (baseScale * agent.traits.bodySize);
            }

            // Camouflage: how far it has faded into the seabed. A brooding female is
            // tucked into her den and reads as hidden whatever her genes say.
            float shown = agent.traits.camouflage;
            if (agent.IsBrooding) shown = Mathf.Min(1f, shown + 0.25f);

            if (!Mathf.Approximately(_shownCamouflage, shown))
            {
                _shownCamouflage = shown;
                _dirty = true;
            }

            if (_dirty) Flush();
        }

        // A soft light on an octopus the learner has singled out, or one that is
        // brooding. Deliberately gentle: enough to follow her across the reef, not
        // enough to look like a video game marker.
        public void SetGlow(float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (Mathf.Abs(amount - _shownGlow) < 0.02f) return;
            _shownGlow = amount;
            _dirty = true;
        }

        // One write to the renderer covering colour and glow together, rather than
        // one per property per frame.
        void Flush()
        {
            _dirty = false;
            if (_renderer == null || _block == null) return;

            // Never all the way to the sand: the learner must still be able to find
            // and tap it.
            float camo = Mathf.Max(0f, _shownCamouflage);
            var tint = Color.Lerp(_baseTint, Seabed, camo * 0.78f);

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(SpeciesVisualLibrary.BaseColorId, tint);
            _block.SetColor(SpeciesVisualLibrary.ColorId, tint);

            float glow = Mathf.Max(0f, _shownGlow);
            _block.SetColor(SpeciesVisualLibrary.EmissionId,
                            new Color(0.42f, 0.86f, 0.72f) * (glow * 0.55f));

            _renderer.SetPropertyBlock(_block);
        }
    }
}

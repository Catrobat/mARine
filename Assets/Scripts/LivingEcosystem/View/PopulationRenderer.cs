using System.Collections.Generic;
using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // Simulate numbers, draw a sample (Design Document 3.1).
    //
    // The renderer draws as many models as the visual budget allows and scales that
    // to the pool. If forty parrotfish exist and the cap is eight, eight are drawn
    // and the true count is shown on screen. If the pool crashes to three, three are
    // drawn.
    //
    // Everything is pooled: instances are created once, then shown, hidden and moved.
    // No allocation and no Instantiate after warm-up, which is what keeps this off
    // the frame budget on a 4 GB Android device.
    public class PopulationRenderer : MonoBehaviour
    {
        [Tooltip("Hard ceiling on models drawn across all species at once. The design " +
                 "document's figure was 30, set before the models existed; the whole " +
                 "roster is only 2,524 triangles, so 48 costs about 12k triangles and " +
                 "buys a reef that actually looks inhabited.")]
        public int totalModelBudget = 48;

        [Tooltip("Radius around the learner within which models are placed.")]
        public float placementRadius = 8f;

        [Tooltip("Display scale on every organism. Life-size is biologically honest " +
                 "but reads as specks against terrain whose dunes are tens of metres " +
                 "across, so the drawn size is lifted a little. This is a disclosed " +
                 "simplification, like the compressed time and brood sizes.")]
        public float sizeMultiplier = 1.8f;

        [Tooltip("How often the drawn sample is rebuilt, in seconds. The simulation " +
                 "ticks far faster than the eye needs the crowd redrawn.")]
        public float refreshInterval = 1.5f;

        [Tooltip("Centre the reef on the learner, which is what AR wants. Turn this " +
                 "off to pin it to this object instead, for a fixed-camera preview " +
                 "where a reef centred on the camera would be half behind it.")]
        public bool followViewer = true;

        [Tooltip("Draw nothing until there is a seafloor to stand on. In AR the " +
                 "environment is not placed until the learner taps a surface, and " +
                 "organisms hanging in mid-air over the camera feed before that just " +
                 "look broken.")]
        public bool requireGround = true;

        // Set when the octopuses have been promoted to individuals with genomes, so
        // every one of them is drawn and wears its own genotype.
        [System.NonSerialized] public Genetics.OctopusPopulation octopuses;

        // Lets the renderer see which octopuses the learner has singled out.
        [System.NonSerialized] public LivingReefController reef;

        static readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        readonly float[] _weightScratch = new float[SpeciesLibrary.Count];

        // Finds the seabed under a point, ignoring the AR session's own planes.
        //
        // Those planes carry colliders, and they are the real-world floor the
        // environment was placed on — not the seafloor the organisms live on. Taking
        // the first hit put the whole reef on the detected plane instead of the
        // terrain, and made the reef appear before the environment had been placed at
        // all, because a scanned plane counted as ground.
        static bool TryFindSeabed(Vector3 above, out float y, out Vector3 normal)
        {
            y = 0f;
            normal = Vector3.up;

            int count = Physics.RaycastNonAlloc(above, Vector3.down, _hitBuffer, 240f);
            float nearest = float.MaxValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.collider == null) continue;
                if (hit.collider.GetComponentInParent<UnityEngine.XR.ARFoundation.ARPlane>() != null)
                    continue;

                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    y = hit.point.y;
                    normal = hit.normal;
                    found = true;
                }
            }
            return found;
        }

        class Instance
        {
            public GameObject go;
            public Transform tf;
            public Vector3 anchor;
            public float phase;
            public float swimSpeed;
            public MotionKind motion;
            public float roamRadius;
            public float verticalRoam;
            public Quaternion groundTilt;   // the slope this one is lying on
            public bool seated;       // has it been given a home yet
            public Quaternion seatedRotation;
            public float groundY;   // seabed height directly under this model
            public float ceilingY;  // the top of its usable water column
            public float bottomOffset; // origin down to the model's lowest point
            public float topOffset;    // origin up to its highest point
            public float baseScale;    // the size this model was normalised to
            public Genetics.OctopusAgentView view;   // octopuses only
            public int agentId = -1;                 // octopuses only
            public Genetics.OctopusAgent agent;      // resolved once per rebuild
            public bool denned;                      // she has settled to brood
        }

        readonly List<Instance>[] _instances = new List<Instance>[SpeciesLibrary.Count];
        readonly int[] _visible = new int[SpeciesLibrary.Count];

        EcosystemSimulation _sim;
        Transform _origin;
        float _refreshTimer;
        float _patchTimer;
        int _placementSeed = 977;

        // Where this patch of reef sits. Fixed once found, so the organisms stay put
        // instead of chasing the camera around. It only moves if the learner walks a
        // long way, or when the seafloor first appears underneath them — in AR the
        // environment is placed by a tap some time after this component starts, and
        // until then there is no ground to sit on.
        Vector3 _patchCentre;
        bool _patchPlaced;
        bool _hadGround;
        WaterSurface _water;
        bool _loggedColumn;

        public void Bind(EcosystemSimulation sim, Transform origin)
        {
            _sim = sim;
            _origin = origin != null ? origin : transform;
            for (int i = 0; i < _instances.Length; i++)
                _instances[i] ??= new List<Instance>(8);
            _refreshTimer = 0f;
            Rebuild();
        }

        void Update()
        {
            if (_sim == null) return;

            _patchTimer -= Time.deltaTime;
            if (_patchTimer <= 0f)
            {
                _patchTimer = 0.25f;
                UpdatePatchCentre();
            }

            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = refreshInterval;
                Rebuild();
            }

            Animate();
        }

        // Decides where the patch of reef lives, and only moves it when it has to.
        void UpdatePatchCentre()
        {
            Vector3 viewer = _origin != null ? _origin.position : transform.position;
            if (followViewer)
            {
                var cam = Camera.main;
                if (cam != null) viewer = cam.transform.position;
            }

            bool hasGround = TryFindSeabed(viewer + Vector3.up * 30f, out float seabed, out _);
            float ground = hasGround ? seabed : viewer.y - 2.5f;

            // First frame, or the learner has walked out of this patch, or the
            // seafloor has just been placed under them.
            // Ground appearing for the first time counts (in AR the seafloor only
            // exists after the learner taps to place it). Ground going away does not,
            // or a chunk dropping its collider at distance would shuffle the reef.
            bool groundJustArrived = hasGround && !_hadGround;

            bool needsMove = !_patchPlaced
                          || groundJustArrived
                          || Vector3.Distance(new Vector3(viewer.x, 0f, viewer.z),
                                              new Vector3(_patchCentre.x, 0f, _patchCentre.z)) > placementRadius * 1.4f;

            if (!needsMove) return;

            _patchCentre = new Vector3(viewer.x, ground, viewer.z);
            _patchPlaced = true;
            _hadGround = hasGround;

            if (!_loggedColumn && hasGround)
            {
                _loggedColumn = true;
                Debug.Log($"[LivingReef] Seabed found at y={ground:0.00} (AR planes excluded). " +
                          $"Water surface reports y={SurfaceHeight():0.00}.");
            }

            // Everything currently out there needs a new home around the new centre.
            for (int s = 0; s < _instances.Length; s++)
            {
                var list = _instances[s];
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++) list[i].seated = false;
            }
        }

        // Decides how many of each species to draw, then shows exactly that many.
        void Rebuild()
        {
            if (_sim == null) return;

            // Nothing to stand on yet — the environment has not been placed.
            if (requireGround && !_hadGround)
            {
                for (int i = 0; i < SpeciesLibrary.Count; i++) SetVisible(i, 0);
                return;
            }

            var all = SpeciesLibrary.All;

            // Share the budget by each species' share of the drawn population, so a
            // species that has crashed visibly thins out instead of holding its slots.
            float totalWeight = 0f;
            var weight = _weightScratch;
            for (int i = 0; i < all.Length; i++) weight[i] = 0f;
            for (int i = 0; i < all.Length; i++)
            {
                if (!_sim.IsAlive(i)) continue;
                float amount = _sim.DisplayAmount(i);
                float per = Mathf.Max(0.01f, all[i].individualsPerModel);
                weight[i] = Mathf.Sqrt(Mathf.Max(0f, amount / per));
                totalWeight += weight[i];
            }

            for (int i = 0; i < all.Length; i++)
            {
                int want = 0;

                // Octopuses are individuals, not a sample. Every living one is drawn,
                // because the learner taps them, inspects them and breeds them — a
                // representative crowd would be meaningless.
                if (i == SpeciesLibrary.Octopus && octopuses != null)
                {
                    want = Mathf.Min(octopuses.AliveCount, totalModelBudget);
                }
                else if (_sim.IsAlive(i) && totalWeight > 0f)
                {
                    float amount = _sim.DisplayAmount(i);
                    float per = Mathf.Max(0.01f, all[i].individualsPerModel);

                    // Never draw more than actually exist, never fewer than one while
                    // any remain: a single surviving animal must still be visible.
                    int trueish = Mathf.CeilToInt(amount / per);
                    int share = Mathf.RoundToInt(totalModelBudget * (weight[i] / totalWeight));
                    want = Mathf.Clamp(Mathf.Min(share, trueish), 1, totalModelBudget);
                }
                SetVisible(i, want);
            }

            ApplyOctopusGenomes();
        }

        // Gives each drawn octopus the size and colour its own genes call for.
        void ApplyOctopusGenomes()
        {
            if (octopuses == null) return;

            var list = _instances[SpeciesLibrary.Octopus];
            if (list == null) return;

            var def = SpeciesLibrary.Get(SpeciesLibrary.Octopus);
            float baseScale = def != null ? def.modelHeightMeters * sizeMultiplier : 1f;

            int shown = 0;
            for (int i = 0; i < octopuses.agents.Count && shown < list.Count; i++)
            {
                var agent = octopuses.agents[i];
                if (!agent.IsAlive) continue;

                var view = list[shown].view;
                if (view != null) view.Apply(agent, list[shown].baseScale);
                list[shown].agentId = agent.id;
                list[shown].agent = agent;
                if (!agent.IsBrooding) list[shown].denned = false;

                if (view != null)
                {
                    bool chosen = reef != null &&
                                  (agent.id == reef.chosenFemaleId || agent.id == reef.chosenMaleId);
                    // A chosen animal is lit steadily; a brooding one keeps a fainter
                    // light so she can still be found once the tool is closed.
                    view.SetGlow(chosen ? 1f : agent.IsBrooding ? 0.35f : 0f);
                }

                shown++;
            }
        }

        void SetVisible(int species, int wanted)
        {
            var list = _instances[species];
            if (list == null) return;

            while (list.Count < wanted)
            {
                var inst = Create(species, list.Count);
                if (inst == null) break;
                list.Add(inst);
            }

            for (int i = 0; i < list.Count; i++)
            {
                bool on = i < wanted;
                if (list[i].go.activeSelf != on) list[i].go.SetActive(on);

                // Only place a model when it first appears, or after the patch has
                // moved. Re-seating every refresh is what made them teleport.
                if (on && !list[i].seated)
                {
                    Reseat(species, list[i], i);
                    list[i].seated = true;
                }
            }

            _visible[species] = Mathf.Min(wanted, list.Count);
        }

        Instance Create(int species, int index)
        {
            var def = SpeciesLibrary.Get(species);
            if (def == null) return null;

            GameObject go;
            var prefab = SpeciesVisualLibrary.PrefabFor(species);
            if (prefab != null)
            {
                go = Instantiate(prefab, transform);
            }
            else
            {
                var mesh = SpeciesVisualLibrary.MeshFor(species);
                if (mesh == null) return null;

                go = new GameObject($"{def.id}_{index}");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = SpeciesVisualLibrary.MaterialFor(species);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }

            go.name = $"{def.id}_{index}";
            // Scaled against the environment the reef is sitting in, so the organisms
            // keep the right size relative to the world however it was placed.
            float worldScale = _origin != null ? Mathf.Max(0.01f, _origin.lossyScale.x) : 1f;
            float targetSize = def.modelHeightMeters * sizeMultiplier * worldScale;
            NormaliseSize(go, targetSize);
            float normalisedScale = go.transform.localScale.x;

            Genetics.OctopusAgentView view = null;
            if (species == SpeciesLibrary.Octopus)
            {
                view = go.AddComponent<Genetics.OctopusAgentView>();
                view.Bind(go.GetComponentInChildren<Renderer>(), def.tint);

                // Something to tap. Sized to the model so it is comfortable to hit on
                // a phone without swallowing taps meant for the interface.
                var collider = go.AddComponent<SphereCollider>();
                collider.radius = 0.62f;
                collider.isTrigger = true;
            }

            // Measure the scaled model so it can be seated on the seabed by its
            // underside rather than by its centre.
            go.transform.localPosition = Vector3.zero;
            float bottomOffset = 0f, topOffset = 0f;
            if (TryCombinedBounds(go, out var bounds))
            {
                bottomOffset = go.transform.position.y - bounds.min.y;
                topOffset = bounds.max.y - go.transform.position.y;
            }

            var rng = new System.Random(_placementSeed + species * 131 + index);
            return new Instance
            {
                go = go,
                tf = go.transform,
                motion = def.motion,
                roamRadius = Mathf.Max(0.05f, def.roamRadius),
                verticalRoam = def.verticalRoam,

                bottomOffset = bottomOffset,
                topOffset = topOffset,
                phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                swimSpeed = 0.25f + (float)rng.NextDouble() * 0.35f,
                baseScale = normalisedScale,
                view = view,
            };
        }

        // Scales whatever the model happens to be so it stands the right number of
        // metres tall, regardless of its native import size.
        static void NormaliseSize(GameObject go, float targetHeight)
        {
            if (targetHeight <= 0f) return;
            if (!TryCombinedBounds(go, out var bounds)) return;

            float h = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (h <= 1e-4f) return;

            go.transform.localScale *= targetHeight / h;
        }

        static bool TryCombinedBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        // World height of the water surface, so swimmers can be layered against the
        // real column. Falls back to a sensible ceiling when there is no water plane.
        float _waterSearch;

        float SurfaceHeight()
        {
            // Looked for occasionally rather than on every call: FindFirstObjectByType
            // is expensive, and in a scene with no water plane the old code paid for
            // it every single time a model was seated.
            if (_water == null && Time.time >= _waterSearch)
            {
                _waterSearch = Time.time + 2f;
                _water = FindFirstObjectByType<WaterSurface>();
            }
            if (_water != null) return _water.transform.position.y;
            return _patchCentre.y + 8f;
        }

        // Places one model somewhere plausible on the patch: sessile life sits on the
        // seafloor, swimmers hold station above it. Deterministic per species and
        // slot, so the same reef lays out the same way twice.
        void Reseat(int species, Instance inst, int index)
        {
            var def = SpeciesLibrary.Get(species);
            if (def == null) return;

            var rng = new System.Random(_placementSeed + species * 733 + index * 17);

            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            // sqrt keeps the scatter even across the disc instead of clumping at the
            // centre, and the inner hole keeps models out of the learner's face.
            float radius = Mathf.Lerp(1.8f, placementRadius, Mathf.Sqrt((float)rng.NextDouble()));

            Vector3 flat = _patchCentre + new Vector3(Mathf.Cos(angle) * radius, 0f,
                                                      Mathf.Sin(angle) * radius);

            // Sit on the seafloor directly beneath this spot, so the reef follows the
            // real terrain instead of floating on one flat plane, and pick up the
            // slope there so attached life can lie along it.
            float groundY = _patchCentre.y;
            Vector3 groundNormal = Vector3.up;
            if (TryFindSeabed(flat + Vector3.up * 30f, out float hitY, out Vector3 hitNormal))
            {
                groundY = hitY;
                groundNormal = hitNormal;
            }

            // Height above the seabed, in plain metres.
            //
            // This deliberately does not derive itself from the water surface.
            // WaterSurface reports an absolute world height while the terrain sits
            // wherever the AR tap landed, and when those two disagreed the computed
            // column collapsed and every swimmer ended up lying on the sand. The
            // seabed is the one reference that is always right underfoot.
            float surfaceY = SurfaceHeight();

            float y;
            Quaternion yaw = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            if (def.attachedToSeabed)
            {
                // Underside on the seabed, not centre — otherwise half the model is
                // buried in the sand — and lying along whatever slope it is fixed to.
                y = groundY + inst.bottomOffset;
                inst.groundTilt = Quaternion.FromToRotation(Vector3.up, groundNormal);
                inst.tf.rotation = inst.groundTilt * yaw;
            }
            else
            {
                y = groundY + Mathf.Lerp(def.seabedClearanceMin, def.seabedClearanceMax,
                                         (float)rng.NextDouble());

                // Never clipping through the seabed. The surface is used only as a
                // ceiling, and only when it reports something credible.
                float lowest = groundY + inst.bottomOffset + 0.05f;
                y = Mathf.Max(y, lowest);
                if (surfaceY > groundY + 2f)
                    y = Mathf.Max(lowest, Mathf.Min(y, surfaceY - inst.topOffset - 0.3f));

                inst.groundTilt = Quaternion.identity;
                inst.tf.rotation = yaw;
            }

            inst.anchor = new Vector3(flat.x, y, flat.z);
            inst.tf.position = inst.anchor;
            inst.seatedRotation = inst.tf.rotation;
            inst.groundY = groundY;
            inst.ceilingY = surfaceY > groundY + 2f ? surfaceY : groundY + 60f;
        }

        // Sessile life sways; swimmers drift around their anchor. Cheap sine motion,
        // no physics, no pathfinding — enough to stop the reef looking like a
        // photograph without costing anything measurable.
        void Animate()
        {
            float t = Time.time;
            for (int s = 0; s < _instances.Length; s++)
            {
                var list = _instances[s];
                if (list == null) continue;

                for (int i = 0; i < _visible[s] && i < list.Count; i++)
                {
                    var inst = list[i];
                    if (!inst.go.activeSelf) continue;

                    if (s == SpeciesLibrary.Octopus && octopuses != null &&
                        AnimateOctopus(inst, t)) continue;

                    switch (inst.motion)
                    {
                        case MotionKind.Fixed:
                            // A coral colony on a limestone skeleton. It stays put.
                            break;

                        case MotionKind.Sway:
                        {
                            // Rooted, but flexible enough to move with the water.
                            float sway = Mathf.Sin(t * 0.7f + inst.phase) * 3.5f;
                            inst.tf.rotation = inst.seatedRotation *
                                               Quaternion.Euler(sway, 0f, sway * 0.5f);
                            break;
                        }

                        case MotionKind.Crawl:
                        {
                            // Slow travel over the seabed, staying on the bottom.
                            float a = t * inst.swimSpeed * 0.38f + inst.phase;
                            var offset = new Vector3(Mathf.Sin(a) * inst.roamRadius, 0f,
                                                     Mathf.Cos(a * 0.83f) * inst.roamRadius);
                            Vector3 next = inst.anchor + offset;
                            next.y = inst.groundY + inst.bottomOffset;

                            Vector3 travel = next - inst.tf.position;
                            inst.tf.position = next;
                            // Face the way it is going, but keep lying on the slope.
                            if (travel.sqrMagnitude > 1e-8f)
                            {
                                var flat = new Vector3(travel.x, 0f, travel.z);
                                if (flat.sqrMagnitude > 1e-8f)
                                    inst.tf.rotation = Quaternion.Slerp(inst.tf.rotation,
                                        inst.groundTilt * Quaternion.LookRotation(flat.normalized, Vector3.up),
                                        0.05f);
                            }
                            break;
                        }

                        default:
                        {
                            float a = t * inst.swimSpeed + inst.phase;
                            var offset = new Vector3(Mathf.Sin(a) * 1.2f,
                                                     Mathf.Sin(a * 0.6f) * inst.verticalRoam,
                                                     Mathf.Cos(a * 0.8f) * 1.2f);
                            Vector3 next = inst.anchor + offset;

                            // Never through the seabed, never out through the surface.
                            next.y = Mathf.Clamp(next.y,
                                                 inst.groundY + inst.bottomOffset,
                                                 inst.ceilingY - inst.topOffset - 0.15f);

                            Vector3 travel = next - inst.tf.position;
                            inst.tf.position = next;
                            if (travel.sqrMagnitude > 1e-6f)
                                inst.tf.rotation = Quaternion.Slerp(inst.tf.rotation,
                                    Quaternion.LookRotation(travel.normalized, Vector3.up), 0.15f);
                            break;
                        }
                    }
                }
            }
        }

        // Courtship and brooding, shown rather than described.
        //
        // A male that has mated goes to the female he mated with and stays with her
        // while he fades; she settles into a den on the seabed and stops moving
        // altogether for the month she is guarding her eggs. Both come straight off
        // the life cycle the simulation is already running, so what the learner
        // watches and what is actually happening are the same thing.
        //
        // Returns true when it has taken charge of this model's movement.
        bool AnimateOctopus(Instance inst, float t)
        {
            if (inst.agentId < 0) return false;

            // Resolved during the last rebuild rather than searched for every frame.
            var agent = inst.agent;
            if (agent == null || !agent.IsAlive || agent.id != inst.agentId) return false;

            if (agent.IsBrooding)
            {
                // She picks a den the moment she starts brooding and does not leave it.
                // She picks a den the moment she starts brooding and does not leave it.
                if (!inst.denned)
                {
                    inst.denned = true;
                    inst.anchor = new Vector3(inst.anchor.x,
                                              inst.groundY + inst.bottomOffset,
                                              inst.anchor.z);
                }

                inst.tf.position = Vector3.Lerp(inst.tf.position, inst.anchor, 0.06f);

                // Just the slow breathing of an animal fanning its eggs.
                float breathe = 1f + Mathf.Sin(t * 0.8f + inst.phase) * 0.02f;
                inst.tf.localScale = Vector3.one * (inst.baseScale * breathe);
                return true;
            }

            // A mated male keeps close to his partner for the little time he has left.
            if (agent.HasMated)
            {
                var partner = FindBroodingPartner(agent.id);
                if (partner.HasValue)
                {
                    // He heads for her while he is still crossing the reef and only
                    // starts circling once he arrives, so the approach reads as an
                    // approach rather than an orbit that begins from far away.
                    float distance = Vector3.Distance(inst.tf.position, partner.Value);
                    float arrived = Mathf.Clamp01(Mathf.InverseLerp(4f, 1.2f, distance));

                    Vector3 orbit = new Vector3(Mathf.Cos(t * 0.4f + inst.phase), 0.35f,
                                                Mathf.Sin(t * 0.4f + inst.phase)) * 0.9f;
                    Vector3 beside = partner.Value + orbit * arrived;

                    Vector3 travel = beside - inst.tf.position;
                    inst.tf.position = Vector3.Lerp(inst.tf.position, beside,
                                                    Mathf.Lerp(0.012f, 0.05f, arrived));
                    if (travel.sqrMagnitude > 1e-5f)
                        inst.tf.rotation = Quaternion.Slerp(inst.tf.rotation,
                            Quaternion.LookRotation(travel.normalized, Vector3.up), 0.08f);
                    return true;
                }
            }

            return false;
        }

        // Where the female carrying this male's brood currently is.
        Vector3? FindBroodingPartner(int maleId)
        {
            var list = _instances[SpeciesLibrary.Octopus];
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].go.activeSelf || list[i].agentId < 0) continue;
                var other = octopuses.ById(list[i].agentId);
                if (other == null || !other.IsBrooding) continue;
                if (other.storedMateId != maleId) continue;
                return list[i].tf.position;
            }
            return null;
        }

        public int VisibleCount(int species) =>
            species >= 0 && species < _visible.Length ? _visible[species] : 0;
    }
}

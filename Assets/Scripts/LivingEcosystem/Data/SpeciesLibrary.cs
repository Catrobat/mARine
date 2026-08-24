using System.Collections.Generic;
using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // The Cabo Verde roster and the food web that connects it.
    //
    // Defined in code so the feature works with no asset authoring, and overridable
    // by dropping a SpeciesLibraryAsset named "SpeciesLibrary" into a Resources
    // folder — the same pattern LifePackLibrary already uses. Retuning the biology
    // therefore never needs a rebuild, which is what the milestone document asks for.
    //
    // Every rate here was solved rather than guessed: the balanced roster is a fixed
    // point of the simulation, holding its starting populations to within a fraction
    // of a percent over 400 simulated days. Changing one number moves that fixed
    // point, so re-run the balance report in
    // Tools > Living Ecosystem > Balance Report after any edit.
    //
    // Content (appearance, habitat, conservation status, the "one thing worth
    // telling") is taken from the Living Ecosystem Literature Document v1, Part 4.
    // Nothing here should be invented; add it to that document first.
    public static class SpeciesLibrary
    {
        // Indices are the wire format for EcosystemSettings.present[] and for the
        // save file. Append new species at the end; never reorder.
        public const int Halimeda    = 0;
        public const int Padina      = 1;
        public const int Coral       = 2;
        public const int Parrotfish  = 3;
        public const int Urchin      = 4;
        public const int Limpet      = 5;
        public const int Lobster     = 6;
        public const int Octopus     = 7;
        public const int TigerShark  = 8;

        public const int Count = 9;

        // Bump this whenever a field is added to SpeciesDefinition or the roster
        // layout changes.
        //
        // A SpeciesLibrary asset in Resources overrides this code entirely. That is
        // the point of it — but an asset written before a field existed deserializes
        // that field as zero, and then silently overrides the code with it. That is
        // exactly what happened: an asset generated from the first roster kept
        // reporting motion = Fixed and zero seabed clearance for every species, so
        // every organism sat motionless on the seabed no matter what this file said,
        // and no amount of editing here changed anything. The version stamp makes a
        // stale asset announce itself and step aside instead of failing silently.
        public const int RosterVersion = 3;

        // How far above 1.0 the supply/need ratio must sit for full breeding, and how
        // far below for full starvation mortality.
        public const float SurplusForFullBreeding = 0.35f;
        public const float DeficitForFullDeath    = 0.50f;

        // Share of what dies that reaches the seafloor rather than being eaten, and
        // the daily share of the detritus pool that bacteria return to the water.
        public const float DetritusFromDeath   = 0.80f;
        public const float DetritusFromGrazing = 0.20f;
        public const float DetritusRemineralised = 0.015f;

        static SpeciesDefinition[] _cached;
        static FoodLink[] _cachedWeb;
        static bool _triedAsset;

        public static SpeciesDefinition[] All
        {
            get { EnsureLoaded(); return _cached; }
        }

        public static FoodLink[] Web
        {
            get { EnsureLoaded(); return _cachedWeb; }
        }

        public static SpeciesDefinition Get(int index)
        {
            EnsureLoaded();
            return index >= 0 && index < _cached.Length ? _cached[index] : null;
        }

        public static string NameOf(int index)
        {
            var s = Get(index);
            return s != null ? s.commonName : "unknown";
        }

        public static bool[] AllPresent()
        {
            var flags = new bool[Count];
            for (int i = 0; i < Count; i++) flags[i] = true;
            return flags;
        }

        // Reloads on next access. Called by the editor tools after an asset edit.
        public static void Invalidate()
        {
            _cached = null;
            _cachedWeb = null;
            _triedAsset = false;
        }

        static void EnsureLoaded()
        {
            if (_cached != null) return;

            if (!_triedAsset)
            {
                _triedAsset = true;
                var asset = Resources.Load<SpeciesLibraryAsset>("SpeciesLibrary");
                if (asset != null)
                {
                    if (asset.rosterVersion != RosterVersion)
                    {
                        Debug.LogWarning(
                            $"[SpeciesLibrary] Ignoring Resources/SpeciesLibrary: it was written " +
                            $"against roster version {asset.rosterVersion}, but this build expects " +
                            $"{RosterVersion}. Fields added since would load as zero and silently " +
                            $"override the code — which is how every organism ended up motionless " +
                            $"on the seabed. Delete the asset, or regenerate it from " +
                            $"Tools > Living Ecosystem > Create Species Library Asset.");
                    }
                    else if (asset.species == null || asset.species.Length != Count)
                    {
                        Debug.LogWarning($"[SpeciesLibrary] Ignoring Resources/SpeciesLibrary: expected " +
                                         $"{Count} species, found {asset.species?.Length ?? 0}.");
                    }
                    else
                    {
                        _cached = asset.species;
                        _cachedWeb = asset.web != null && asset.web.Length > 0 ? asset.web : BuildWeb();
                        Debug.Log("[SpeciesLibrary] Using the roster from Resources/SpeciesLibrary.");
                        return;
                    }
                }
            }

            _cached = BuildDefault();
            _cachedWeb = BuildWeb();
        }

        // ── The food web (Design Document 2.3) ───────────────────────────────
        static FoodLink[] BuildWeb()
        {
            return new[]
            {
                // Grazers on the two algae. The coral is not grazed in this model.
                new FoodLink(Parrotfish, Halimeda, 0.53f, 18f),
                new FoodLink(Parrotfish, Padina,   0.42f, 18f),
                // Sparisoma also takes juvenile urchins, which is part of why losing
                // parrotfish lets a barren form (Clemente et al., Canary Islands).
                new FoodLink(Parrotfish, Urchin,   0.05f, 10f),

                new FoodLink(Urchin, Halimeda, 0.50f, 6f),
                new FoodLink(Urchin, Padina,   0.50f, 6f),

                new FoodLink(Limpet, Halimeda, 0.40f, 8f),
                new FoodLink(Limpet, Padina,   0.60f, 8f),

                // The lobster is an omnivore; the rest of its demand is detritus.
                new FoodLink(Lobster, Halimeda, 0.35f, 12f),
                new FoodLink(Lobster, Padina,   0.25f, 12f),

                new FoodLink(Octopus, Lobster, 0.80f, 4f),
                new FoodLink(Octopus, Limpet,  0.20f, 1f),

                new FoodLink(TigerShark, Parrotfish, 0.30f, 12f),
                new FoodLink(TigerShark, Urchin,     0.55f, 8f),
                new FoodLink(TigerShark, Octopus,    0.15f, 10f),
            };
        }

        // ── The roster ───────────────────────────────────────────────────────
        static SpeciesDefinition[] BuildDefault()
        {
            var list = new List<SpeciesDefinition>(Count);

            list.Add(new SpeciesDefinition
            {
                id = "halimeda",
                commonName = "Calcareous green alga",
                scientificName = "Halimeda sp.",
                level = TrophicLevel.Producer,
                growthRate = 0.16f, baseCapacity = 140f, naturalLoss = 0.020f, recruitment = 0.004f,
                optimumTemperature = 26f, temperatureTolerance = 7f,
                calcifierWeight = 0.55f, startingStock = 92.44f,
                tint = new Color(0.44f, 0.76f, 0.42f),
                // A low bushy clump, 10-25 cm across.
                modelHeightMeters = 0.22f, individualsPerModel = 12f,
                attachedToSeabed = true, motion = MotionKind.Sway,
                appearance = "Chains of small flat green segments, each stiffened with limestone, growing in low bushy clumps.",
                habitat = "Shallow sand and rubble in warm seas worldwide.",
                diet = "Photosynthesis — it makes its own food from sunlight.",
                roleInModel = "The quickest producer to rebound. Keeps the ecosystem alive through moderate grazing.",
                iucnStatus = "Not individually assessed",
                regionalStatus = "Common on Cabo Verde shallow sand",
                oneThingWorthTelling =
                    "This alga builds limestone into its own tissue. When it dies the soft parts rot away and " +
                    "the limestone segments fall to the seafloor as pale sand. A great deal of tropical white " +
                    "sand is the remains of algae like this one. You are standing on it.",
                simplificationNote = "Given at genus level. The specific Cabo Verde species is still to be confirmed against a regional checklist.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "padina",
                commonName = "Fan alga",
                scientificName = "Padina sp.",
                level = TrophicLevel.Producer,
                growthRate = 0.16f, baseCapacity = 100f, naturalLoss = 0.018f, recruitment = 0.006f,
                optimumTemperature = 25f, temperatureTolerance = 7.5f,
                calcifierWeight = 0.25f, startingStock = 73.81f,
                tint = new Color(0.72f, 0.62f, 0.34f),
                // Fan blades are small — a hand's width.
                modelHeightMeters = 0.16f, individualsPerModel = 12f,
                attachedToSeabed = true, motion = MotionKind.Sway,
                appearance = "Thin fan- or funnel-shaped blades, pale brown with fine concentric banding, often with a chalky white surface.",
                habitat = "Shallow rock and rubble in warm and warm-temperate seas.",
                diet = "Photosynthesis — it makes its own food from sunlight.",
                roleInModel = "The opportunist. First to spread across space that grazing or coral loss opens up.",
                iucnStatus = "Not individually assessed",
                regionalStatus = "Common on Cabo Verde shallow rock",
                oneThingWorthTelling =
                    "When grazing animals are removed, this is the alga that takes over. It grows over coral and " +
                    "blocks its light. A reef shifting from coral-dominated to algae-dominated is one of the " +
                    "most-studied problems in reef ecology, and it usually begins with the loss of grazers rather " +
                    "than with anything happening to the coral itself.",
                simplificationNote = "Given at genus level. The specific Cabo Verde species is still to be confirmed against a regional checklist.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "siderastrea-radians",
                commonName = "Lesser starlet coral",
                scientificName = "Siderastrea radians",
                level = TrophicLevel.Producer,
                growthRate = 0.010f, baseCapacity = 60f, naturalLoss = 0.002f, recruitment = 0.0004f,
                optimumTemperature = 26.5f, temperatureTolerance = 5f,
                bleachingThresholdC = 29.5f,
                calcifierWeight = 0.90f, startingStock = 30.65f,
                tint = new Color(0.52f, 0.60f, 0.51f),
                modelHeightMeters = 0.30f, individualsPerModel = 10f,
                attachedToSeabed = true, motion = MotionKind.Fixed,
                appearance = "Small domes or encrusting sheets, grey to greenish-brown, rarely more than 30 cm across, with deeply pitted star-shaped cups.",
                habitat = "Shallow water under 25 m, most common under 10 m. Forms pavements over shallow rock in Cabo Verde.",
                diet = "Mostly sugars from the single-celled algae living in its tissue, topped up by catching small drifting animals at night. An organism that both photosynthesises and feeds is called a mixotroph — a coral is an animal, not a plant.",
                roleInModel = "The bleaching indicator, and the demonstration of how slowly some things recover.",
                iucnStatus = "Least Concern",
                regionalStatus = "Forms pavements on shallow Cabo Verde rock",
                oneThingWorthTelling =
                    "This is one of the toughest corals in the Atlantic. It survives conditions that kill more " +
                    "delicate reef-building species — murky water, wide temperature swings, being rolled loose " +
                    "across the seafloor. That toughness is why it is still common while many showier corals are " +
                    "in decline, and it is a useful counterweight to the idea that all corals are equally fragile.",
                simplificationNote = "Bleaching here is modelled as a loss of growth and a faster wasting away above 29.5 °C. Bleached coral is not dead and recovers if the water cools quickly enough.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "sparisoma-cretense",
                commonName = "Parrotfish",
                scientificName = "Sparisoma cretense",
                level = TrophicLevel.PlantEater,
                appetite = 0.170f, assimilation = 0.34f, livingCost = 0.03874f,
                birthRate = 0.0220f, starveRate = 0.055f,
                ceiling = 42f, unitMass = 1.20f, presence = 1f,
                refuge = 3.0f, startingStock = 18f,
                tint = new Color(0.90f, 0.45f, 0.38f),
                modelHeightMeters = 0.45f, individualsPerModel = 3f,
                seabedClearanceMin = 3.4f, seabedClearanceMax = 5.6f, motion = MotionKind.Swim, verticalRoam = 0.7f,
                appearance = "Two distinct colour phases. Females are grey with a red saddle and yellow markings; terminal-phase males are reddish-brown and plainer. Up to about 50 cm.",
                habitat = "Shallow rocky and vegetated bottoms. One of the two most common parrotfishes in Cabo Verde.",
                diet = "Algae, scraped from rock with fused beak-like teeth. Also takes juvenile sea urchins.",
                roleInModel = "The main grazer. Its removal is the fastest route to algal overgrowth.",
                iucnStatus = "Not currently listed as threatened",
                regionalStatus = "Locally fished",
                oneThingWorthTelling =
                    "Every one of these fish starts life as a female, and only the largest become males, changing " +
                    "colour completely as they do. Sex is not fixed at birth for a great many fish. It is also why " +
                    "size-selective fishing is dangerous for these species — take the biggest fish and you are " +
                    "taking all the males.",
                simplificationNote = "Sex change is described on this card but is not simulated; the model tracks numbers, not individuals.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "diadema-africanum",
                commonName = "Long-spined sea urchin",
                scientificName = "Diadema africanum",
                level = TrophicLevel.PlantEater,
                appetite = 0.035f, assimilation = 0.30f, livingCost = 0.00764f,
                birthRate = 0.0270f, starveRate = 0.038f,
                ceiling = 400f, unitMass = 0.25f, presence = 1f,
                detritusFraction = 0.20f,
                refuge = 3.0f, startingStock = 45f,
                calcifierWeight = 0.30f,
                tint = new Color(0.14f, 0.12f, 0.19f),
                modelHeightMeters = 0.24f, individualsPerModel = 8f,
                attachedToSeabed = true, motion = MotionKind.Crawl, roamRadius = 0.7f,
                appearance = "Dark globe with very long, fine, mobile black spines.",
                habitat = "Shallow rocky bottoms across the eastern Atlantic archipelagos and West Africa.",
                diet = "Algae, grazed continuously, plus detritus and encrusting material — which is why it keeps going once the algae are gone.",
                roleInModel = "The runaway grazer. At high density it strips vegetation to bare rock.",
                iucnStatus = "Not listed",
                regionalStatus = "Has suffered severe mass-mortality events",
                oneThingWorthTelling =
                    "This urchin has been both villain and victim. Dense populations created bare barrens across " +
                    "these islands for decades. Then, beginning in 2022, a disease swept through and killed most " +
                    "of them, with some island populations falling by more than 99 percent. The cause is still not " +
                    "fully understood. Losing a grazer that was itself a problem does not restore balance — it " +
                    "produces a different imbalance, and the ecosystem is still adjusting.",
                simplificationNote = "The 2022 mass-mortality disease is described on this card but is not simulated.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "fissurella",
                commonName = "Keyhole limpet",
                scientificName = "Fissurella sp.",
                level = TrophicLevel.PlantEater,
                appetite = 0.004f, assimilation = 0.30f, livingCost = 0.00085f,
                birthRate = 0.0327f, starveRate = 0.030f,
                ceiling = 200f, unitMass = 0.05f, presence = 1f,
                refuge = 0.30f, startingStock = 26f,
                calcifierWeight = 0.45f,
                tint = new Color(0.56f, 0.48f, 0.40f),
                modelHeightMeters = 0.07f, individualsPerModel = 10f,
                attachedToSeabed = true, motion = MotionKind.Crawl, roamRadius = 0.3f,
                appearance = "Low conical shell with a small opening at the apex, giving the group its name.",
                habitat = "Shallow rock. Cabo Verde holds several lineages found nowhere else.",
                diet = "Algal film and turf, rasped from rock.",
                roleInModel = "A slow, steady grazer and reliable octopus prey.",
                iucnStatus = "Not individually assessed",
                regionalStatus = "Endemic lineages are inherently vulnerable",
                oneThingWorthTelling =
                    "These limpets have young that drift only briefly before settling, so they rarely cross the " +
                    "water between islands. Populations on different islands have been separated long enough to " +
                    "become distinct species found on one island and nowhere else on Earth. Isolation makes new " +
                    "species, and it also makes them fragile — a species that exists on one island can be lost entirely.",
                simplificationNote = "Given at genus level. The specific Cabo Verde lineage is still to be confirmed.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "panulirus-echinatus",
                commonName = "Brown spiny lobster",
                scientificName = "Panulirus echinatus",
                level = TrophicLevel.PlantEater,
                appetite = 0.030f, assimilation = 0.32f, livingCost = 0.00697f,
                birthRate = 0.0595f, starveRate = 0.030f,
                ceiling = 40f, unitMass = 0.80f, presence = 1f,
                detritusFraction = 0.40f,
                refuge = 2.0f, startingStock = 14f,
                calcifierWeight = 0.35f,
                tint = new Color(0.72f, 0.36f, 0.24f),
                // Body plus those body-length antennae, which the mesh includes.
                modelHeightMeters = 0.55f, individualsPerModel = 3f,
                attachedToSeabed = true, motion = MotionKind.Crawl, roamRadius = 1.1f,
                appearance = "Spiny-shelled lobster with long antennae and no large claws, dark brown with pale spots.",
                habitat = "Rocky crevices in shallow water. A main coastal catch in Cabo Verde.",
                diet = "Algae, detritus and small animals — genuinely an omnivore.",
                roleInModel = "The octopus's principal prey, and the link that makes the shark's removal matter.",
                iucnStatus = "Not listed",
                regionalStatus = "Locally and commercially fished",
                oneThingWorthTelling =
                    "Spiny lobsters have no crushing claws. Their defence is armour, spines and a crevice to back " +
                    "into. This is precisely why an octopus can take them — an octopus does not need to overpower " +
                    "a lobster, only to reach into the crevice. Predator and prey are shaped by each other.",
                simplificationNote = "Sits between plant eater and hunter in reality; the pyramid places it with the plant eaters.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "octopus-vulgaris",
                commonName = "Common octopus",
                scientificName = "Octopus vulgaris",
                level = TrophicLevel.Hunter,
                appetite = 0.090f, assimilation = 0.36f, livingCost = 0.01739f,
                birthRate = 0.0139f, starveRate = 0.050f,
                ceiling = 9f, unitMass = 2.50f, presence = 1f,
                refuge = 2.5f, startingStock = 5f,
                tint = new Color(0.80f, 0.42f, 0.62f),
                modelHeightMeters = 0.60f, individualsPerModel = 1f,
                seabedClearanceMin = 0.20f, seabedClearanceMax = 1.10f, motion = MotionKind.Swim, verticalRoam = 0.9f,
                appearance = "Soft-bodied, eight-armed, and able to change colour and skin texture almost instantly.",
                habitat = "Rock, rubble and sand from the shore down to the shelf. Dens in crevices.",
                diet = "Crustaceans and molluscs, occasionally fish.",
                roleInModel = "The mesopredator — the mid-level hunter whose numbers rise when the shark above it is removed.",
                iucnStatus = "Not listed as threatened",
                regionalStatus = "Heavily and often unregulated fished",
                oneThingWorthTelling =
                    "The octopus changes colour and texture faster and more completely than almost any other " +
                    "animal, using pigment sacs that muscles pull open and closed, reflective layers beneath, and " +
                    "small muscles that raise bumps in the skin. The whole system is under direct nervous control, " +
                    "which is why the change is nearly instant. It breeds once and then dies — a pattern called " +
                    "semelparity, normal for almost all octopuses.",
                simplificationNote = "Lives about a year in the wild; this reef runs faster than that. The handful of octopuses here are individuals with real genes, but a wild female lays tens of thousands of eggs and almost none survive - the broods here are two to four so that inheritance can be followed at all.",
            });

            list.Add(new SpeciesDefinition
            {
                id = "galeocerdo-cuvier",
                commonName = "Tiger shark",
                scientificName = "Galeocerdo cuvier",
                level = TrophicLevel.TopPredator,
                appetite = 4.0f, assimilation = 0.40f, livingCost = 0.13335f,
                birthRate = 0f, starveRate = 0.012f,
                ceiling = 2f, unitMass = 60f,
                // A visitor, not a resident: it is on this patch about a fifth of the
                // time, and it leaves rather than starves.
                presence = 0.20f, transient = true,
                refuge = 0f, startingStock = 1f,
                tint = new Color(0.42f, 0.52f, 0.62f),
                modelHeightMeters = 3.2f, individualsPerModel = 1f,
                seabedClearanceMin = 1.5f, seabedClearanceMax = 2.9f, motion = MotionKind.Swim, verticalRoam = 0.5f,
                appearance = "Commonly 3.3 to 4.3 m; females can exceed 5 m. Blunt snout and faint vertical barring that fades with age.",
                habitat = "Coastal and oceanic. Individuals in Cabo Verde have been tracked across the full life cycle.",
                diet = "Extremely broad — fish, turtles, seabirds, molluscs, crustaceans, carrion.",
                roleInModel = "The transient apex predator whose absence reshapes everything below.",
                iucnStatus = "Near Threatened",
                regionalStatus = "Present in the eastern Atlantic from Morocco southward",
                oneThingWorthTelling =
                    "Reputation and behaviour do not match. Tracking work has repeatedly found tiger sharks " +
                    "spending much of their time cruising shallow vegetated seafloor rather than patrolling open " +
                    "water — so consistently that researchers have used camera-carrying tiger sharks to map " +
                    "seafloor habitat that ships and satellites had missed. The animal is a survey instrument as " +
                    "well as a predator. One female tagged in Cabo Verde completed a round trip across the " +
                    "Atlantic of nearly 18,000 km.",
                simplificationNote = "Highly migratory in reality. Modelled as present or absent rather than as a population, because it visits this patch rather than living on it.",
            });

            return list.ToArray();
        }
    }
}

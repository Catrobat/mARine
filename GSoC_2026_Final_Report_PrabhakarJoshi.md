<div align="center">
  <h1>Upgradation of AR-Based Interactive and Procedural Marine Ecosystem Simulation</h1>
</div>
<div align="center">
  <h3>Google Summer of Code 2026 · Final Work Product</h3>

  [![Get it on Google Play](https://img.shields.io/badge/Google%20Play-Download-414141?logo=googleplay&logoColor=white)](https://play.google.com/store/apps/details?id=com.Arishna.MarineBiologyAR&hl=en_IN)
  [![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
  [![Platform](https://img.shields.io/badge/Platform-Android-green)]()
  [![Unity](https://img.shields.io/badge/Unity-6000.3.14f1%20LTS-white)](https://unity.com/)
  [![GSoC 2026](https://img.shields.io/badge/GSoC-2026-yellow)](https://summerofcode.withgoogle.com/)
  [![Status](https://img.shields.io/badge/Status-Merged%20to%20main-brightgreen)]()
</div>

---

## At a Glance

| Field | Detail |
|---|---|
| **Contributor** | Prabhakar Joshi ([@joshi-p](https://github.com/joshi-p)) |
| **Organisation** | International Catrobat Association |
| **Project** | Upgradation of AR-Based Interactive and Procedural Marine Ecosystem Simulation |
| **Repository** | [Catrobat/mARine](https://github.com/Catrobat/mARine) |
| **Engine** | Unity 6000.3.14f1 LTS · Universal Render Pipeline · AR Foundation / ARCore |
| **Backend** | Firebase Authentication · Firestore · Unity Addressables (remote content delivery) |
| **Headline deliverable** | Living Ecosystem & Genetics - 49 scripts, +11,437 lines, zero scene or bundle edits |
| **Catalogue** | 293 organisms in the AR Spawner, served from the project's Text-to-3D pipeline |
| **Field study** | Controlled AR-vs-conventional classroom experiment, Grade 6–7, two cohorts of ten |
| **Licence** | GNU AGPL v3.0 |

---

## Table of Contents

- [Project Overview](#project-overview)
  - [The Challenge](#the-challenge)
  - [The Solution](#the-solution)
  - [Key Accomplishments](#key-accomplishments)
- [Phase 1 - Platform, Architecture and Backend](#phase-1--platform-architecture-and-backend)
  - [Application Architecture Upgrade](#application-architecture-upgrade)
  - [Unity LTS Migration](#unity-lts-migration)
  - [Addressable-Based Asset Offloading Architecture](#addressable-based-asset-offloading-architecture)
  - [Firebase Authentication Integration](#firebase-authentication-integration)
  - [Backend-Compatible Ocean Explore Data Pipeline](#backend-compatible-ocean-explore-data-pipeline)
  - [AR Spawner - A 293-Organism Catalogue](#ar-spawner--a-293-organism-catalogue)
  - [Integration Work](#integration-work)
- [Phase 2 - The Living Ecosystem & Genetics System](#phase-2--the-living-ecosystem--genetics-system)
  - [The Constraint That Shaped Everything](#the-constraint-that-shaped-everything)
  - [How It Attaches to the App](#how-it-attaches-to-the-app)
  - [Milestone 1 - The Living Reef](#milestone-1--the-living-reef)
  - [Milestone 2 - The Octopus](#milestone-2--the-octopus)
  - [Milestone 3 - Memory and Meaning](#milestone-3--memory-and-meaning)
  - [The Ecosystem Report](#the-ecosystem-report)
  - [Interface](#interface)
  - [Performance](#performance)
- [Phase 3 - Classroom Research Study](#phase-3--classroom-research-study)
- [Scientific Grounding](#scientific-grounding)
- [Verification and Testing](#verification-and-testing)
- [Quick Start](#quick-start)
  - [Prerequisites](#prerequisites)
  - [For Educators & Students](#for-educators--students)
  - [For Developers](#for-developers)
- [Project Structure](#project-structure)
- [Usage Guide](#usage-guide)
- [Known Limits and Open Items](#known-limits-and-open-items)
- [Future Goals](#future-goals)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgements](#acknowledgements)
- [Connect With the Vision](#connect-with-the-vision)

---

## Project Overview

This project upgrades an AR-based Marine Ecosystem Simulation along three axes at once: **realism**, **scalability**, and **performance on the mid-range Android devices that schools actually own**.

The platform already offered an interactive AR experience. What it lacked was ecosystem dynamics with real biology underneath them, and an infrastructure capable of growing without a rebuild for every asset change. This project addressed both - and then went one step further, by taking the finished application into a classroom and measuring whether it teaches better than the slides it hopes to replace.

### The Challenge

Marine biology is taught with static pictures. A photograph of a flounder cannot show a student that both of its eyes are on one side of its head; a diagram of a food web cannot show what happens forty days after the shark is removed. Meanwhile, on the engineering side, an AR application that ships all of its content inside the APK cannot scale, cannot be updated independently, and cannot be worked on by several contributors at once without collisions.

### The Solution

A rebuilt content and identity backend that lets the application grow remotely, plus a **living, simulated reef with genuine population dynamics and Mendelian genetics** that a learner can enter, disturb, question, save, return to, and export as a research-style PDF report.

### Key Accomplishments

- **Migrated the project to Unity 6000.3.14f1 LTS**, resolving package incompatibilities and clearing Google Play Console requirements.
- **Replaced in-build content with a Unity Addressables delivery pipeline** - remote hosting, on-demand download, smaller builds, independent content updates.
- **Integrated Firebase Authentication end to end** - Google Sign-In, email/password, guest login, password reset, persistent sessions.
- **Rebuilt the Ocean Explore entry flow** as a configurable, backend-compatible pipeline, removing hardcoded scene dependencies.
- **Built the AR Spawner into a 293-organism catalogue**, served from the project's own Text-to-3D generation pipeline rather than from hand-authored prefabs.
- **Designed and built the Living Ecosystem & Genetics system** - a nine-species food web, individual octopus agents with three-gene Mendelian inheritance, a reasoning engine, persistent memory, and an on-device PDF report generator, in 49 scripts and 11,437 lines, **without editing a single scene, prefab or Addressable bundle**.
- **Verified the simulation numerically** rather than by eye: a Python reference model tuned first, then proven reproduced exactly by a headless C# harness.
- **Ran a controlled classroom experiment** comparing AR against a conventional lesson with content held word-for-word identical.

---

## Phase 1 - Platform, Architecture and Backend

*Community bonding through midterm.*

### Application Architecture Upgrade

The overall architecture was redesigned for modularity, scalability and collaborative development. Several tightly coupled systems were refactored into independent modules, which made subsequent development materially easier and cut the knowledge-transfer and maintenance overhead that a multi-contributor project accumulates.

- Modular system organisation
- Clean separation of responsibilities between feature areas
- A comprehensive project report covering the architecture as rebuilt
- A structured codebase diagram, so a new contributor can orient without reading every file

### Unity LTS Migration

The project was upgraded to **Unity 6000.3.14f1 LTS**, addressing security updates, Android compatibility, Google Play Console requirements, and long-term engine support.

The migration was not a version-number change. It required resolving package compatibility failures across the dependency graph and updating project dependencies to versions that agreed with one another under the new editor.

### Addressable-Based Asset Offloading Architecture

The single largest architectural improvement of the first half: a completely new asset delivery pipeline built on **Unity Addressables**.

| Before | After |
|---|---|
| Content lives in the APK | Content is hosted remotely and fetched at runtime |
| Every content change needs a full rebuild | New assets are published independently |
| Large install size | Smaller application build, on-demand download |
| Contributors collide on binary assets | Content and code evolve on separate tracks |

This also unlocked a workable CI/CD story and made parallel contribution practical - the difference between several people sharing one repository and several people fighting over one repository.

### Firebase Authentication Integration

Firebase Authentication was integrated into the application, implementing Google Sign-In, email/password authentication, guest login, password reset, persistent login, and secure session handling.

This is the foundation the project needs for cloud-based personalisation and per-student progress tracking, and it is the prerequisite for any classroom deployment where a teacher expects to see who did what.

### Backend-Compatible Ocean Explore Data Pipeline

A configurable exploration framework for the Ocean Explore module, letting a user choose an exploration mode and an environment **before** entering AR rather than being dropped into a hardcoded scene.

- A configurable `FreeExploreConfig` scene with an environment carousel
- Runtime environment selection and AR placement flow
- Portal and Boundless modes wired into scene-based navigation
- Architecture prepared for modular content delivery

The effect is that scene dependencies are no longer baked in, and content can be loaded dynamically. Everything built in Phase 2 depends on this - the Living Ecosystem attaches itself to an environment profile chosen through exactly this flow.

### AR Spawner - A 293-Organism Catalogue

The AR Spawner was rebuilt around a **293-organism catalogue**, and the models in it are not hand-authored prefabs shipped in the build. They are **generated by the project's own Text-to-3D pipeline**, developed by **@Shivansh**, and pulled in at runtime.

Work delivered on the application side:

- **Firebase and GitHub backend** for the AR Spawner module, so the catalogue is served rather than bundled
- **Runtime model importing** and the asset-processing path that receives generated models and makes them spawnable
- **Application-level architecture** for the Text-to-3D workflow - validated in a separate environment before it touched the shipping app
- Backend expanded and stabilised to cover both the AR Spawner and Free Explore modules
- Camera pipeline fix that had been blocking spawner use in AR

The result is that the catalogue grows without a rebuild. Adding an organism is a content operation, not an engineering one - which is the whole point of the Addressables work above, applied to the part of the app that needs it most.

### Integration Work

Alongside my own features, I brought three other contributors' modules into the shipping application and handled the knowledge transfer around them: the **Module Builder** (@Gunjan2004), the **multilingual AI Assistant** (@rohanshrma222), and the **Boundless procedural terrain system** (@KarruHarin). In each case the work was encapsulation, user-flow design and architectural compatibility - making the module reachable and safe to run without disrupting anything around it.

---

## Phase 2 - The Living Ecosystem & Genetics System

*Midterm through final. 49 scripts under `Assets/Scripts/LivingEcosystem/` - 111 files, +11,437 lines.*

Three milestones were specified. All three are complete.

| Milestone | Status | What it delivers |
|---|---|---|
| **1 - The Living Reef** | Complete | Nine-species food web, health stages, reasoning engine, procedural organisms |
| **2 - The Octopus** | Complete | Individual agents, Mendelian genetics, breeding tool, family tree |
| **3 - Memory and Meaning** | Complete | History, journal, save/resume, Welcome Back card, two-page PDF report |

Each milestone ends with something that works and could ship on its own. If work had stopped after any one of them, what existed would still have been useful to a learner.

### The Constraint That Shaped Everything

The application's scenes are **Addressables, fetched remotely at runtime**. Editing a scene means rebuilding and re-uploading a bundle - which breaks every contributor's working copy and every already-deployed client until they re-sync.

So the feature was built to touch none of them.

Only `FreeExploreConfig`, `CustomEnvBuilder` and `FreeExploreEndless` were in scope. `FreeExplore` was never touched.

### How It Attaches to the App

The whole feature is **additive**. Across the three pre-existing files it modifies, it adds **18 lines** and changes or deletes none:

| File | Change |
|---|---|
| `EnvironmentProfile.cs` | One `EcosystemSettings` field, plus one line in `Clone()` |
| `EnvironmentBounds.cs` | One call to `ecosystem.Clamp()` |
| `EnvironmentEditorUI.cs` | Six lines calling `EcosystemFormSection.Build(...)` |

Nothing is placed in a scene. `LivingReefBootstrap` uses `[RuntimeInitializeOnLoadMethod]` to subscribe to `SceneManager.sceneLoaded`; when `FreeExploreEndless` loads it spawns the controller, and **every canvas, panel, button and renderer is constructed in code**. The scene name is matched case-insensitively, because the asset on disk is `freeExploreEndless.unity` while the Addressable address is `FreeExploreEndless`.

Three consequences worth knowing:

1. Existing Addressable bundles keep working, untouched.
2. Deleting `Assets/Scripts/LivingEcosystem/` restores the app exactly as it was.
3. `FreeExplore` cannot be affected, because the bootstrap only ever matches one scene name.

Every reason the feature declines to start is **logged out loud**. A silent no-op is indistinguishable from a broken build, and there are several legitimate reasons not to run: no environment profile selected, a profile saved before the feature existed, or the ecosystem switched off for that environment.

### Milestone 1 - The Living Reef

Nine species from the Cabo Verde shallow reef, defined in `SpeciesLibrary.cs` and stamped with `RosterVersion = 3` so a stale generated asset can no longer silently shadow it. Fourteen feeding links connect them.

| Species | Binomial | Role |
|---|---|---|
| Calcareous green alga | *Halimeda* sp. | Producer |
| Fan alga | *Padina* sp. | Producer |
| Lesser starlet coral | *Siderastrea radians* | Producer (bleaches) |
| Parrotfish | *Sparisoma cretense* | Plant eater |
| Long-spined sea urchin | *Diadema africanum* | Plant eater |
| Keyhole limpet | *Fissurella* sp. | Plant eater |
| Brown spiny lobster | *Panulirus echinatus* | Plant eater |
| Common octopus | *Octopus vulgaris* | Hunter (agent-driven) |
| Tiger shark | *Galeocerdo cuvier* | Top predator (transient) |

#### What the tick actually computes

- **Producers** - logistic growth against a carrying capacity modulated by light, temperature and acidity, plus a recruitment term so a population at zero can return from drifting spores.
- **Consumers** - a multi-prey **Holling type II** disc equation with preference weights and a per-prey half-saturation, so a predator switches prey as availability shifts instead of exhausting one in list order.
- **Prey refuge** - every prey pool keeps a biomass no predator can reach. Without it the model has no stable low equilibrium and always collapses.
- **Detritus loop** - 80% of biomass lost to death and 20% of grazing losses feed a detritus pool, remineralised at 1.5% per day.
- **Energy balance** - a 35% surplus gives full breeding, a 50% deficit gives full starvation, scaled linearly between.
- **Coral bleaching** - loss of growth and faster wasting above 29.5 °C; bleached coral recovers if the water cools soon enough.

The tiger shark is modelled as *present or absent* rather than as a population, because it visits this patch rather than living on it.

#### Health, reasons and rewind

`EcosystemHealth` reports a level (green / amber / red) and one of six collapse stages: **Healthy, Imbalance, Overgrazing, Starvation, Collapse, Barren**. Health is judged against *grazeable* producer biomass, not total - the coral is a producer but is not grazed, and measuring against the total would let coral mask a seafloor grazed to bare rock.

`ReasonEngine` turns state into explanation. **Eleven conditions** are recognised, each producing *what is happening*, *why*, and *what happens next*: predator removed, mesopredator release, overgrazing, producers falling, water too warm, warm water, acidified water, no grazers, no producers, recovering, balanced. The Why panel and the PDF report read this same list, so they cannot disagree with each other.

`RewindBuffer` keeps three snapshots at 30-day intervals, so a decision can be undone and retried.

#### Time and appearance

`EcosystemClock` accumulates `Time.deltaTime` while the app runs. Speed is **Paused**, **Normal** (2 s per simulated day) or **Fast** (0.25 s per day). This is entirely separate from the away-time calculation, which reads wall-clock time from a file - the two never interact, so time spent away can never disturb a live session.

Organisms are **procedurally generated meshes**, built in code with smoothing-angle normal averaging; winding was verified by outward-face, signed-volume and radial tests. Each species sits at a biologically sensible depth, attached or free, with a motion kind (fixed, sway, crawl, swim) and a roam radius. The seabed raycast skips AR planes, so organisms sit on the terrain rather than floating at AR session plane height. The reef and its tab come to life only **once the environment is placed on a scanned plane** - not when the plane is first detected.

### Milestone 2 - The Octopus

Octopuses are **individuals, not a number pool**. At most five live agents at a time, each with age, energy, state and identity; the simulation hands their births and deaths to the agent layer via an `agentManaged` flag, so the pool and the agents never both decide.

#### Genes

| Gene | Letter | What it changes |
|---|---|---|
| Camouflage | A / a | Chance of escaping the shark; skin colour when drawn |
| Body size | B / b | Model scale, appetite, and share of biomass taken |
| Heat tolerance | C / c | Metabolic cost in warm water |

Two copies each, dominant/recessive with an additive component. One capital copy is enough to show the trait, so only a lower-case pair shows the weaker version. Mutation rate is **1.2% per allele**. Individual variation is layered on top of the genotype with a per-animal noise seed, so two identical genotypes are not identical animals.

#### Life history

| Parameter | Value | Note |
|---|---|---|
| Maturity | 92 days | ~3 simulated months |
| Maximum age | 330 days | About a year; most breed and die first |
| Brood duration | 34 days | She fasts for the whole of it |
| Male decline after mating | 26 days | Dies well before the eggs hatch |
| Brood size | 2–4 | Wild is tens of thousands; see *Known Limits* |
| Daily breeding chance | 1.4% | Scaled up to 4× with age |
| Settlement from plankton | every 38 days if below 2 | Larvae arriving from elsewhere |
| Generations kept in full | 8 | Older ones collapse to a summary |

**Semelparity is modelled properly: breeding is what kills them.** The female stores the sperm packet, so the male can die - as he always does - long before the eggs hatch, without the brood losing its father. Brooding is tracked as a *countdown* rather than as a state, so a transient state change such as fleeing the shark cannot silently cancel it.

#### What a learner can do with them

- **Inspect** - tap an octopus for its name, sex, age, state, genotype and traits.
- **Breed** - choose two animals; a Punnett grid predicts the ratios before the brood hatches, and the actual brood of 2–4 usually does not match it. **That mismatch is the lesson.**
- **Family tree** - the pedigree survives death, because dead animals are kept as ~20-byte records rather than as agents. Eight generations in full, older ones summarised.
- **Watch drift** - at five animals an allele can vanish through luck alone. Genetic drift acting against selection is *shown in the app*, not asserted in a caption.

Breeding is also staged visually: the chosen female glows and settles, a male approaches, and a circling sequence plays.

### Milestone 3 - Memory and Meaning

| System | What it does |
|---|---|
| `ReefHistory` | One byte per figure, 13 bytes per sample (9 species + 3 gene frequencies + 1 health level), sampled every 5 days, 60 samples kept - **300 days of history in under a kilobyte**, where the same data as JSON floats would be about six |
| `ReefJournal` | The last 20 events worth remembering, stored as a **code plus a subject** rather than a sentence - so a reef saved in English still reads correctly in German, and each event costs a few bytes instead of sixty |
| `ReefChronicle` | Edge-triggered observation. Recording "the coral is bleached" every day would fill the journal in under a month |
| `ReefSave` / `ReefSaveFile` | One small JSON file per environment under `persistentDataPath/reefs/`, agents and ancestors packed as base64 binary, **4 KB budget checked on every write** |

Save guards check **both** file format and `RosterVersion`: a save written against a different species order would put one species' numbers into another's pool. Saving happens on **pause, focus loss, quit and destroy** - the four ways a session ends on a phone.

Events are ranked so the learner's own actions outrank births, which outrank percentage changes in biomass.

#### Time away

**One real hour away is one day in the ocean, capped at 14 days.** Under an hour away shows nothing at all - coming back after ten minutes to be told nothing much happened is worse than not being told anything.

The **Welcome Back card** appears automatically after the environment is placed, shows at most four ranked lines, and when the 14-day cap applies it says so out loud rather than quietly truncating. A learner returning after a month should not have to wonder why their reef is only a fortnight older.

### The Ecosystem Report

A **two-page PDF**, laid out the way a researcher writes for themselves: figures and short factual lines rather than teaching prose. An earlier draft carried a paragraph of explanation under every chart and ran to five pages.

| Section | Contents |
|---|---|
| Masthead | Environment name, day, date, health verdict as a coloured chip |
| Setup | Temperature, pH, starting life, species present and left out, prediction against outcome |
| Populations | Line chart of every animal, plus a start/finish table with percentage change |
| Energy | Four-band pyramid with biomass in kg |
| What happened | Up to 12 ranked events in date order, then why it is like this now |
| Octopus pedigree | Counts, generations, and the family tree with genotypes and parent links |
| Gene frequencies | Chart over time, plus each gene's share and whether it is fixed or lost |
| Species | One line each: name, binomial, role, IUCN status |
| Footnote | What the model simplifies, in two lines |

#### Why it is written in C#

The design proposed composing the report as HTML and rendering it on the device. Unity has no HTML renderer, and the platform ones sit behind a native plugin that can only be exercised on a phone - which would have meant every layout change costing a full device build.

`PdfWriter.cs` **writes the PDF directly instead** - no plugin, no dependency - so a report can be generated and checked on a laptop. Type is Helvetica, one of the fourteen fonts every PDF reader must provide, so nothing is embedded and a full report is about **45 KB**. Everything sits on a **13-point baseline grid**, so line spacing is even from the masthead to the footnote. The family tree is drawn as boxes by generation with genotypes and parent lines; wide generations wrap rather than losing members.

#### How it is shared

On Android the file is written into the Downloads folder through **MediaStore**, then offered to the share sheet. That route was chosen because it needs no `FileProvider` and therefore **no change to `AndroidManifest.xml`** - a file shared by every scene in the project, including those out of scope. The project's minimum SDK is 29, exactly where MediaStore's Downloads collection became available, so nothing is given up. The PDF also survives uninstalling the app, which is right for something meant to be handed in.

The file is written **before** the share sheet is attempted; if the sheet fails, the report still exists. An in-app toast names the file and where it went, and works in the Editor too, unlike Android's native toast.

### Interface

Built **entirely in code**, so nothing in the host scene is touched or can be disturbed.

- **Slide-out reef panel** - water controls, per-species readouts with trend arrows, octopus section, energy pyramid, health and day, a **Why?** button, and *Save my reef report*.
- **The panel does not pause the simulation**, so cause and effect stay observable while it is open.
- **Config section** - Living Ecosystem controls added to `CustomEnvBuilder`: on/off, temperature, acidity, starting life, speed, which species are present, and the prediction prompt *("What do you think will happen?")*.
- **Overlays** - Why panel, organism picker, octopus inspector, breeding tool, family tree, barren prompt, Welcome Back card, toast.
- **Reduced clutter** - duplicate close buttons removed, padding aligned across the panel, and dense genetics readouts cut back after they were judged overwhelming.

### Performance

The Editor was measured at 32 fps. The dominant cost was **material instancing**: a new `Material` per drawn organism meant 48 instances produced 48 materials and 48 draw calls, with no batching at all.

| Fix | Effect |
|---|---|
| One shared material per species, with `MaterialPropertyBlock` for per-octopus camouflage and glow | Per-instance colour no longer breaks batching |
| Cached `Shader.PropertyToID` values | String lookups removed from the hot path |
| Seabed raycast throttled to 4 Hz; `WaterSurface` lookup throttled | Stopped running every rebuild |
| Per-rebuild array allocation removed; agent reference cached | Fewer allocations per frame |
| Panel refresh gated on visibility | It was rebuilding every readout, the pyramid and the octopus block four times a second **behind a closed panel** |
| Text assignment guarded | Assigning the same string still dirties the canvas |

---

## Phase 3 - Classroom Research Study

Post-midterm, the application was taken into a school and tested against the thing it is meant to replace.

> **The question:** Does an AR/3D representation of marine biology teach better than a conventional slide-and-board lesson, **when the content is held identical**?

| | |
|---|---|
| **Design** | Between-subjects, pre-test/post-test, randomised |
| **Level** | Grade 6–7 (11–13 years), two cohorts of 10 |
| **Conditions** | **Cohort C** - static 2D (slides, photographs, board) · **Cohort A** - the app on one phone mirrored to a projector |
| **Session** | 60 min per cohort · 38 min instruction on an identical clock |
| **Instrument** | 13 MCQs, pre and post, inside the hour |

### The rule that makes the data valid

> ### Same words. Different pictures.

Every sentence of science content was scripted and spoken identically to both cohorts. Only the picture changed - a depth-zone diagram against Boundless Mode, a still render against the same model rotated in 3D, a photograph of a flounder's head against the model turned until both eyes are visibly on one side.

The single-device setup is a **strength**, not a limitation. Because Cohort A is whole-class and facilitator-led, it is structurally identical to Cohort C, and the comparison collapses to one variable: *how the concept is depicted*. A 1:1 device setup would have added hands-on agency, self-pacing and novelty on top of AR - four tangled variables, and an uninterpretable result.

### What was measured

Total learning gain, a **SPATIAL** subscale (7 items) and a **VERBAL** subscale (6 items), observer engagement tallies, six matched Likert items, and a confidence shift. Held constant: facilitator, script, vocabulary, examples, time-boxes, quiz, worksheet, glossary, room, participation turns and cohort size.

**The hypothesis that matters** is not that AR wins overall - novelty alone would do that. It is that the advantage **concentrates in the spatial subscale**. Rotating a model to see eye placement is something a single photograph structurally cannot do; there is no mechanism by which AR should improve recall of *"reefs need warm water."* If the spatial-only pattern holds, there is a mechanism - and a mechanism survives the *"it's just because it's shiny"* objection that sinks most AR-in-education studies.

The design was written down before the sessions, including sixteen bias controls and a rule that a concept may be *scored* only if both media can show it. The survey says *"today's lesson"* and never names the medium, so students rate the lesson rather than the technology.

> **Status:** the study was designed, the instruments were built, and the sessions were run. Quantitative analysis is the immediate next step; results are not reported here.

---

## Scientific Grounding

The ecosystem is not a generic reef. It is a specific place, chosen for a specific reason.

**Setting:** the Cabo Verde archipelago, eastern central Atlantic, roughly 570 km off the West African coast, 5–20 m depth. Because the islands are volcanic and isolated, their shallow waters are a mix of black basalt rock, pale carbonate sand made largely from the skeletons of algae and corals, and scattered coral pavement - exactly what the Shallow Sand terrain depicts. The archipelago sits at a biological crossroads, carrying species shared with the Mediterranean and the Canaries, species from tropical West Africa, and a striking number found nowhere else.

**Why not the Caribbean.** The common octopus was for two centuries called *Octopus vulgaris* everywhere it was found, from Japan to Brazil. Genetic work over the last two decades has shown that this was several different species wearing one name; the Caribbean animal has since been separated as *Octopus americanus*. *Octopus vulgaris* in the strict sense belongs to the north-eastern and eastern central Atlantic and the Mediterranean - which includes Cabo Verde, where it is the only bottom-dwelling octopus present.

Setting the ecosystem in Cabo Verde means the octopus can be called by its correct name. The alternative would have been to label a Caribbean animal with a name that no longer applies to it.

> **Teaching point, and it is on the info card:** species are not fixed labels handed down from antiquity. They are hypotheses about which animals belong together, and they get revised when better evidence arrives. The octopus is a live, current example.

A companion Literature Document is the source of truth for everything the application says about marine biology. Info cards, the reasoning strings in the Why panel, the report notes and the assistant's knowledge base all trace back to a statement in it. The rule for writers is explicit: *if you need to state something about marine biology that is not in this document, do not invent it - add it here first, with a source, and have it checked.*

---

## Verification and Testing

Balance was **solved numerically rather than guessed**. A Python model was tuned first, then a headless C# harness proved the port reproduces it exactly.

| Check | Result | What it covers |
|---|---|---|
| Numeric balance proof | exact match | 45.3 urchins at equilibrium, peak 210.8, trough 84.9 - identical to the Python reference |
| Genetics harness | **22 / 22** | Milestone 2's "done when" clauses, run against the real classes |
| Report / PDF harness | **66 / 66** | Structure, layout and content of generated PDFs |
| Mesh checks | pass | Outward-face count, signed volume, radial tests |
| Compile - runtime | 0 errors | `Assembly-CSharp` via Unity's bundled Roslyn |
| Compile - editor | 0 errors | `Assembly-CSharp-Editor` |
| Compile - Android | 0 errors | Forced `UNITY_ANDROID`, to reach the share path |

The PDF harness is worth describing, because **a PDF a reader refuses to open looks exactly like a PDF that was never written**. It confirms every cross-reference offset lands on the object it claims, every stream length matches the bytes actually written, the page tree agrees with itself, text literals are balanced, no text or filled bar runs off the page edge, and a long-running reef still fits two pages. It also builds a one-day-old reef and a reef with no octopuses, which is where the *"not enough history yet"* branches are taken.

### Editor tooling

Everything below runs on a laptop. **No device and no AR session required** - a deliberate constraint, because a feature that can only be tested on a phone is a feature that gets tested rarely.

- **Reef Memory window** - `Tools ▸ Living Ecosystem ▸ Saved Reefs and Reports`. Build a sample report headlessly for a chosen number of days, seed and temperature; inspect each save's day, size against the 4 KB budget, generation reached and how long ago it was closed; delete saves; **backdate** one to test the Welcome Back card without waiting hours; or build the real report mid-session.
- **Ecosystem Balance window** - tune and observe the food web without entering play mode.
- **Living Reef preview scene** - view the procedural organisms on their own.

---

## Quick Start

### Prerequisites

**For Development:**
- Unity **6000.3.14f1 LTS** or higher
- Unity Addressables
- Firebase SDK (Authentication, Firestore)
- AR Foundation + ARCore XR Plugin
- Git

### For Educators & Students

- **Hardware Requirements:**
  - AR-compatible Android device (ARCore support)
  - Minimum 4 GB RAM
  - 2 GB available storage space

1. **Download the App**
   - **[Google Play Store](https://play.google.com/store/apps/details?id=com.Arishna.MarineBiologyAR&hl=en_IN)**
   - **[Direct APK](https://drive.google.com/file/d/1o5uC7fDLakEpLtUWs2OPDNYrKOi-Se_6/view?usp=sharing)**

2. **Read the manual**
   - **[Student & Teacher Manual](https://drive.google.com/file/d/1-FSHMVHkzg_evf02AYSmlyX0VC0fXr2U/view?usp=sharing)** - the full walkthrough of every module, written for classroom use. Start here if you are teaching with the app rather than building it.

3. **Get Started**
   - Install and launch the application
   - Sign in with Google, email, or continue as a guest
   - Complete the interactive tutorial
   - Choose an exploration mode and environment, then scan a flat surface to place it

### For Developers

- **Hardware Requirements:**
  - RTX 2050 or higher
  - Minimum 8 GB RAM
  - 25 GB available storage space

1. **Clone Repository**
   ```bash
   git clone https://github.com/Catrobat/mARine.git
   cd mARine
   ```

2. **Open in Unity**
   ```bash
   # Editor version
   Install Unity 6000.3.14f1 LTS.

   # Open the project from Unity Hub
   From Unity Hub, click Add Project and select the mARine folder.

   # Let Unity resolve and import dependencies
   ```

3. **Configure the backend**
   ```bash
   # Firebase
   Place google-services.json in Assets/ and configure the Firebase console project.

   # Addressables
   Set the remote load path to your hosted ServerData/ catalogue,
   or build Addressables locally for offline development.
   ```

4. **Build Settings**
   - `File → Build Settings`
   - Choose **Android** as the target platform and Switch Platform
   - Ensure ARCore is enabled in **XR Plug-in Management**
   - Minimum API level **29**

5. **Deploy to Device**
   - Connect an ARCore-compatible device
   - Run as a development build, or configure a keystore to bundle
   - Click **Build and Run**

6. **Try the Living Ecosystem without a device**
   - Open **Create Environment**, switch **Living Ecosystem** on, save
   - Play `FreeExploreEndless` with that environment
   - The console logs `[LivingReef] Installed in FreeExploreEndless with seed N` - and if it declines, it logs exactly why
   - Tap a scanned plane to place the environment; the **Reef** tab appears then

---

## Project Structure

```
mARine/
├── Assets/
│   ├── Scripts/
│   │   ├── Auth/                     # Firebase authentication flows
│   │   ├── Custom_Create/            # Module Builder / Create Environment
│   │   ├── FreeExploreGeneral/       # Ocean Explore config, carousel, AR placement
│   │   ├── Language/                 # Localisation
│   │   ├── Species_Questions/        # Assessment content
│   │   ├── TutorialManager/          # Onboarding
│   │   ├── Whisper&TTS/              # Speech in / speech out
│   │   └── LivingEcosystem/          # ── The Living Ecosystem & Genetics system ──
│   │       ├── Data/                 # SpeciesLibrary, SpeciesDefinition,
│   │       │                         #   EcosystemSettings, EcosystemBounds
│   │       ├── Sim/                  # EcosystemSimulation, EcosystemHealth,
│   │       │                         #   EcosystemClock, RewindBuffer
│   │       ├── Reasoning/            # ReasonEngine, EcosystemWarnings
│   │       ├── Genetics/             # Genome, OctopusAgent, OctopusPopulation,
│   │       │                         #   OctopusTraits, OctopusNames, OctopusAgentView
│   │       ├── Memory/               # ReefHistory, ReefJournal, ReefChronicle,
│   │       │                         #   ReefSave, ReefSaveFile, PdfWriter,
│   │       │                         #   EcosystemReport, ReportShare
│   │       ├── View/                 # PopulationRenderer, ReefMeshLibrary,
│   │       │                         #   SpeciesVisualLibrary
│   │       ├── UI/                   # EcosystemPanelUI, WhyPanelUI, OrganismPickerUI,
│   │       │                         #   EnergyPyramidUI, BarrenPromptUI, OctopusUIHub,
│   │       │                         #   OctopusInspectorUI, BreedingToolUI,
│   │       │                         #   FamilyTreeUI, WelcomeBackUI, EcoToast, EcoUIKit
│   │       ├── Config/               # EcosystemFormSection, NoteAutoHeight
│   │       ├── Editor/               # EcosystemBalanceWindow, LivingReefPreviewScene,
│   │       │                         #   ReefMemoryWindow, SpeciesLibraryAssetCreator
│   │       └── (root)                # LivingReefBootstrap, LivingReefController,
│   │                                 #   LivingReefPreview
│   ├── AISpawner/                    # AR Spawner - 293-organism catalogue,
│   │                                 #   Text-to-3D delivery, runtime model import
│   ├── Custom-create/                # Module Builder assets
│   ├── AddressableAssetsData/        # Addressable groups, profiles, link.xml
│   ├── Firebase/                     # Firebase SDK
│   └── Scenes/                       # StartScene, FreeExploreConfig,
│                                     #   FreeExploreEndless, CustomEnvBuilder …
├── ServerData/                       # Built Addressable bundles for remote hosting
├── Packages/                         # Package manifest
└── ProjectSettings/                  # Unity 6000.3.14f1 LTS project settings
```

---

## Usage Guide

> A complete walkthrough of every module, written for the classroom, is in the **[Student & Teacher Manual](https://drive.google.com/file/d/1-FSHMVHkzg_evf02AYSmlyX0VC0fXr2U/view?usp=sharing)**. What follows is the short version.

### For Educators

1. **Build a scenario**
   - Open **Create Environment**
   - Configure terrain, clarity, actors and behaviours
   - Switch **Living Ecosystem** on, set temperature, acidity, starting life and speed
   - Choose which species are present, and write the prediction prompt students will answer
   - Save the environment profile

2. **Run it in class**
   - Have students place the environment on a scanned surface
   - Use the reef panel to change conditions live and let the class watch the consequences
   - Use the **Why?** panel to make the ecosystem explain its own state
   - Collect the exported PDF report as a submitted artefact

### For Students

1. **Enter the reef**
   - Sign in, choose an environment, scan a flat surface and place it
   - The **Reef** tab appears once the environment is down

2. **Experiment**
   - Answer the prediction prompt before you change anything
   - Remove the shark, warm the water, acidify it - and watch what follows
   - Tap an octopus to read its genes; breed two and compare the Punnett prediction against the actual brood
   - Open the family tree and follow a gene across eight generations
   - Use **Rewind** to undo a decision and try a different one
   - Close the app and come back tomorrow - the reef will have moved on without you

3. **Hand it in**
   - Tap **Save my reef report** for a two-page PDF in your Downloads folder

---

## Known Limits and Open Items

Stated plainly, because an educational application's credibility rests on saying what it simplifies.

### Deliberate simplifications

- Broods are 2–4 rather than tens of thousands, so inheritance can be followed at all.
- The shark is present or absent, not a population.
- Parrotfish sex change and the 2022 urchin mass-mortality are described on their info cards but not simulated.
- Three species are given at genus level, pending a regional checklist.
- The lobster sits between plant eater and hunter in reality; the pyramid places it with the plant eaters.

### Open items

- **Agent identity by list order** - `ApplyOctopusGenomes` maps living agents to drawn models in list order, so a model's identity can shift when an octopus dies or hatches. Known, not yet fixed.
- **Android share untested on device** - the JNI path compiles under a forced `UNITY_ANDROID` build, but overload resolution can only be proven on hardware. The file is written before the share sheet is attempted, so a failure there does not lose the report.
- **Frame rate not re-measured** - the material and throttling work is in; the figure after those changes has not been taken.
- **Study results** - sessions were run; quantitative analysis is outstanding.

### A note on `link.xml`

`Assets/AddressableAssetsData/link.xml` is the IL2CPP stripping preserve list. After the next Addressables build it will legitimately gain entries for the new `CreateEnv.Ecosystem` types; commit that separately so it is obvious what it is. **If it ever appears as deleted rather than modified, that is Unity clearing it, and it should be restored** - stripping is exactly what breaks remotely fetched scenes in a release build.

---

## Future Goals

### Short-term (3–6 months)

- **Ecosystem-aware assistant** - pass the live ecosystem snapshot to the in-app assistant as context, so a learner can ask why *their* reef is doing what it is doing, and get an answer about that reef rather than a general one
- **Publish the study** - analyse the collected pre/post and survey data against the pre-registered hypotheses and write it up
- **Fix agent identity mapping** and re-measure frame rate on a 4 GB Android device
- **Cloud-persisted progress** - carry the reef save into Firestore so a student's ecosystem follows them across devices

### Long-term (6–12 months)

- **More ecosystems** - the architecture is species-data-driven; a kelp forest or a deep-ocean roster is a data problem, not a code one
- **Classroom dashboards** - a teacher view over the whole class's reefs, built on the authentication layer already in place
- **Real-world data integration** - live oceanographic feeds driving the temperature and acidity inputs
- **Curriculum alignment** - map the existing content spine onto NGSS and equivalent national frameworks

---

## Contributing

Contributions are welcome from developers, educators, marine biologists and educational-technology researchers.

### Ways to Contribute

- **Scientific accuracy** - validate the food web, the life-history parameters, and the info-card text against the literature
- **Educational content** - curriculum-aligned scenarios and lesson plans
- **Technical development** - features, performance, bug fixes
- **Research** - replicate the classroom study with a larger sample, or with a 1:1 device design
- **Accessibility** - improve the platform for diverse learners
- **Documentation** - guides for educators and developers

### Contribution Process

1. **Fork & Clone**
   ```bash
   git clone https://github.com/Catrobat/mARine.git
   ```

2. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-enhancement
   ```

3. **Develop & Test** - and where you touch the ecosystem, run the harnesses before you open the PR

4. **Submit Pull Request**
   - Include a detailed description
   - Add the educational rationale alongside the technical notes
   - If you changed a scene or an Addressable bundle, say so explicitly

---

## License

This project is licensed under the **GNU Affero General Public License v3.0** - see the [LICENSE](https://www.gnu.org/licenses/agpl-3.0.en.html) file for details.

**Educational Use:**
- Free for all educational institutions and non-profit educational organisations
- Copyleft licence ensuring derivative works remain open source
- Full source code access for educational customisation and transparency
- Commercial use permitted under AGPL terms with source disclosure requirements

---

## Acknowledgements

- **Wolfgang Slany** for sharing his vision and guidance
- **Krishan Mohan Patel** & **Himanshu Kumar** for being with me throughout the journey
- **Google Summer of Code 2026** & **International Catrobat Association** for the opportunity
- **Harin Karru**, **Gunjan**, **Rohan Sharma** & **Shivansh**, my fellow contributors, for the company
- **Marine Biology Experts** for scientific validation of the research design and life-history parameters
- **Educational Partners** for testing and validation
- **Accessibility Advocates** for inclusive design guidance

---

## Connect With the Vision

- **Download**: [Google Play Store](https://play.google.com/store/apps/details?id=com.Arishna.MarineBiologyAR&hl=en_IN) · [Direct APK](https://drive.google.com/file/d/1o5uC7fDLakEpLtUWs2OPDNYrKOi-Se_6/view?usp=sharing)
- **Project Repository**: [Catrobat/mARine](https://github.com/Catrobat/mARine)
- **Student & Teacher Manual**: [How to use the application, module by module](https://drive.google.com/file/d/1-FSHMVHkzg_evf02AYSmlyX0VC0fXr2U/view?usp=sharing)
- **Mid-Term Report**: [Google Docs](https://docs.google.com/document/d/1ccdlE0lbV5Kyqqrnp5Wc2181Dmdkiv4SZvN2qUxHnM0/edit)
- **GSoC 2025 Final Work Product**: [Previous year's contribution](https://gist.github.com/joshi-p/2509578c628d567a28ea0e5216474da6)
- **Organisation**: [catrobat.org](https://catrobat.org/)
- **Contributor**: Prabhakar Joshi · [@joshi-p](https://github.com/joshi-p)

---

<div align="center">
  <sub>Built for Google Summer of Code 2026.</sub>
</div>

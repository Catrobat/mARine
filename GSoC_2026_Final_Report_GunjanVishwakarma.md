<div align="center">
  <h1>Extension of Sandbox Toolkit for Simplifying the Development of Marine based AR Modules</h1>
</div>
<div align="center">
  <h3>Simplifying the Development of Realistic, Configurable Marine AR Ecosystems</h3>

  [![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
  [![Platform](https://img.shields.io/badge/Platform-Android-green)]()
  [![Unity](https://img.shields.io/badge/Unity-6000.3.14f1-white)](https://unity.com/)
  [![GSoC 2026](https://img.shields.io/badge/GSoC-2026-yellow)](https://summerofcode.withgoogle.com/)
</div>

---

## Table of Contents

- [Project Overview](#project-overview)
  - [Key Accomplishments](#key-accomplishments)
- [Key Features](#key-features)
  - [3D Ecosystem & Simulation Engine](#3d-ecosystem--simulation-engine)
  - [Marine Life Behaviour System](#marine-life-behaviour-system)
  - [Sandbox Configuration & Developer Tools](#sandbox-configuration--developer-tools)
  - [Application Experience](#application-experience)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Usage Guide](#usage-guide)
- [What's Left / In Progress](#whats-left--in-progress)
- [Future Goals](#future-goals)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgements](#acknowledgements)
- [Connect With the Project](#connect-with-the-project)

---

## Project Overview

This project extends the marine AR sandbox toolkit so developers and educators can build marine ecosystem AR modules with far less manual setup. The original sandbox was a flat, 2D grid with hand-placed organisms and no real ecological logic. Over GSoC 2026, it was rebuilt into a **configurable 3D multi-layer ecosystem** where organisms move, hunt, flee, feed, and defend themselves autonomously driven by a modular food chain and hunger system rather than scripted per-creature behaviour.

**The Challenge**: Building a believable marine ecosystem in AR normally means hand-scripting every creature's behaviour and manually laying out a scene slow, error-prone, and hard to scale to new species or environments.

**The Solution**: A configurable sandbox where layers, depth, food-chain relationships, and creature behaviour are all data-driven, so new organisms and environments can be added with minimal code changes.

### Key Accomplishments
- Rebuilt the sandbox from a 2D grid into a **configurable 3D layered ecosystem**
- Implemented a **modular marine food chain** with trophic levels and prey mappings
- Added **autonomous movement, hunting, fleeing, and defence behaviours** for marine organisms
- Built a **hunger-driven survival system** governing feeding, starvation, and death
- Built a reusable **Actor Ability System** for composing new organism behaviours
- Improved app startup reliability and made the UI responsive across screen sizes

---

## Key Features

### 3D Ecosystem & Simulation Engine
- **3D Layer-Based Sandbox**: Redesigned the sandbox from a flat 2D environment into a configurable 3D layered grid. Organisms are no longer confined to a single plane they can now interact across multiple depths, which makes predator-prey encounters, schooling, and vertical migration possible for the first time and lays the groundwork for richer ecosystem presets down the line.
- **Configurable Sandbox System**: The number of ecosystem layers and the overall simulation depth are controlled through `SandboxSettings` and generated dynamically at runtime, so a developer can retune an entire environment from a shallow reef to a deep-water column without touching a single line of source code.
- **Sandbox Bounds**: `SandboxBounds` defines the outer edge of the designated environment and steers organisms back inward through smooth, automatic avoidance rather than a hard clamp or teleport, so actors never visibly leave the simulated area and movement still reads as natural.
- **Vertical Swimming & Multi-Layer AR Placement**: Organisms can move up and down across depth layers, and the AR placement logic was extended so creatures can be positioned and interact across those layers in physical space — not just scattered across a single flat plane in front of the camera.

### Marine Life Behaviour System
- **Marine Food Chain**: A modular food chain defines predator-prey relationships through configurable trophic levels and prey mappings, so a new species can be slotted into the chain by data rather than by writing bespoke interaction code for every possible pairing.
- **Autonomous Behaviour & Movement**: Every organism roams on its own, with smooth, natural-looking direction changes driven by an `AutonomousMovement` state machine that governs transitions between roam, hunt, flee, and feed states the ecosystem stays active without any manual scripting per creature.
- **Predator Hunting Behaviour**: Predators continuously scan for nearby prey, switch out of roaming into an active hunt once prey is detected, and resolve the encounter through chance-based hunting logic with a realistic chase-and-capture sequence rather than a guaranteed catch.
- **Prey Escape Behaviour**: Prey detect nearby predators, flee directly away from the threat, and only resume normal roaming once they're a safe distance away, with chance-based escape odds so outcomes vary run to run instead of always resolving the same way.
- **Species Defence Mechanism**: Species-specific defence behaviours such as `CamouflageAbility` and an octopus-style defensive escape sit on a shared, flexible framework, so additional defence types for future species can be added without reworking existing creatures.
- **Hunger-Based Behaviour**: A hunger value gates hunting itself predators only initiate a hunt once hunger crosses a configurable threshold and the same system drives feeding, starvation, and death, so survival becomes a consequence of the simulation rather than a scripted event.

### Sandbox Configuration & Developer Tools
- **Actor Ability System**: `ActorAbility`, `ActorAbilityConfig`, and `ActorAbilityManager` let food-chain and behaviour logic be composed per organism rather than hard-coded per species, with `AbilityUIPanel` and `AbilityButtonUI` surfacing those abilities directly in the editor for quick configuration and testing.
- **Scene Navigation**: Centralised routing between Addressable-managed and built-in scenes, with consistent back-navigation and UI state handling maintained across the sandbox and every supporting screen in the app.

### Application Experience
- **Startup Flow Enhancements**: A version-checked startup sequence with an animated loading bar shows real progress while the app checks for and automatically downloads any updated content bundles, instead of leaving the user staring at a static screen.
- **Startup Reliability Fixes**: Resolved a progress bar that could stall indefinitely on physical Android devices, along with a content-catalog mismatch that was silently causing failed downloads on first launch.
- **Responsive UI**: The UI was reworked to adapt cleanly across different screen sizes and aspect ratios, so layouts hold up consistently on both smaller phones and larger tablets rather than being fixed to one reference resolution.

---

## Quick Start

**Prerequisites**
- Unity 6000.3.14f1 or newer
- AR Foundation / ARCore XR Plugin
- Git

**For Developers**
```bash
git clone https://github.com/catrobat/mARine.git
cd mARine
```
Open the project in Unity Hub, let Unity resolve packages, then go to **File → Build Settings**, select Android, and ensure ARCore is enabled in XR Plug-in Management.

**Try the App**
- [Get it on Google Play](https://play.google.com/store/apps/details?id=com.Arishna.MarineBiologyAR&hl=en_IN)
- [Download the APK](https://drive.google.com/file/d/1o5uC7fDLakEpLtUWs2OPDNYrKOi-Se_6/view?usp=sharing)

---

## Project Structure

```
Assets/
├── Custom_Create/
│   ├── Abilities/          # ActorAbility, ActorAbilityConfig, ActorAbilityManager, CamouflageAbility
│   ├── Behavioural/        # AutonomousMovement, hunting, fleeing, hunger system
│   └── Sandbox/            # SandboxSettings, SandboxBounds, layered grid generation
└── UI/                     # Responsive sandbox and app UI
```

---

## Usage Guide

**For Educators / Developers**
1. Open the sandbox and configure the number of ecosystem layers and depth
2. Place marine organisms food chain, hunger, and behaviour are handled automatically
3. Adjust sandbox bounds and layer settings to fit the target environment
4. Build and deploy to an ARCore-compatible Android device

---

## What's Left / In Progress

- Expanding the Actor Ability System to cover additional species and defence types
- Broader testing of the multi-layer AR placement across varied physical spaces
- Continued UI refinement across the startup and navigation flow

---

## Future Goals

**Living Ecosystem Expansion**
- A Cabo Verde shallow-sand ecosystem as a new environment preset
- A biomass-pool approach to modelling energy flow through the food chain, in place of fixed per-organism hunger values
- A genetics system allowing traits to vary and propagate across organism populations

---

## Contributing

We welcome contributions from developers, educators, and marine biology enthusiasts! All contributions go through the main project repository:

**[github.com/catrobat/mARine](https://github.com/catrobat/mARine)**

**For Developers**
```bash
git clone https://github.com/catrobat/mARine.git
git checkout -b feature/your-enhancement
# develop & test
# submit a pull request with a clear description
```

**For Educators & Students**
- No engine setup required to contribute ideas — organisms, environments, and food-chain relationships are configuration-driven, so new species or ecosystem presets can often be proposed and tested without touching code
- Feedback on ecological accuracy and classroom usability is welcome via issues on the main repository

---

## License

This project is licensed under the GNU Affero General Public License v3.0 — see the [LICENSE](https://www.gnu.org/licenses/agpl-3.0.en.html) file for details.

---

## Acknowledgements

- **Catrobat International Organization** for guidance and support throughout the program
- **Google Summer of Code 2026** for the opportunity
- Fellow contributors and the open-source community

---

## Connect With the Project

- **Project Repository**: [github.com/catrobat/mARine](https://github.com/catrobat/mARine)
- **Try the App**: [Google Play](https://play.google.com/store/apps/details?id=com.Arishna.MarineBiologyAR&hl=en_IN) · [Download the APK](https://drive.google.com/file/d/1o5uC7fDLakEpLtUWs2OPDNYrKOi-Se_6/view?usp=sharing)
- **Organization**: [catrobat.org](https://catrobat.org/)
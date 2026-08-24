<div align="center">
  <h1>Gemini-Powered Ecosystem Narration and
Analysis Interface</h1>
</div>
<div align="center">
  <h3>Marine Biology AR Ecosystem - Catrobat</h3>

  [![GSoC 2026](https://img.shields.io/badge/GSoC-2026-yellow)](https://summerofcode.withgoogle.com/)
  [![Platform](https://img.shields.io/badge/Platform-Android-green)]()
  [![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS%20%7C%206000.1.9f1-white)](https://unity.com/)
  [![Gemini API](https://img.shields.io/badge/AI-Gemini%20API-blue)](https://ai.google.dev/)
</div>

---

*Rohan Sharma · github.com/rohanshrma222 · rohansharmarohansharma22@gmail.com*

Building AI-narrated, audio-rich, camera-driven modules for Catrobat's Marine Biology AR education platform, ahead of and alongside my GSoC 2026 proposal, "Gemini-Powered Ecosystem Narration and Analysis Interface."

---

## Table of Contents

- [Project Overview](#project-overview)
  - [Key Accomplishments](#key-accomplishments)
- [Repositories](#repositories)
- [Key Contributions](#key-contributions)
  - [1. Gemini-Powered Marine Voice Assistant](#1-gemini-powered-marine-voice-assistant)
  - [2. Procedural Marine Soundscape](#2-procedural-marine-soundscape)
  - [3. FaceAR - Scuba Mask Filter](#3-facear---scuba-mask-filter)
  - [4. ROV Placement & Simulation](#4-rov-placement--simulation)
- [Deterministic Narration](#deterministic-narration)
- [Tech Stack](#tech-stack)
- [Acknowledgements](#acknowledgements)

---

## Project Overview

Catrobat's Marine Biology AR platform turns underwater ecosystems into something students can walk into and interact with. My contributions target the layers that make that world feel alive and explainable: a Gemini-powered voice assistant that answers marine biology questions out loud, procedurally generated marine audio (no samples, fully synthesized), an AR face filter that drops the user into a scuba mask, and an AR-placed ROV with physics-driven behavior. Together, these four modules feed directly into the GSoC project I'm proposing, a Gemini narration and analysis layer for the core ecosystem simulation.

### Key Accomplishments
- Shipped a native-Android voice assistant ("Marina") integrating Google Gemini for marine-biology Q&A, using platform STT/TTS instead of bundled model binaries
- Directly applied Gemini API integration experience (from the voice assistant) toward the system-prompt grounding and structured-output design used in my GSoC entry task and proposal
- Built a fully procedural, sample-free marine audio engine synthesizing 5 organism layers in real time on Unity's audio thread with zero heap allocations
- Implemented an AR face-tracked scuba mask filter with an underwater overlay, caustics, and a one-click scene builder for fast integration
- Delivered an AR ROV placement and simulation module: camera recognition, self-righting physics, waypointed marine creatures, and UI polish, over 15 iterative commits

---

## Repositories

This work is being built toward Catrobat's main marine ecosystem project: github.com/Catrobat/mARine

---

## Key Contributions

### 1. Gemini-Powered Marine Voice Assistant

**Repo**: github.com/rohanshrma222/UnityAndriod

A Unity 2022.3+ Android voice assistant, "Marina," that answers marine-biology questions conversationally. This is the piece most directly connected to my GSoC proposal, it's my first shipped integration of the Gemini API into a live Unity/Android pipeline.

- **Native Android SpeechRecognizer**: Low-latency STT, with phrase biasing tuned for marine terms (cephalopods, bioluminescence, cnidarians)
- **Native Android TextToSpeech**: Spoken responses, no bundled neural TTS/STT binaries, keeping the app lightweight
- **Gemini Chat Client**: Driven by a marine-biology-expert system prompt
- **Multi-language Support**: English and German supported end-to-end (recognition, generation, and synthesis)

```
User Speech -> SpeechRecognizer Bridge -> Gemini Chat Client -> TextToSpeech Bridge -> Device Speakers
```

**Recent work**: cleaned up the native STT/TTS implementation, adjusted the UI, and updated documentation to reflect the native-Android-first architecture (dropping earlier bundled-model approaches).

### 2. Procedural Marine Soundscape

**Repo**: github.com/rohanshrma222/proceduralSoundGeneration

All marine audio is generated entirely in real time via a single OnAudioFilterRead callback, no recordings or samples. Five organism layers synthesize simultaneously, mixed with soft-clipping (tanh) instead of hard clamping.

| Layer | Technique | Notes |
|---|---|---|
| Ambient Ocean | Granular synthesis (32-grain cloud) + brown noise | Background water texture, slow filter-sweep LFO |
| Whale | Triangle carrier + FM + Chamberlin SVF bandpass | Alternates Cry (upward FM sweep) and Moan (descending pitch) calls |
| Shark | Sub-bass rumble + tail-beat envelope + turbulence noise | State machine: Idle to Rumbling to Strike, speed-reactive |
| Octopus | Noise bursts + bandpass sweeps | State machine: Idle to JetPulse / InkBurst |
| Seahorse | Karplus-Strong waveguide | Click-train stridulation, distance-reactive rate |

A proximity system drives both volume and behavior frequency per organism (closer = louder + more frequent), with a one-pole lowpass filter simulating underwater high-frequency absorption (20 kHz near to 200 Hz far), plus a global 4 kHz water-medium EQ on the final mix.

**Recent work**: first-phase implementation of the full layered system, followed by a pass improving the whale call design (FM modulation, formant tracking).

### 3. FaceAR - Scuba Mask Filter

**Repo**: github.com/rohanshrma222/FaceAR

A lightweight Unity/ARCore project that detects the user's face via the front camera, overlays a scuba mask, and wraps the view in an underwater scene, built to integrate directly into the main Marine Biology AR application.

- **ARCore Face Tracking**: Built with AR Foundation + URP
- **Custom Shaders**: Water-surface and underwater-tint shaders (caustics, god rays, blue tint)
- **Bubble Particle System**: Mask switching (3 mask types), screenshot capture
- **One-Click Scene Builder**: Editor tool (FaceAR to Setup Complete Scene) that auto-generates materials, prefabs, and the scene, roughly 90 KB of app code, roughly 15-25 MB built APK

**Recent work**: initial full implementation of face tracking, mask overlay, and the underwater shader/UI stack.

### 4. ROV Placement & Simulation

**Repo**: github.com/rohanshrma222/ROV

An AR-placed, physics-driven ROV (remotely operated vehicle) module for exploring the simulated marine environment, built out over 15+ incremental commits.

- **AR Placement Controller**: Camera-based ROV recognition, model rotation, and self-righting physics
- **Waypointed Marine Creatures**: Jitter and sizing fixes for smoother behavior
- **Underwater Screen Overlay**: Fixed to render fullscreen, plus directional lighting adjustments
- **UI Additions**: Back and info buttons, an improved bubble effect replacing the earlier placeholder
- **Release Readiness**: Project/package-name and build-configuration cleanup

**Recent work (chronological)**: initial ROV implementation to AR placement/self-righting physics/XR config to camera-recognition bug fix to back/info buttons to fullscreen underwater overlay fix to improved bubble effect to waypointed creatures to lighting and creature-size adjustments to whale jitter fix.

---

## Deterministic Narration

**Project**: Gemini-Powered Ecosystem Narration and Analysis Interface, an AI narration layer on top of the Unity marine ecosystem simulation.

**Task** (github.com/rohanshrma222/Marine-Ecosystem-narration): a two-stage pipeline where a deterministic state-summarization step condenses raw simulation events into compact JSON. Because the VM available for deployment couldn't reliably support live calls to the Gemini model, the narration step itself is currently implemented deterministically (hardcoded, template-based) rather than generated live at runtime. The pipeline still includes a full pytest suite, a mock mode requiring no API key, and CLI tooling, so the grounding architecture is fully in place.

**Proposed scope**: swapping the deterministic narration step for live Gemini narration once VM/API access allows it, plus causal Q&A ("Why is the coral bleaching?"), natural-language organism spawning, ecosystem health scoring, and full prompt-transparency logging. A future contributor can build directly on top of this pipeline without redesigning it.

---

## Tech Stack

| Simulation / AR | AI / Backend | Audio | Tools |
|---|---|---|---|
| Unity (C#) | Gemini API | OnAudioFilterRead DSP | Git / GitHub |
| AR Foundation / ARCore | Python 3.10+, asyncio | Granular synthesis | pytest / TDD |
| Android native STT/TTS | JSON Schema | Karplus-Strong waveguide | GitHub Actions CI/CD |
| URP / custom shaders | prompt engineering | Chamberlin SVF filters | VS Code |

---

## Acknowledgements

Thanks to mentors Aakash Tyagi, Abha Kumari, Garima Jain, and Kumari Deepika, and to the International Catrobat Association for the opportunity to contribute to this project.

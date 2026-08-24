# mARine – Marine Biology AR App

<div align="center">

  [![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
  [![Platform](https://img.shields.io/badge/Platform-Android-green)](https://github.com/Catrobat/mARine)
  [![Unity](https://img.shields.io/badge/Unity-6000.3.14f1%20LTS-white)](https://unity.com/)
  [![Google Play](https://img.shields.io/badge/Google%20Play-Download-414141?logo=googleplay&logoColor=white)](https://play.google.com/store/apps/details?id=com.Arishna.MarineBiologyAR&hl=en_IN)
  [![GSoC 2026](https://img.shields.io/badge/GSoC-2026-yellow)](https://summerofcode.withgoogle.com/)
</div>

Revolutionizing marine biology education through immersive AR! mARine transforms abstract marine science concepts into tangible interactive experiences, enabling educators to create custom underwater learning environments where students explore marine ecosystems and experiment with environmental variables in real-time.

A student can place a reef on a classroom desk, remove the shark from the food web, and watch the urchins climb and the algae fall over the following minutes - then ask the app why it happened, and take the answer home as a report.

---

## Table of Contents

- [Features](#features)
- [Modules](#modules)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)

---

## Features

**Ecosystem simulation**
- **Living Ecosystem & Genetics**: A nine-species food web that runs in real time - producers, grazers, a hunter and a top predator, connected by fourteen feeding links, with coral bleaching, prey refuges and a detritus loop
- **Mendelian genetics**: Octopuses are individual agents with three inherited genes. Breed two of them, compare the Punnett prediction against the actual brood, and follow a gene across eight generations of the family tree
- **Explains itself**: A reasoning engine turns ecosystem state into plain language - what is happening, why, and what comes next
- **Remembers**: The reef is saved between sessions and keeps running while you are away, one reef day per real hour
- **Exports**: A two-page PDF report of populations, energy, events, pedigree and gene frequencies, generated on the device

**Content creation and delivery**
- **Intuitive Module Builder**: Drag-and-drop interface for educators to create custom learning scenarios without technical expertise
- **AR Spawner**: A catalogue of **293 organisms** streamed on demand, with models produced by the project's own Text-to-3D generation pipeline
- **Addressables asset delivery**: Content is hosted remotely and fetched at runtime, so new assets ship without rebuilding the application
- **Procedural terrain**: Boundless mode generates open marine environments to explore

**Learning experience**
- **Realistic Marine Life Simulation**: Authentic 3D models with natural swimming behaviors, predator-prey interactions, and species-specific characteristics
- **Environmental Control Interface**: Real-time sliders for temperature, acidity and clarity with immediate visual feedback
- **Marina AI**: A multilingual in-app assistant with speech recognition and text-to-speech
- **Human Pose Detection**: AR Foundation body and face tracking for natural gesture interactions with AR marine environments
- **Immersive Effects**: Realistic underwater atmosphere with volumetric water rendering, caustics and dynamic lighting
- **Guided onboarding**: Interactive tutorials, quizzes and mini-games

**Platform**
- **Firebase Authentication**: Google Sign-In, email/password, guest login, password reset and persistent sessions
- **QR Code Sharing System**: Instant module distribution for classroom deployment
- **Unity 6 LTS** on AR Foundation with ARCore

---

## Modules

| Module | What it does |
|---|---|
| **Ocean Explore** | Choose an environment and an exploration mode - Portal or Boundless - then place it on a scanned surface |
| **Living Ecosystem** | The simulated reef: water controls, live population readouts, octopus genetics, health, and the reef report |
| **Module Builder** | Build custom scenarios - terrain, actors, behaviours and scripts - and share them by QR code |
| **AR Spawner** | Browse 293 organisms, place any of them in the room at real scale, and read their facts |
| **Marina AI** | Ask questions by voice or text, in multiple languages |
| **Human Interaction** | Pose-driven interaction with marine life through the device camera |

---

## Screenshots

<div align="center">
  <table>
    <tr>
      <td align="center">
        <img width="300" alt="Living Ecosystem control panel" src="https://github.com/user-attachments/assets/98ea1d18-ac42-4257-b5fc-5a59db1d0dca">
        <br>
        <em>Living Ecosystem - water controls, populations, energy pyramid and health</em>
      </td>
      <td align="center">
        <img width="300" alt="The reef in Boundless mode" src="https://github.com/user-attachments/assets/6c39836e-0bd5-432e-ad3c-fd045776442a" />
        <br>
        <em>The reef running - tiger shark, octopus, lobster and coral</em>
      </td>
    </tr>
    <tr>
      <td align="center">
        <img width="300" alt="AR Spawner placing an organism" src="https://github.com/user-attachments/assets/2c8656d7-ff7b-4283-b00a-f2b6cb5a51f4" />
        <br>
        <em>AR Spawner - any of 293 organisms, placed at real scale</em>
      </td>
      <td align="center">
        <img width="300" alt="Marina AI assistant" src="https://github.com/user-attachments/assets/30276885-aff5-49d9-b265-d58345a4b77c" />
        <br>
        <em>Marina AI - multilingual voice assistant</em>
      </td>
    </tr>
  </table>
</div>

---

## Getting Started

### Prerequisites

#### For Educators & Students

- **Hardware Requirements:**
  - AR-compatible Android device (with ARCore support)
  - Android 10 (API 29) or higher
  - Minimum 4GB RAM
  - 2GB available storage space
  - An internet connection on first run, to download content

#### For Developers

- **Hardware Requirements:**
  - RTX 2050 or higher
  - Minimum 8GB RAM
  - 25GB available storage space

### Installation

#### For Educators & Students

1. **Get Started**
   - Launch the application
   - Sign in with Google or email, or continue as a guest
   - Complete the interactive tutorial
   - Navigate through the different modules of the application

#### For Developers

1. **Clone Repository**
   ```bash
   git clone https://github.com/Catrobat/mARine.git
   cd mARine
   ```

2. **Open in Unity**
   ```text
   # Editor and recommended version
   Install Unity 6000.3.14f1 LTS (or newer).

   # Open the project from Unity Hub
   From Unity Hub, click Add Project and select the mARine folder.

   # Unity will automatically handle packages and dependencies
   Let Unity resolve and import dependencies.
   ```

3. **Dependencies**
   ```text
   # AR
   AR Foundation 6.3.4 with the ARCore XR Plugin.
   Vuforia Engine SDK 11.2.4 - AR target recognition & tracking.

   # Backend and content
   Firebase SDK (Authentication, Firestore).
   Unity Addressables 2.9.1 for remote content delivery.
   glTFast 6.19.0 for runtime model import.

   # Add all packages in Unity
   Ensure each is installed via Unity's Package Manager or as a custom package.
   ```

4. **Configure the backend**
   ```text
   # Firebase
   Place google-services.json in Assets/ and point it at your Firebase project.

   # Addressables
   Set the remote load path to your hosted ServerData/ catalogue,
   or build Addressables locally to work offline.
   ```
   Without this step the app still builds, but remote environments and the organism
   catalogue will not load.

5. **Build Settings**
   - Go to File → Build Settings.
   - Choose Android as the target platform and Switch Platform.
   - Make sure ARCore is enabled in XR Plug-in Management.
   - Set the minimum API level to 29.

6. **Deploy to Device**
   - Connect your ARCore-compatible device.
   - Run as a development build, or
     - Configure signing (keystore for Android) to bundle.
   - Click Build and Run.

> **iOS**: the project ships the ARKit plugin and AR Foundation covers both platforms, so an iOS
> target is buildable, but the released build is Android and iOS is not currently tested.

---

## Documentation

- **[Student & Teacher Manual](https://drive.google.com/file/d/1-FSHMVHkzg_evf02AYSmlyX0VC0fXr2U/view?usp=sharing)** - a full walkthrough of every module, written for classroom use

---

## Contributing

Contributions are welcome from educators, developers, marine biologists, and educational technology specialists! Here's how you can help:

**Ways to Contribute:**
- **Educational Content**: Create curriculum-aligned modules and lesson plans
- **Technical Development**: Implement features, optimize performance, fix bugs
- **Scientific Accuracy**: Validate biological behaviors and ecosystem modeling
- **Accessibility**: Improve platform accessibility for diverse learners
- **Documentation**: Enhance guides and educational resources

**Contribution Process:**

1. **Fork** the repository
2. Create a feature branch:
   ```bash
   git checkout -b feature/your-enhancement
   ```
3. **Develop & Test**: Follow Test-Driven Development and Clean Code principles
4. Commit your changes with meaningful messages
5. Push to your fork and open a **pull request**
6. Include detailed description with educational rationale and technical notes

Please adhere to the existing code style and ensure changes are well-tested in both Unity editor and device environments.

**A note on scenes and bundles**: the project's scenes are delivered as Addressables. Editing a
scene means rebuilding and re-uploading a bundle, which affects every contributor and every
deployed client. If your change touches a scene, a prefab or an Addressable group, say so
explicitly in the pull request.

---

## License

This project is licensed under the **[GNU Affero General Public License v3.0](LICENSE)**.

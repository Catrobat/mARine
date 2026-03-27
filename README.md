# mARine – Marine Biology AR App

<div align="center">

[![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-green)](https://github.com/Catrobat/mARine)
[![Unity](https://img.shields.io/badge/Unity-6000.1.9f1%20LTS-white)](https://unity.com/)
[![GSoC 2026](https://img.shields.io/badge/GSoC-2026-yellow)](https://summerofcode.withgoogle.com/)

</div>

---

## Overview

mARine is an Augmented Reality (AR) platform designed to enhance marine biology education.

It transforms abstract marine science concepts into interactive underwater experiences, enabling users to explore ecosystems, study marine life behavior, and experiment with environmental variables in real time.

Built with Unity, the platform supports both educators and students in creating simulation-based learning environments.

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Project Architecture](#project-architecture)
- [Dependencies](#dependencies)
- [Build & Deployment](#build--deployment)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- **Complete AR Framework**: Modular system with 15+ integrated components for marine ecosystem simulation
- **Intuitive Module Builder**: Drag-and-drop interface for educators to create custom learning scenarios without technical expertise
- **Realistic Marine Life Simulation**: Authentic 3D models with natural swimming behaviors, predator-prey interactions, and species-specific characteristics
- **Environmental Control Interface**: Real-time sliders for temperature, pH, and pollution levels with immediate visual feedback
- **Cross-platform Compatibility**: Unity-based, supporting Android (ARCore) and iOS (ARKit)
- **Human Pose Detection**: MediaPipe-based natural gesture interactions with AR marine environments
- **QR Code Sharing System**: Instant module distribution for classroom deployment
- **Immersive Effects**: Realistic underwater atmosphere with volumetric water rendering and dynamic lighting

---

## Screenshots

<div align="center">
<table>
<tr>
<td align="center">
<img width="300" src="https://github.com/user-attachments/assets/96405860-0e60-4d72-ade8-4eaaed1c436d" />
<br><em>Marine Ecosystem Simulation</em>
</td>
<td align="center">
<img width="300" src="https://github.com/user-attachments/assets/8878ea37-c53a-4eb9-95ab-ba5ffdd57f22" />
<br><em>Interactive AR Experience</em>
</td>
</tr>
</table>
</div>

---

## Getting Started

### Prerequisites

#### For Educators & Students

- AR-compatible mobile device (ARCore / ARKit)
- Minimum 4GB RAM
- 2GB available storage

#### For Developers

- GPU: RTX 2050 or higher
- Minimum 8GB RAM
- 25GB available storage

---

## Installation

### For Educators & Students

Download the application:

- [MarineBiology_AR](https://drive.google.com/file/d/1o4Zu4FMbyJZXPOKiPRe0a28uvSHgDGBP/view?usp=sharing) for Android

Steps:

1. Install the application
2. Launch the app
3. Complete onboarding
4. Explore modules

---

### For Developers

#### Clone Repository

    git clone https://github.com/Catrobat/mARine.git
    cd mARine

#### Open in Unity

- Install Unity 6000.1.9f1 LTS (or newer)
- Open Unity Hub
- Add project from disk
- Select the mARine folder
- Allow Unity to resolve dependencies

---

## Project Architecture

### Overview

    Assets/
    ├── Scripts/        # Core logic and gameplay systems
    ├── Scenes/         # Main AR environments
    ├── Prefabs/        # Reusable game objects
    ├── Models/         # 3D marine assets
    ├── Materials/      # Rendering materials
    ├── Textures/       # Texture assets
    ├── Shaders/        # Custom shaders
    ├── Plugins/        # External integrations
    ├── XR/             # XR configurations
    └── XRI/            # XR Interaction Toolkit

---

### Detailed Structure

    Assets/
    │
    ├── #NVJOB Boids/             # Boids simulation system
    ├── AddressableAssetsData/    # Unity addressable asset configuration
    ├── Animations/               # Animation assets
    ├── Audio/                    # Sound and audio files
    ├── Custom-create/            # Custom user-created content
    ├── Editor/                   # Unity editor scripts and tools
    ├── Icons/                    # UI icons and sprites
    ├── Input Maps/               # Input system configurations
    ├── Joystick Pack/            # Joystick input assets
    ├── Localization/             # Multi-language support
    ├── Map_Layouts/              # Scene/environment layouts
    ├── Materials/                # Rendering materials
    ├── MediaPipeUnity/           # Pose detection integration
    ├── MobileARTemplateAssets/   # AR template assets
    ├── Models/                   # 3D models and marine assets
    ├── Oculus/                   # Oculus/VR related assets
    ├── Plugins/                  # External plugins
    ├── Post-Processing Profiles/ # Visual effects configurations
    ├── Prefabs/                  # Reusable game objects
    ├── Resources/                # Runtime-loaded assets
    ├── Samples/                  # Sample/demo content
    ├── Scanner/                  # Scanning/AR detection features
    ├── Scenes/                   # Main scenes and environments
    ├── Scripts/                  # Core logic and gameplay scripts
    ├── Settings/                 # Project configuration settings
    ├── Shaders/                  # Custom shaders
    ├── Sketchfab For Unity/      # Sketchfab asset integration
    ├── StreamingAssets/          # Raw assets included in builds
    ├── TextMesh Pro/             # Text rendering system
    ├── Textures/                 # Texture assets
    ├── UIWithFog/                # UI effects with fog rendering
    ├── UMS_LODs/                 # Level of Detail system assets
    ├── XR/                       # XR core configurations
    └── XRI/                      # XR Interaction Toolkit

---

## Dependencies

The project relies on the following external SDKs and plugins:

### Vuforia Engine SDK

AR target recognition and tracking.

- Website: https://developer.vuforia.com/
- Unity Setup Guide: https://developer.vuforia.com/library/vuforia-engine/getting-started/development-environments/getting-started-vuforia-engine-unity/

---

### MediaPipe Unity Plugin (homuler fork)

Human pose detection and gesture interaction.

- GitHub Repository: https://github.com/homuler/MediaPipeUnityPlugin

---

### Installation

1. Open Unity Package Manager or import custom packages
2. Add Vuforia Engine (via Unity or official package)
3. Import MediaPipe Unity Plugin from GitHub
4. Ensure all dependencies are resolved before running the project

---

## Build & Deployment

- Go to File → Build Settings.
- Choose Android or iOS as target platform.
- Switch Platform.
- Make sure ARCore/ARKit is enabled in XR Plug-in Management.
- Connect your ARCore/ARKit compatible device.
- Run as a development build or
  - Configure signing (keystore for Android, provisioning profile for iOS) to bundle.
- Click Build and Run.

---

## Troubleshooting

| Issue          | Solution                     |
| -------------- | ---------------------------- |
| AR not working | Enable XR Plug-in Management |
| Vuforia error  | Add license key              |
| Black screen   | Enable camera permissions    |
| Shader issues  | Use URP pipeline             |

---

## Contributing

Contributions are welcome from educators, developers, marine biologists, and educational technology specialists! Here's how you can help.

### Ways to Contribute

- **Educational Content**: Create curriculum-aligned modules and lesson plans
- **Technical Development**: Implement features, optimize performance, fix bugs
- **Scientific Accuracy**: Validate biological behaviors and ecosystem modeling
- **Accessibility**: Improve platform accessibility for diverse learners
- **Documentation**: Enhance guides and educational resources

### Contribution Process

### Contribution Process

1. Fork the repository
2. Create a new branch:

   ```bash
   git checkout -b feature/your-enhancement
   ```

3. Make your changes and test them
4. Commit your changes:

   ```bash
    git commit -m "Add: short description of your changes"
   ```

5. Push to your fork:

   ```bash
   git push origin feature/your-enhancement
   ```

6. Open a Pull Request with a clear description of your changes
   s.

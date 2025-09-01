<div align="center">
  <h1>Mobile Application for Marine Biology AR Learning</h1>
</div>
<div align="center">
  <h3>Revolutionizing Marine Biology Education Through Augmented Reality</h3>
  
  [![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-green)](https://github.com/organization/AR-Marine-Ecosystem-Simulation)
  [![Unity](https://img.shields.io/badge/Unity-6.0%20LTS-white)](https://unity.com/)
  [![GSoC 2025](https://img.shields.io/badge/GSoC-2025-yellow)](https://summerofcode.withgoogle.com/)
</div>

---

## Table of Contents

- [Project Overview](#project-overview)
  - [Key Accomplishments](#key-accomplishments)
- [Key Features](#key-features)
  - [Immersive AR Environment & Navigation](#immersive-ar-environment--navigation)
  - [Marine Ecosystem Simulation & Behaviors](#marine-ecosystem-simulation--behaviors)
  - [Educational Platform & Learning Systems](#educational-platform--learning-systems)
  - [Technical Architecture & Performance](#technical-architecture--performance)
- [Quick Start](#quick-start)
  - [Prerequisites](#prerequisites)
  - [For Educators & Students](#for-educators--students)
  - [For Developers](#for-developers)
- [Project Structure](#project-structure)
- [Usage Guide](#usage-guide)
  - [For Educators](#for-educators)
  - [For Students](#for-students)
- [Contributing](#contributing)
  - [Ways to Contribute](#ways-to-contribute)
  - [Contribution Process](#contribution-process)
- [Future Goals](#future-goals)
- [License](#license)
- [Acknowledgements](#acknowledgements)
- [Connect With the Vision](#connect-with-the-vision)

---

## Project Overview

The project is aimed at transforming marine biology education through immersive Augmented Reality technology! This comprehensive platform enables educators to create custom underwater learning environments where students explore marine ecosystems, observe ecological interactions, and experiment with environmental variables in real-time.

**The Challenge**: Traditional marine biology education relies on static textbooks that fail to convey the dynamic nature of underwater ecosystems.

**The Solution**: A complete AR platform that converts abstract marine science concepts into tangible interactive experiences - all through the usage of handheld devices.

### Key Accomplishments
- Development of portal-based AR transition system from real-world to underwater environments
- Implementation of depth-level navigation with immersive underwater atmosphere rendering
- Creation of intelligent marine creature behaviors with spline-based movement and environmental awareness
- Integration of MarineBuddy AI guide system with cross-platform text-to-speech capabilities
- Establishment of modular tutorial and goal management architecture for educational onboarding
- Optimization for mid-range smartphone deployment with advanced shader systems and LOD implementation

---

## Key Features

### Immersive AR Environment & Navigation
- **Portal-Based AR Transition**: Seamless transition from real-world plane detection into fully immersive underwater environments through AR portals
- **Depth-Level Navigation System**: Multi-level underwater exploration (surface → mid-depth → ocean floor) with intuitive floating action button/slider controls
- **Advanced Shader Systems**: URP + Shader Graph based water/fog shaders with Fresnel edge highlighting, distance-based gradient effects, and full-screen underwater post-processing overlays
- **Environment Zone Separation**: Fog-like shader walls creating shimmering water dividers between different biome areas for enhanced spatial navigation

### Marine Ecosystem Simulation & Behaviors
- **Realistic Marine Life Simulation**: Coral reef and sandbed biomes featuring diverse marine species with authentic behavioral patterns
- **Advanced Octopus Character System**: Spline-based lane movement with interactive behaviors including forward/backward movement, lane switching, environment-aware camouflage near coral reefs, and ink-spewing defense mechanisms
- **Dynamic Predator-Prey Interactions**: Scripted shark sequences with circular patrol movements and intelligent interaction triggers that demonstrate natural marine ecosystem dynamics
- **Environmental Awareness**: Creatures respond to environmental changes and proximity with realistic behavioral adaptations

### Educational Platform & Learning Systems
- **MarineBuddy AI Guide System**: Intelligent in-app tutorial companion using CrossPlatformTTS for voice instructions, providing onboarding guidance, UI highlights, path-following assistance, and contextual activity hints
- **Comprehensive Learning Framework**: Modular quiz system supporting MCQs and true/false questions, marine species identification minigames, and interactive audio learning through creature interaction
- **Free Explorer Mode**: Open-world marine environment navigation with depth control, guided tutorials, reward systems, and progression tracking for self-paced learning
- **Goal-Based Tutorial Architecture**: Event-driven onboarding system with modular scriptable tutorial goals and button hints for interactive educational guidance

### Technical Architecture & Performance
- **Architecture of Marine Ecosystem Simulation**: Robust [system architecture](https://drive.google.com/file/d/1glYFZJKHec0oC1cp5jGxuk-OjReJePY4/view?usp=sharing) enabling real-time simulation of marine ecosystems, designed to replace static educational tools and help students better understand complex ecological processes through immersive AR experiences
- **Optimized Mobile Performance**: LOD (Level of Detail) systems, proximity-based activation, shader-based occlusion optimization, and addressable asset loading designed specifically for mid-range smartphones
- **Modular AR Framework**: Marine AR Module Builder with grid-based actor placement, JSON-based behavior scripting, and lightweight AR module packaging for easy content distribution
- **Cross-System Shared Services**: Unified text-to-speech, integrated quiz management, creature identification systems, and dynamic layer switching with memory-efficient streaming
- **Scalable Backend Integration**: Planned Golang Gin backend with PostgreSQL storage for AI-generated 3D models, school databases, student tracking, and microservices architecture

---

## Quick Start

### Prerequisites

**Hardware Requirements:**
- AR-compatible mobile device (With ARCore/ARKit support)
- Minimum 4GB RAM
- 2GB available storage space

**For Development:**
- Unity 6.0 LTS or higher recommended
- AR Foundation
- URP (Universal Render Pipeline)
- Git

### For Educators & Students

- **Hardware Requirements:**
   - AR-compatible mobile device (With ARCore/ARKit support)
   - Minimum 4GB RAM
   - 2GB available storage space

1. **Download the App**
   - [MarineBiology_AR](https://drive.google.com/file/d/1o4Zu4FMbyJZXPOKiPRe0a28uvSHgDGBP/view?usp=drive_link) for Android

2. **Get Started**
   - Download the application
   - Launch the application and scan a flat surface to create AR portal
   - Follow MarineBuddy's guided tutorial
   - Navigate through different underwater depth levels

### For Developers

- **Hardware Requirements:**
   - RTX 2050 or higher
   - Minimum 8GB RAM
   - 25GB available storage space

1. **Clone Repository**
   ```bash
   git clone https://github.com/Catrobat/mARine.git
   cd mARine
   ```

2. **Open in Unity**
   ```bash
   # Editor and recommended version
   Install Unity 6.0 LTS (or newer).
   
   # Open the project from Unity Hub
   From Unity Hub, click Add Project and select the mARine folder.

   # Unity will automatically handle packages and dependencies
   Let Unity resolve and import dependencies.
   ```

3. **Dependencies**
   ```bash
   # Add AR Foundation for AR capabilities
   AR Foundation SDK – AR plane detection & tracking.

   # Add Universal Render Pipeline for advanced shaders
   URP Package for enhanced underwater rendering.

   # Add CrossPlatformTTS for MarineBuddy voice guidance
   CrossPlatformTTS Plugin for text-to-speech functionality.

   # Add all packages in Unity
   Ensure all are installed via Unity's Package Manager or as custom packages.
   ```

4. **Build Settings**
   - Go to File → Build Settings.
   - Choose Android or iOS as target platform.
   - Switch Platform.
   - Make sure ARCore/ARKit is enabled in XR Plug-in Management.


5. **Deploy to Device**
   - Connect your ARCore/ARKit compatible device.
   - Run as a development build or
      - Configure signing (keystore for Android, provisioning profile for iOS) to bundle.
   - Click Build and Run.

---

## Project Structure

```
App/
├── Assets/
│   ├── core/                      # System architecture and framework
│   │   ├── ar-portal/             # AR transition and portal systems
│   │   ├── free-explorer/         # Free navigation marine ecosystem simulation
│   │   ├── tutorial-system/       # Goal-based tutorial and onboarding
│   │   └── marine-buddy/          # AI guide system with TTS integration
│   ├── marine-life/               # 3D creatures, spline behaviors, interactions
│   ├── environments/              # Depth zones, coral reefs, shader walls
│   ├── simulation/                # Ecosystem dynamics, creature behaviors
│   ├── shaders/                   # URP underwater effects, fog walls, post-processing
│   ├── learning/                  # Quiz systems, species identification
│   └── services/                  # Cross-platform TTS, shared services
├── mobile/
│   ├── android-arcore/            # Android application
│   └── ios-arkit/                 # iOS application
├── assets/                        # 3D models, textures, audio, scripts
└── tests/                         # Testing suites
```

---

## Usage Guide

### For Educators

1. **Create Learning Modules**
   - Access Free Explorer through mobile application
   - Use Marine AR Module Builder for custom content creation
   - Configure marine creatures and environmental parameters
   - Generate modules for student distribution

2. **Classroom Implementation**
   - Share module content with students
   - Monitor progress through MarineBuddy tutorial system
   - Facilitate discussions based on student ecosystem discoveries

### For Students

1. **Access AR Environment**
   - Launch app and scan flat surface for AR portal
   - Enter underwater world through portal transition
   - Follow MarineBuddy's interactive tutorial

2. **Explore & Learn**
   - Navigate different depth levels with slider controls
   - Observe octopus behaviors and predator-prey interactions
   - Complete species identification challenges
   - Use audio learning through creature interactions

---

## Contributing

We welcome contributions from educators, developers, marine biologists, and educational technology specialists!

### Ways to Contribute
- **Educational Content**: Create curriculum-aligned modules and lesson plans
- **Technical Development**: Implement features, optimize performance, fix bugs
- **Scientific Accuracy**: Validate biological behaviors and ecosystem modeling
- **Accessibility**: Improve platform accessibility for diverse learners
- **Documentation**: Enhance guides and educational resources

### Contribution Process

1. **Fork & Clone**
   ```bash
   git clone https://github.com/Catrobat/mARine.git
   ```

2. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-enhancement
   ```

3. **Develop & Test**

4. **Submit Pull Request**
   - Include detailed description
   - Add educational rationale and technical notes

---

## Future Goals

### Short-term (3-6 months)
- **Advanced Curriculum Integration**: NGSS-aligned lesson plans
- **Enhanced AI Backend**: Golang Gin backend with AI-generated 3D model integration
- **Professional Development**: Educator training and certification

### Long-term (6-12 months)
- **Real-world Data Integration**: Live oceanographic monitoring
- **Advanced Ecosystem Modeling**: Climate change scenarios
- **Microservices Architecture**: Complete backend with PostgreSQL for institutional deployment

---

## License

This project is licensed under the GNU Affero General Public License v3.0 - see the [LICENSE](https://www.gnu.org/licenses/agpl-3.0.en.html) file for details.

**Educational Use:**
- Free for all educational institutions and non-profit educational organizations
- Copyleft license ensuring derivative works remain open source
- Full source code access for educational customization and transparency
- Commercial use permitted under AGPL terms with source disclosure requirements

---

## Acknowledgements
- **Wolfgang Slany** & **Patrick Ratschiller** from International Catrobat Organization for their guidance
- **Aryavardhan Sharma**, **Krishan Mohan Patel** & **Himanshu Kumar** for being with me throughout the journey
- **Google Summer of Code 2025** & **Catrobat** for the opportunity
- **Prabhakar Joshi** & **Dhruvanshu Joshi**, my fellow contributors, for the company
- **Educational Partners** for testing and validation
- **Open Source Forums and Communities**
- **Marine Biology Experts** for scientific validation
- **Accessibility Advocates** for inclusive design guidance

---

## Connect With the Vision

- **Project Repository**: [GitHub](https://github.com/Catrobat/mARine.git)
- **Educational Resources**: [Cephalopod Behaviour](https://www.cambridge.org/core/books/cephalopod-behaviour/2D21474D460811C160EFDBA35796FAC0)
- **Contact**: [catrobat.org](https://catrobat.org/)
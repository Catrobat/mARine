<div align="center">
  <h1>AR Based Interactive Marine Ecosystem Simulation</h1>
</div>
<div align="center">
  <h3>Revolutionizing Marine Biology Education Through Augmented Reality</h3>
  
  [![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-green)](https://github.com/organization/AR-Marine-Ecosystem-Simulation)
  [![Unity](https://img.shields.io/badge/Unity-6000.1.9f1%20LTS-white)](https://unity.com/)
  [![GSoC 2025](https://img.shields.io/badge/GSoC-2025-yellow)](https://summerofcode.withgoogle.com/)
</div>

---

## Table of Contents

- [Project Overview](#project-overview)
  - [Key Accomplishments](#key-accomplishments)
- [Key Features](#key-features)
  - [System Architecture & Simulation Engine](#system-architecture--simulation-engine)
  - [Educational Platform & Content Creation](#educational-platform--content-creation)
  - [AR Technology & User Interaction](#ar-technology--user-interaction)
  - [Content Distribution & Infrastructure](#content-distribution--infrastructure)
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
- Building of a complete modular AR framework with 10+ integrated systems
- Development of a intuitive Module Builder for non-technical educators
- Implementation of realistic marine ecosystems simulation with environmental controls
- Creation of cross-platform mobile apps deployed on Android app stores
- Establishment of QR-based sharing system for instant classroom distribution
- Integration of human pose detection for natural marine environment interaction

---

## Key Features

### System Architecture & Simulation Engine
- **Architecture of Marine Ecosystem Simulation**: Robust [system architecture](https://drive.google.com/file/d/1glYFZJKHec0oC1cp5jGxuk-OjReJePY4/view?usp=sharing) enabling real-time simulation of marine ecosystems, designed to replace static educational tools and help students better understand complex ecological processes through immersive AR experiences
- **Realistic Marine Life Simulation**: Authentic 3D models with natural swimming behaviors, predator-prey interactions, and species-specific characteristics that respond to environmental changes
  - **Shark 3D Model Integration**: High-fidelity shark models with realistic swim animations and behavioral patterns
- **Environmental Control Interface**: Real-time sliders for temperature, pH, and pollution levels with immediate visual feedback showing effects on coral health and ecosystem dynamics
  - **Temperature & pH Simulation**: Interactive sliders controlling underwater environmental parameters with real-time effects on coral health and marine ecosystem dynamics
- **Aquatic Portal System**: Central hub architecture facilitating seamless navigation between different marine learning environments

### Educational Platform & Content Creation
- **Comprehensive Module Builder System**: Complete toolkit along with [segmented architecture](https://drive.google.com/file/d/1_RJSdJxfAGf_p7WLWIxGBmlgDsT6oBla/view?usp=sharing) allowing educators to design custom marine learning scenarios through intuitive drag-and-drop interfaces without requiring technical expertise
  - **Environment Integration**: Dynamic system for adding and configuring diverse underwater environments (coral reefs, deep ocean, kelp forests)
  - **Actor Placement System**: Intuitive interface for positioning marine life actors within educational scenarios
  - **Behavior Assignment Tools**: Comprehensive system for assigning realistic behaviors to placed marine actors
  - **Script Integration Framework**: Advanced scripting capabilities allowing educators to create complex interactive learning scenarios
  - **Module Collectivization**: End-to-end system that packages environments, actors, behaviors, and scripts into cohesive educational modules
- **Collaborative Educational Tools**: Module saving, loading, and sharing capabilities that enable educators to build upon each other's work and create institutional lesson libraries
- **Educational Integration**: Application architecture specially designed for K-12 integration with existing learning management systems

### AR Technology & User Interaction
- **Cross-Platform AR Integration**: Native mobile applications for Android with ARCore compatibility, ensuring broad accessibility across educational institutions
- **Human Interaction Controls**: MediaPipe-based pose detection enabling natural gesture interactions with AR marine environments and creatures
  - **Real-time Pose Detection**: MediaPipe-powered human pose recognition for natural interaction with AR marine life
  - **Live Marine Interaction**: Real-time responsive interactions between users and AR-based marine creatures through gesture recognition
- **Immersive Environmental Effects**: Realistic underwater atmosphere including volumetric water rendering, dynamic god rays, and accurate caustic light patterns
  - **Underwater Volume Rendering**: Advanced volumetric water effects creating authentic underwater atmosphere simulation
  - **Dynamic God Rays**: Realistic light ray penetration effects enhancing underwater environment immersion
  - **Caustic Light Patterns**: Authentic underwater caustic lighting effects for enhanced environmental realism

### Content Distribution & Infrastructure
- **QR Code Sharing System**: Instant module distribution allowing educators to share custom-created lessons with student devices through a user-friendly QR code scanning
  - **Cross-App QR Integration**: Seamless QR code generation and scanning functionality enabling instant module sharing across devices
- **Data Management Infrastructure**: Robust database architecture supporting real-time state synchronization and scalable institutional deployment
  - **PlayerPrefs Data Management**: Complete PlayerPrefs implementation ensuring end-to-end app-state connectivity and persistence across application sessions
  - **Module Persistence System**: Advanced saving and loading capabilities for entire educational modules with full state preservation

---

## Quick Start

### Prerequisites

**Hardware Requirements:**
- AR-compatible mobile device (With ARCore/ARKit support)
- Minimum 4GB RAM
- 2GB available storage space

**For Development:**
- Unity 6000.1.9f1 LTS or higher recommended
- MediaPipe Plugin by Homuler
- Vuforia SDK
- Git

### For Educators & Students

- **Hardware Requirements:**
   - AR-compatible mobile device (With ARCore/ARKit support)
   - Minimum 4GB RAM
   - 2GB available storage space

1. **Download the App**
   - [MarineBiology_AR](https://drive.google.com/file/d/1o4Zu4FMbyJZXPOKiPRe0a28uvSHgDGBP/view?usp=sharing) for Android

2. **Get Started**
   - Download the application
   - Launch the application
   - Complete interactive tutorials
   - Navigate through different modules of the application

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
   Install Unity 6000.1.9f1 (or newer).
   
   # Open the project from Unity Hub
   From Unity Hub, click Add Project and select the mARine folder.

   # Unity will automatically handle packages and dependencies
   Let Unity resolve and import dependencies.
   ```

3. **Dependencies**
   ```bash
   # Add Vuforia Software Development Kit
   Vuforia Engine SDK – AR target recognition & tracking.

   # Add human pose detection with MediaPipe Unity Plugin
   MediaPipe Unity Plugin (homuler fork) for pose detection.

   # Add both packages in Unity
   Ensure both are installed via Unity's Package Manager or as custom packages.
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
│   │   ├── ar-spawner/
│   │   ├── free-explorer/         # Open world marine ecosystem simulation
│   │   ├── custom-create/         # Module creation and spawing toolkit
│   │   └── human-interaction/     # Central access hub
│   ├── marine-life/               # 3D creatures, behaviors, interactions
│   ├── environments/              # Coral reefs, deep ocean, kelp forests
│   ├── simulation/                # Physics, environmental controls
│   ├── interaction/               # Human Interaction using pose detection
│   ├── database/                  # Persistence acrdss app using playerprefs
│   └── sharing/                   # QR codes generating and scanning for content distribution
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
   - Use drag-and-drop Module Builder
   - Configure marine creatures and environmental parameters
   - Generate QR codes for student distribution

2. **Classroom Implementation**
   - Share module QR codes with students
   - Monitor progress through analytics dashboard
   - Facilitate discussions based on student discoveries

### For Students

1. **Access Modules**
   - Scan educator's QR code
   - Complete AR environment setup
   - Follow interactive tutorial

2. **Explore & Learn**
   - Use natural gestures to interact with marine life
   - Manipulate environmental controls
   - Observe ecosystem responses in real-time
   - Complete embedded assessments

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
- **Collaborative Learning**: Multi-student shared AR environments
- **Professional Development**: Educator training and certification

### Long-term (6-12 months)
- **Real-world Data Integration**: Live oceanographic monitoring
- **Advanced Ecosystem Modeling**: Climate change scenarios
- **Geofencing**: App-state freeze if device goes outside the geofenced area for added security

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

- **Wolfgang Slany** & **Patrick Ratschiller** for their guidance
- **Krishan Mohan Patel** & **Himanshu Kumar** for being with me throughout the journey
- **Google Summer of Code 2025** & **International Catrobat Association** for the opportunity
- **Ashwani Kumar Moudgil** & **Dhruvanshu Joshi**, my fellow contributors, for the company
- **Educational Partners** for testing and validation
- **Open Source Forums and Communities**
- **Marine Biology Experts** for scientific validation
- **Accessibility Advocates** for inclusive design guidance

---

## Connect With the Vision

- **Project Repository**: [GitHub](https://github.com/Catrobat/mARine.git)
- **Educational Resources**: [Cephalopod Behaviour](https://www.cambridge.org/core/books/cephalopod-behaviour/2D21474D460811C160EFDBA35796FAC0)
- **Contact**: [catrobat.org](https://catrobat.org/)

# mARine – Marine Biology AR App

<div align="center">
  
  [![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
  [![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-green)](https://github.com/Catrobat/mARine)
  [![Unity](https://img.shields.io/badge/Unity-6.0%20LTS-white)](https://unity.com/)
  [![GSoC 2025](https://img.shields.io/badge/GSoC-2025-yellow)](https://summerofcode.withgoogle.com/)
</div>

Revolutionizing marine biology education through immersive AR! mARine transforms abstract marine science concepts into tangible interactive experiences, enabling educators to create custom underwater learning environments where students explore marine ecosystems and experiment with environmental variables in real-time.

---

## Table of Contents

- [Features](#features)  
- [Screenshots](#screenshots)  
- [Getting Started](#getting-started)  
  - [Prerequisites](#prerequisites)  
  - [Installation](#installation)   
- [Contributing](#contributing)  
- [License](#license)  

---

## Features

- **Complete AR Framework**: Modular system with 15+ integrated components for marine ecosystem simulation
- **Intuitive Module Builder**: Drag-and-drop interface for educators to create custom learning scenarios without technical expertise
- **Realistic Marine Life Simulation**: Authentic 3D models with natural swimming behaviors, predator-prey interactions, and species-specific characteristics
- **Environmental Control Interface**: Real-time sliders for temperature, pH, and pollution levels with immediate visual feedback
- **Cross-platform compatibility**: Unity-based, supporting Android with ARCore and iOS with ARKit
- **Human Pose Detection**: MediaPipe-based natural gesture interactions with AR marine environments
- **QR Code Sharing System**: Instant module distribution for classroom deployment
- **Immersive Effects**: Realistic underwater atmosphere with volumetric water rendering and dynamic lighting

---

## Screenshots

<div align="center">
  <table>
    <tr>
      <td align="center">
        <img width="300" alt="mARine Main Interface" src="https://github.com/user-attachments/assets/96405860-0e60-4d72-ade8-4eaaed1c436d" />
        <br>
        <em>Marine Ecosystem Simulation</em>
      </td>
      <td align="center">
        <img width="300" alt="mARine AR Experience" src="https://github.com/user-attachments/assets/8878ea37-c53a-4eb9-95ab-ba5ffdd57f22" />
        <br>
        <em>Octo-Shark Activity</em>
      </td>
    </tr>
  </table>
</div>

---

## Getting Started

### Prerequisites

### For Educators & Students

- **Hardware Requirements:**
   - AR-compatible mobile device (With ARCore/ARKit support)
   - Minimum 4GB RAM
   - 2GB available storage space

### For Developers

- **Hardware Requirements:**
   - RTX 2050 or higher
   - Minimum 8GB RAM
   - 25GB available storage space

### Installation

### For Educators & Students

1. **Download the App**
   - [MarineBiology_AR](https://drive.google.com/file/d/1o4Zu4FMbyJZXPOKiPRe0a28uvSHgDGBP/view?usp=sharing) for Android

2. **Get Started**
   - Download the application
   - Launch the application
   - Complete interactive tutorials
   - Navigate through different modules of the application

### For Developers

1. **Clone Repository**
   ```bash
   git clone https://github.com/Catrobat/mARine.git
   cd mARine
   ```

2. **Open in Unity**
   ```text
   # Editor and recommended version
   Install Unity 6.0LTS (or newer).
   
   # Open the project from Unity Hub
   From Unity Hub, click Add Project and select the mARine folder.

   # Unity will automatically handle packages and dependencies
   Let Unity resolve and import dependencies.
   ```

3. **Dependencies**
   ```text
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

---

## License

This project is licensed under the **[GNU Affero General Public License v3.0](LICENSE)**.

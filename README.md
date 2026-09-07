# MozXR Unity Framework

Unity Package that provides an interface to the MozXR Unreal Cluster.

## Overview

This is a Unity Package that provides an interface to the Unreal Engine based MozXR Cluster Renderer.

It provides automatic scene synchronization for cluster setups, a compatible input system, and an interface for tracking systems (e.g. OptiTrack or Pharus).

Stereo rendering is based on (https://github.com/Vital-Volkov/Stereoscopic-3D-system-for-Unity-2019-) by [vital-Volkov](https://github.com/Vital-Volkov). A change to allow for off-axis projection was implemented.

## Requirements

- **Unity Engine**: 6000.3 or higher
- **Platform**: Windows 64-bit

## Project Structure

```
MoxFramework/
└── Runtime/
    ├── Editor/                 # MozXR Editor extension (simulator, tracking, config)
	├── Materials/              # Materials for NDisplay cluster visualization
	├── Plugins/                # FL IPC (inter process communication) Library binaries
    ├── Prefabs/                # Pre-prepared utility Gameobjects (Sync Management, Spout, etc.)
    ├── Scripts/                # C# scripts
	│   ├── NDisplay/           # NDisplay config parser and camera handler
	│   ├── Simulator/          # Unreal interface for Cluster simulation
	│   ├── Spout/              # Spout setup scripts
	│   ├── SyncManagement/     # Synchronization scripts (Scene, input, audio, tracking)
	│	└── Utility/            # Utility scripts
	└── Textures/               # Textures for NDisplay cluster visualization
```

## Default Ports

- **Scene Sync**: 7779
- **Input Sync**: 7780
- **Tracking Sync**: 7781
- **Spout Sync**: 7782
- **Simulator Config**: 7783
- **Simulator Mode**: 7784
- **Tracking Config**: 7785
- **L-ISA Commands**: 7786 (for one-shot commands)
- **Ableton Commands**: 7787 (for one-shot commands)
- **Audio Sync**: 7788

## Default Pipe Names

### Scene Sync
- **unityPipe** (Unreal Server pipe)
- **unityPipe2** (Unity Server pipe)

### Input Sync
- **unityInputPipe** (Unity Server pipe)

### Audio Sync
- **unityAudioPipe** (Unreal Server pipe)

### Tracking Sync
- **unityTrackingPipe** (Unity Server pipe)

## License

mozXR Attribution License (mozXR-AL) v1.0
Based on the MIT License

Copyright (c) Mozarteum University Salzburg – X-Reality Lab

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1. The above copyright notice and this permission notice shall be included in
   all copies or substantial portions of the Software.

2. Attribution Requirement:
   Any project, product, or documentation that uses the Software or substantial
   portions of it must include the following credit in a reasonable manner:
   "Based on mozXR by Mozarteum University Salzburg – X-Reality Lab and 
    Ars Electronica Futurelab"
   This credit must appear in:
   - Project documentation (e.g., README, manuals)
   - Public-facing credits (e.g., About page, project credits)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

SPDX-License-Identifier: mozXR-AL-1.0

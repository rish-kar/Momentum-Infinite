# Momentum - Infinite

A **Unity 6 endless runner** that combines **procedural terrain generation**, **Wave Function Collapse-inspired object placement**, and **real-time path validation** using autonomous agents.

Built as an **MSc Artificial Intelligence thesis project at King's College London**, Momentum - Infinite is not just another runner prototype. It is a practical research and engineering project focused on a harder question:

> **How do you keep procedurally generated gameplay fresh without letting the generator create broken, unfair, or impossible paths?**

This repository answers that by pairing an endless runner with an active validation pipeline. Terrain is generated at runtime, obstacles are placed procedurally, evaluation agents scan upcoming tiles before the player reaches them, blocked corridors are reported in detail, and the system can export structured failure reports for later debugging and analysis.

That combination of **generation + validation + reporting** is the core identity of the project.

---

## Table of Contents

- [Overview](#overview)
- [Why This Project Matters](#why-this-project-matters)
- [Feature Highlights](#feature-highlights)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
- [Controls](#controls)
- [How It Works](#how-it-works)
- [System Architecture](#system-architecture)
- [Repository Map](#repository-map)
- [Technical Stack](#technical-stack)
- [Research Context](#research-context)
- [Current Limitations](#current-limitations)
- [Future Improvements](#future-improvements)
- [Author](#author)

---

## Overview

Momentum - Infinite is a 3D infinite runner where the environment continues to extend as the player moves forward. Instead of shipping a fixed handcrafted level, the game generates terrain during runtime, populates it with procedural objects, and evaluates whether the generated space remains traversable.

From a gameplay perspective, the idea is simple: keep moving, avoid obstacles, and survive as long as possible.

From a technical perspective, the project is more interesting. Every newly created region can be checked by dedicated evaluation agents. If the procedural system creates a blocked or unsafe path, the project can capture the failure context and turn it into a structured report instead of leaving it as an unexplained gameplay bug.

This makes the repository useful in three ways:

- as a playable Unity prototype,
- as a procedural content generation experiment,
- and as a debugging-oriented research system for runtime validation.

---

## Why This Project Matters

Procedural generation is excellent for replayability, but it comes with a real engineering risk: **randomness can create bad content**.

In an endless runner, bad content is not just visually awkward. It can mean:

- an obstacle layout that fully blocks the player,
- unfair spacing that removes reaction time,
- collider overlap that creates misleading traversal space,
- or a technically unsolvable section generated on the fly.

Most procedural projects stop at content creation.

Momentum - Infinite goes further by asking whether the content is still playable while the game is running. That is the part that makes this project stand out for recruiters, researchers, and gameplay programmers. It is not only generating content; it is also trying to **verify, explain, and debug** it.

---

## Feature Highlights

### Core gameplay systems

- Infinite terrain spawning during gameplay
- Forward-running endless runner loop with side movement and jumping
- Score progression linked to forward travel distance
- Runtime-adjustable game parameters through UI controls

### Procedural generation systems

- Real-time terrain generation using dynamic tile spawning
- One-dimensional Wave Function Collapse-inspired object placement
- Procedural obstacle distribution designed for variation without total chaos
- Tile cleanup behind the player to prevent scene overload

### Validation and debugging systems

- Flying agent that scans future terrain before the player arrives
- Ghost runner agent built on Unity NavMesh for grounded traversal checks
- Dense raycasting across corridor width to verify passable segments
- `Physics.OverlapBox` checks to catch irregular collider-based blockages
- Automatic blockage identification with detailed report generation
- Cross-scene crash caching and in-project report display
- PDF export support for debugging and research evidence

---

## Screenshots

### Current repository screenshots

![Basic Scene Editor](Screenshots/SceneEditor.png)

*Unity editor basic scene view showing layout, environment setup, and development context.*

![Complex Scene Editor](Screenshots/Scene%20View.png)

*Unity editor complex scene view showing layout, environment setup, and development context.*

![Project Phase 1 - Game View](Screenshots/GameView.png)

*In-game play view (starting of the project) showing the active runner environment.*

![Project Phase 4 - Game View](Screenshots/Game$20View.png)

*In-game play view (ending of the project) showing the active runner environment.*

![Physics Editor View](Screenshots/Player%20Physics.png)

*Player physics during the editing environment.*

![Running Editor View](Screenshots/Player%20Running.png)

*Player runner physics during the editing environment.*

![Wave Funciton Collapse - 1](Screenshots/Wave%20Function%20Collapse1.png)

*Wave Function Collapse - 1*

![Wave Funciton Collapse - 2](Screenshots/Wave%20Function%20Collapse2.png)

*Wave Function Collapse - 2*

![Particle System](Screenshots/Particle%20System.png)

*Particle System*

![In-game Crash Report](Screenshots/Crash%20Report%20Ingame.png)

*In-game blocker detector*

![PDF Crash Report](Screenshots/Crash%20Report%20PDF.png)

*PDF blocker detector*

![Overlapping Mesh](Screenshots/Overlapping%20Mesh.png)

*Overlapping Mesh Problem for Evaluation Agent*


---

## Getting Started

### Requirements

- **Unity 6**
- Recommended editor version: **6000.2.0b6**
- Git

### Clone the repository

```bash
git clone https://github.com/rish-kar/Momentum-Infinite.git
cd Momentum-Infinite
```

### Open the project in Unity

1. Open **Unity Hub**.
2. Select **Add project from disk**.
3. Choose the cloned `Momentum-Infinite` folder.
4. Open the project using **Unity 6 / 6000.2.0b6**.
5. Let Unity restore packages from the project manifest.

### Main scenes

The repository currently includes these key scenes:

- `Assets/Scenes/Intro Scene.unity`
- `Assets/Scenes/Ground Level.unity`
- `Assets/Scenes/Exit Report.unity`

### Fastest way to run the project

1. Open `Assets/Scenes/Ground Level.unity`
2. Press **Play** in the Unity Editor

### Recommended exploration flow

1. Open `Intro Scene.unity`
2. Move into the gameplay scene
3. Test procedural generation and runtime controls
4. Inspect `Exit Report.unity` to review the failure-reporting pipeline

---

## Controls

- **W** - Move forward
- **A** - Move left
- **D** - Move right
- **Space** - Jump

The intentionally simple control scheme keeps focus on procedural content, spacing, traversal, and obstacle response rather than on complex input mechanics.

---

## How It Works

At a high level, the gameplay loop looks like this:

1. The player runs forward through the active environment.
2. The terrain system decides when a new ground tile should be spawned.
3. A fresh tile is instantiated ahead of the player.
4. The procedural spawner places objects on the new tile using a constrained rule-based approach inspired by one-dimensional Wave Function Collapse.
5. The flying agent scans ahead and checks the corridor for blockage risk.
6. The ghost runner agent uses NavMesh-guided traversal logic to validate movement space from a grounded perspective.
7. If the system detects an unsolvable path, it records the incident with structured details.
8. The report can be displayed in-project and exported to PDF for later analysis.

This means the generator is not left unchecked. The project continuously creates content and continuously inspects it.

---

## System Architecture

### 1. Player movement

The player movement system is responsible for forward momentum, side movement, jumping, camera support, and gameplay responsiveness. Distance travelled forms the basis of score progression, which makes movement directly tied to performance.

Relevant files:

- `Assets/Primary Library/Scripts/Character Physics/PlayerMovement.cs`
- `Assets/Primary Library/Scripts/Character Physics/UnifiedCameraController.cs`
- `Assets/Primary Library/Scripts/Character Physics/FXController.cs`

### 2. Procedural terrain generation

The terrain system extends the world incrementally rather than loading a full level in advance. According to the thesis, tile generation is driven by runtime and proximity conditions so that the world remains continuous while avoiding unnecessary overhead.

Older tiles can be destroyed behind the player, helping keep the scene manageable during long runs.

Relevant files:

- `Assets/Primary Library/Scripts/Environment/ProceduralTerrain.cs`
- `Assets/Primary Library/Scripts/Environment/DestroyProceduralTerrain.cs`

### 3. Procedural object placement

Instead of using unconstrained randomness, Momentum - Infinite applies a **one-dimensional WFC-style spawning strategy** for environmental objects. This is an important design decision because pure randomness easily produces nonsense layouts. A constrained procedural strategy preserves unpredictability while keeping local placement patterns more believable.

Relevant file:

- `Assets/Primary Library/Scripts/Environment/EnvironmentObjectSpawner.cs`

### 4. Flying agent evaluation

The flying agent acts as an early-warning system. It travels ahead of the player and scans upcoming space before the player gets there. The thesis describes a corridor-based scan that uses dense ray probes across tile width, supported by overlap-box checks to catch irregular geometry.

If configured, the evaluator can identify blocking objects before they become active failures for the player.

Relevant files:

- `Assets/Primary Library/Scripts/Agents/FlyerAgent.cs`
- `Assets/Primary Library/Scripts/Agents/FlyerCorridorScanner.cs`

### 5. Ghost runner validation

The second evaluator is a **ground-based runner agent** that uses Unity's NavMesh to validate pathing from a traversal perspective. This matters because aerial scanning alone does not fully represent grounded movement behaviour. The thesis also describes recovery logic when the agent becomes stuck at tile transitions.

Relevant file:

- `Assets/Primary Library/Scripts/Agents/GhostRunnerAgent.cs`

### 6. Report generation and export

When a blockage is found, the project records detailed context rather than only printing a vague console log. That report can include scene information, player state, tile context, scan settings, and obstacle details. The result is cached for later viewing and can be exported as a PDF.

Relevant files:

- `Assets/Primary Library/Scripts/Evaluation/BlockageDetailDTO.cs`
- `Assets/Primary Library/Scripts/Evaluation/BlockageReporter.cs`
- `Assets/Primary Library/Scripts/Evaluation/CrashCache.cs`
- `Assets/Primary Library/Scripts/Evaluation/BlockageReportDisplay.cs`
- `Assets/Primary Library/Scripts/Evaluation/PdfExporter.cs`

### 7. Runtime UI and tuning

The project includes runtime sliders for forward speed, side speed, and spawn-related parameters. This makes the prototype useful not only as a game, but also as an experimentation platform where behaviour can be tuned during play.

Relevant files:

- `Assets/Primary Library/Scripts/UI/GameManager.cs`
- `Assets/Primary Library/Scripts/UI/PlayerScore.cs`
- `Assets/Primary Library/Scripts/UI/PlayerForwardSpeedSlider.cs`
- `Assets/Primary Library/Scripts/UI/PlayerSideSpeedSlider.cs`
- `Assets/Primary Library/Scripts/UI/SpawnSlider.cs`
- `Assets/Primary Library/Scripts/UI/ExitButtonHandler.cs`
- `Assets/Primary Library/Scripts/UI/ExitGame.cs`
- `Assets/Primary Library/Scripts/UI/IntroductionSceneVideoPlayer.cs`

---

## Repository Map

This is the most useful starting point for developers opening the project for the first time.

```text
Momentum-Infinite/
├── Assets/
│   ├── Scenes/
│   │   ├── Intro Scene.unity
│   │   ├── Ground Level.unity
│   │   └── Exit Report.unity
│   │
│   ├── Primary Library/
│   │   ├── Animations/
│   │   ├── Documentation/
│   │   ├── Images/
│   │   ├── Materials/
│   │   ├── Models/
│   │   ├── Physics/
│   │   ├── StreamingAssets/
│   │   ├── Textures/
│   │   └── Scripts/
│   │       ├── Agents/
│   │       ├── Character Physics/
│   │       ├── Environment/
│   │       ├── Evaluation/
│   │       └── UI/
│   │
│   ├── Resources/
│   ├── Fonts/
│   ├── Plugins/
│   ├── Extra Files/
│   └── Settings/
│
├── Packages/
├── ProjectSettings/
├── Screenshots/
└── README.md
```

### Where to start reading the code

If you only want the most important logic, start here:

- **Generation logic:** `Assets/Primary Library/Scripts/Environment/`
- **Validation agents:** `Assets/Primary Library/Scripts/Agents/`
- **Crash/report pipeline:** `Assets/Primary Library/Scripts/Evaluation/`
- **Gameplay movement:** `Assets/Primary Library/Scripts/Character Physics/`
- **Runtime controls and score:** `Assets/Primary Library/Scripts/UI/`

---

## Technical Stack

### Engine and language

- **Unity 6**
- **C#**

### Package highlights from the repository manifest

- `com.unity.ai.assistant`
- `com.unity.ai.inference`
- `com.unity.ai.navigation`
- `com.unity.timeline`
- `com.unity.ugui`
- `org.khronos.unitygltf`

### Core technical ideas used by the project

- Procedural Content Generation (PCG)
- One-dimensional Wave Function Collapse-inspired spawning
- Runtime path validation
- NavMesh-based traversal agents
- Dense raycasting across corridor width
- `Physics.OverlapBox` for volumetric obstacle checks
- Structured blockage reporting
- PDF export workflow

---

## Research Context

Momentum - Infinite was developed as an **MSc Artificial Intelligence thesis project at King's College London**. The thesis frames the project around two central goals:

1. create endless content dynamically while the game is running,
2. validate that content in real time so that generated paths remain technically sound and solvable.

The thesis also describes why a one-dimensional WFC variant is a good fit for lane-based object placement, why runtime validation is needed in procedural systems, and how agent-based evaluation can help identify failures that traditional scripted level design might avoid through manual authoring.

In other words, this repository is not only trying to be playable. It is also trying to contribute to a more robust approach to **procedural generation with accountability**.

---

## Current Limitations

This is a strong thesis prototype, but it is still a prototype.

Current constraints include:

- the procedural logic is tailored to an endless-runner corridor rather than a full open-world generator,
- the README can point to system areas, but deeper API-level class documentation would still help contributors,
- public presentation would benefit significantly from GIFs and architecture diagrams,
- fairness evaluation is centered on passability and blockage detection rather than full human difficulty modelling,
- some public-facing polish is still secondary to the technical and research goals of the project.

---

## Future Improvements

Natural next steps for the project include:

- richer biome or environment transitions,
- more advanced obstacle types and gameplay variation,
- better visual debugging overlays for the evaluation agents,
- stronger fairness heuristics beyond simple path passability,
- more polished report rendering and export output,
- extension-point documentation for researchers and gameplay programmers,
- benchmark scenes for repeatable experiments,
- additional media in the README so the systems can be understood instantly by visitors.

---

## Author

**Rishabh Kar**  
MSc Artificial Intelligence  
King's College London, United Kingdom
Ex-SAP | Ex-TCS

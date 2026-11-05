# Smart-Evac: Fire and Smoke Simulation

A Unity first-person disaster simulation set in an office/maze environment. Explore the level while fire and smoke evolve on a CPU grid, watch the health HUD respond to exposure, and restart after death.

## What works in the current project

- Office/maze scene with Rigidbody movement, camera controls and trigger-operated doors.
- Automatic ignition at three scene markers, fuel burning, heat diffusion and upper/lower smoke simulation.
- Pooled fire/smoke particles and smoke-driven global fog.
- Shared player health for heat/smoke damage, with a green/yellow/red health bar.
- Game-over overlay, paused scaled-time simulation/audio, and restart by button or keyboard.

The original research idea included AI evacuation guidance and social herding experiments. Live Gemini guidance, coordinated crowd evacuation, gaze tracking, participant management, trial CSV export and a win/exit state are not connected in this version. Research utilities remain in the repository separately.

## Open and run

1. Clone or download this repository's `main` branch.
2. Install Unity Hub and the project Editor version, **Unity `6000.3.27f1`**, with an active Unity license.
3. In Hub, select **Add project from disk** and choose **`Disaster Management`** inside the repository. That folder directly contains `Assets`, `Packages` and `ProjectSettings`.
4. Open the project and let Unity restore packages and import assets.
5. In Unity's Project panel, open **Assets → ScifiOfficeLite → Scene → Demo**.
6. Press **Play** and click inside the Game view.

`Assets/Scenes/Mainscene.unity` is a basic scene; use **Demo** for the simulation. All required recovered fire assets are committed under `Assets/RecoveredFire`; no separate fire-asset checkout is needed. A Gemini API key and Python packages are not required to play.

## Controls

| Input | Action |
| --- | --- |
| W / S | Move forward / backward |
| A / D | Turn in the demo controller's saved keyboard mode; the additional Rigidbody controller also reads these for strafing |
| E | Switch between keyboard and keyboard/mouse control modes |
| Mouse | Look around in keyboard/mouse mode |
| Control / Shift | Crouch in desktop control modes |
| R / Space | Restart after death |
| Restart button | Restart after death using the overlay |

The scene includes both `RigidbodyMovement` and `DemoFirstPersonController`; both can influence player movement. The old proposed research hotkeys 1/2/3/P are not implemented gameplay controls.

## Health and restart

Heat above 60 and lower smoke above 1 cause damage through `PlayerHealth`. Health starts at 100 in Demo. The fill color changes as health decreases.

At zero health, the retry overlay appears, scaled-time gameplay and audio pause, and the cursor becomes visible. Restart reloads the player's scene and restores pause-related settings. The health script creates missing retry controls at runtime, so a manually assigned retry panel is not required.

## Project layout

```text
disaster-simulation/
├── README.md
├── technical.md                  Implementation details and actual scene settings
├── architecture.md               Subsystem ownership, data flow and lifecycle
├── Disaster Management/          Open this folder in Unity Hub
│   ├── Assets/
│   │   ├── ScifiOfficeLite/       Demo scene, environment and controller
│   │   ├── Scripts/              Health, restart UI, movement, door and NPC source
│   │   ├── RecoveredFire/        Fire/smoke source and required effects
│   │   └── TextMesh Pro/         Text rendering resources
│   ├── Packages/
│   └── ProjectSettings/
├── analysis/                     Separate analytics scripts and sample data
├── tools/                        Separate audio-generation and Gemini utilities
└── docs/                         Earlier research/integration documents
```

## Documentation

- [Technical implementation](technical.md): grid data, solver behavior, exposure formulas, HUD, controls and build requirements.
- [System architecture](architecture.md): current components, scene associations, lifecycle and data-flow diagrams.
- [Earlier research protocol](docs/EXPERIMENT_PROTOCOL.md): proposed experiment context; it does not establish implemented runtime features.

The technical and architecture documents describe the current code and saved scene. Other research documents and sample analysis outputs may describe earlier plans or synthetic data.

## Building and Android status

The committed build scene list is empty. Before creating a standalone player or APK, add **`Assets/ScifiOfficeLite/Scene/Demo.unity`** to the active build profile's scene list. Scene-index-based restart in a built player depends on this.

For Android, install **Android Build Support**, **SDK/NDK tools** and **OpenJDK** for the Editor version you use. Configure and test mobile controls, then build with APK output rather than an app bundle when an `.apk` is wanted. A successful APK build and device performance test have not been established for this repository state.

## Current limitations

- The saved hazard grid is **500 × 650 cells**; topology baking, solver work, particle scans and debug gizmos can be expensive.
- The saved wall mask is zero, so grid propagation currently ignores walls; moving doors do not rebake topology.
- The solver is a gameplay approximation with a 2D hazard grid and global fog, rather than a validated physical fire model.
- NPC random-wander source exists but has no direct script attachment in the saved Demo scene; coordinated crowd behavior is not implemented.
- The owner reported the health/restart behavior working in Editor Play Mode; this is not an automated end-to-end or Android test claim.

## Common startup issues

- **Hub says “Select a valid project”:** choose the inner `Disaster Management` folder, not the repository root or its `Assets` folder.
- **An empty scene opens:** double-click `Assets/ScifiOfficeLite/Scene/Demo.unity`.
- **Unity asks for a license:** sign in through Hub and activate an appropriate license.
- **Import, lighting or script errors:** read the full message in **Window → General → Console** before changing assets. Do not delete source assets to clear an error.

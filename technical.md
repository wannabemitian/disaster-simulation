# Smart-Evac: Current Technical Implementation

This document describes the Unity project currently committed in this repository. For subsystem boundaries and lifecycle diagrams, see [architecture.md](architecture.md).

## 1. Project and entry scene

| Item | Current value |
| --- | --- |
| Unity project directory | `Disaster Management/` |
| Recorded Editor version | Unity `6000.3.27f1` |
| Playable scene | `Assets/ScifiOfficeLite/Scene/Demo.unity` |
| Rendering | Universal Render Pipeline `17.3.0` |
| Input System package | `1.20.1` |
| Interface | Unity uGUI `2.0.0` and TextMeshPro resources |
| Fire source directory | `Assets/RecoveredFire/My_Scripts/` |
| Player/game-over source directory | `Assets/Scripts/` |

All asset paths below are relative to `Disaster Management/`. Open that directory through Unity Hub, allow packages to restore, open **Demo**, and enter Play Mode. `Assets/Scenes/Mainscene.unity` is a basic scene, not the simulation entry point.

The recovered fire scripts, particle prefabs, materials, textures, and their `.meta` files are stored directly in the project. The old unresolved `Assets/Fire_Asset` Git reference has been removed. A separate asset repository is not required.

## 2. Player controls and environment interaction

Two movement scripts are present in the Demo scene:

- `Assets/Scripts/RigidbodyMovement.cs`: reads WASD from `Keyboard.current`; writes horizontal Rigidbody velocity in `FixedUpdate`; preserves vertical velocity and freezes Rigidbody rotation. Its code default speed is 5.
- `Assets/ScifiOfficeLite/Scripts/DemoFirstPersonController.cs`: controls the camera/player body, legacy input movement, and crouching. The saved scene selects `keyboard` mode (`controlType = 1`) with speed 3 and mouse sensitivity 50. This mode uses W/S for forward/back movement and A/D for turning. **E** switches between keyboard and keyboard/mouse modes; Control or Shift crouches in desktop modes.

Both components can affect the same player body. Their combined behavior must be considered when tuning movement. The project currently enables both input backends; changing to a single backend requires checking both scripts.

`SimpleDoor.cs` raises its assigned door by two local Y units when a collider associated with `RigidbodyMovement` enters its trigger, then restores the closed position on exit. The door reference must be assigned.

The demo controller also contains mobile button callbacks (`MobileWalk`, `MobileCrouch`) and an Android control mode. Their presence does not establish that the current scene is configured or validated for mobile play.

## 3. Fire and smoke data

`FireSimData.cs` defines the structs used by the simulation:

| Struct | Fields / role |
| --- | --- |
| `SimParameters` | Ambient, ignition and maximum temperature; burn/fuel rates; heat/smoke diffusivity; heat output; cooling coefficient |
| `CellTopology` | Four directional permeability values, initial fuel and solid-obstacle flag |
| `CellState` | Temperature, remaining fuel, fire intensity, upper smoke and lower smoke |
| `HazardSample` | Interpolated temperature, fire intensity, upper smoke and lower smoke for player exposure |

The simulation is a CPU grid in the world X/Z plane. Y is not used when sampling a hazard. It is a gameplay approximation; no real-world fire or evacuation validation is established by this implementation.

### Saved Demo configuration

Scene serialization overrides several code defaults. These are the saved `FireSimulationManager` values:

| Setting | Demo value |
| --- | --- |
| Grid dimensions | `500 × 650` (325,000 cells) |
| Cell size | `1` world unit |
| Tick interval | `0.1` seconds |
| Random smoke | Enabled; probability `0.5` per processed tick |
| Smoke injection amount | `20` upper-smoke units into one randomly selected non-solid cell |
| Wall mask | `0` (no layers selected) |
| Fuel mask | `1` (Default layer) |
| Ambient / ignition / maximum temperature | `35 / 80 / 1000` |
| Burn multiplier / fuel consumption rate | `1.5 / 0.015` |
| Thermal / smoke diffusivity | `1.5 / 4` |
| Heat output / cooling coefficient | `18000 / 0.025` |
| Debug gizmos | Temperature |

The code defaults are a `50 × 50` grid, ambient temperature 20, ignition temperature 200, thermal diffusivity 6, heat output 400 and cooling coefficient 0.02. Inspect the scene rather than assuming these defaults describe gameplay.

## 4. Simulation lifecycle and solver

`FireSimulationManager.Awake()` records its transform position as the grid origin, bakes topology, allocates two state arrays, and initializes temperature and fuel. `GridTopologyBaker` uses `Physics.CheckBox` for solid/fuel occupancy and `Physics.Linecast` for edge permeability. Fuel-covered cells use Perlin noise to vary initial fuel from 0.05 to 1.5; other open cells receive 0.05.

On a processed tick, the manager optionally injects random smoke, processes queued ignition cells, runs `FireSmokeSolver.StepSimulation`, swaps read/write arrays, and resets its timer. The update processes at most one tick per frame and resets the timer to zero; it does not catch up all missed ticks under load. The current source calls `ProcessIgnitions()` twice; the first call drains the queue.

`AutoIgniter` queues ignition at three assigned scene markers after a one-second delay. `ClickToIgnite` is also available in source for mouse-raycast ignition, but is not attached in the saved Demo scene. `IgniteCell(x, z)` ignores out-of-range coordinates; processing skips solid cells and raises ignition temperature to the configured maximum with initial fire intensity 1.5.

The solver uses four-neighbor heat and upper-smoke diffusion, fuel consumption, combustion heat, cooling toward ambient, and transfer from upper to lower smoke. Its diffusion substep calculation is:

```text
maxDiff = max(thermalDiffusivity, smokeDiffusivity)
maxStableDt = cellSize² / (4.01 × maxDiff)
subSteps = ceil(tickInterval / maxStableDt)
dt = tickInterval / subSteps
```

Temperature is clamped to ambient/maximum limits; upper/lower smoke are each clamped to 0–100. The first substep reads the original buffer; subsequent substeps read and write the output buffer in place, so this is not a fully double-buffered numerical solver at every substep. Lower smoke accumulates without a separate removal term. Topology is baked once; doors moving afterward do not rebake permeability.

## 5. Particle rendering and fog

`FireParticleSpawner` maintains per-cell fire/smoke references and initially creates pools of 50 instances of each assigned particle prefab. Pools can grow when exhausted.

| Visual | Activation and emission |
| --- | --- |
| Fire | Active above fire intensity 0.1; emission = intensity × 20 |
| Smoke | Active above combined smoke 0.5; emission = clamp(combined smoke × 0.2, 0, 8) |

Smoke particles are positioned 1.5 world units above their grid position. Inactive effects are stopped and returned to their pools. The spawner scans the full grid every rendered frame.

`HazardExposureAndVFXBridge` samples the grid at the player's position and enables global exponential-squared fog. Combined smoke drives fog density toward a maximum of 0.15 and blends the configured gray/near-black colors. This is global fog driven by local exposure, not a three-dimensional smoke volume.

## 6. Shared health, HUD and death

The authoritative player health is `PlayerHealth.currentHealth`. The hazard bridge calls `PlayerHealth.TakeDamage()` and mirrors the result into its legacy `playerHealth` field. This connects fire/smoke damage to both the HUD and death state.

For each frame with duration `deltaTime`, damage is calculated as:

```text
heatDamage = 0, if temperature <= 60
heatDamage = clamp01((temperature - 60) / (maxFireTemp - 60))
             × burnDamageRate × deltaTime, otherwise

smokeDamage = 0, if smokeLower <= 1
smokeDamage = smokeLower × smokeChokeRate × deltaTime, otherwise
```

The Demo bridge uses burn rate 10 and smoke rate 5. `PlayerHealth.Awake()` sets health to its maximum (100 in Demo), configures the assigned Slider, finds its Fill Image if needed, and ensures a restart interface exists. The bar interpolates with unscaled delta time; the fill changes from green through yellow to red based on remaining health.

`TakeDamage` ignores nonpositive/NaN damage and damage after death, clamps health to zero/maximum, and enters death once. The layer-10 trigger-damage fallback is bypassed when the player has the hazard bridge, avoiding duplicate damage. Layer 10 is a numeric fallback in code; it is not proof that a named fire layer is configured.

At death, `PlayerHealth` immediately empties the bar, shows the retry panel, sets `Time.timeScale = 0`, pauses audio, and unlocks/shows the cursor. This pauses scaled-time simulation and physics; UI and other unscaled/update logic can still run.

`HealthGameOverUI.Ensure()` supplies a screen-space overlay Canvas, dim background, TextMeshPro message, and Restart button when references are missing. The generated Canvas uses sorting order 1000 and reference resolution `1280 × 720`. It reuses an existing EventSystem or creates one if absent. Runtime-generated UI is not saved back into the scene.

After death, **R**, **Space**, or **Restart** reloads the player's scene. Pause/audio/cursor settings are restored before reload and when the component is destroyed. In the Editor, restart uses `EditorSceneManager.LoadSceneInPlayMode`; a built player uses the scene's build index.

## 7. NPC and research tooling status

`NPC_RandomWander.cs` exists and requires a `NavMeshAgent`. It chooses random destinations within radius 15 using `NavMesh.SamplePosition` and area mask 1. There is no direct reference to this script in the saved Demo scene; prefab usage and NavMesh setup must be checked before claiming working crowd behavior. It does not implement coordinated evacuation or social herding.

The `analysis/`, `tools/audio/`, `tools/gemini/`, and research documents remain as separate scaffolding. The current Unity gameplay scripts do not implement or call the previously specified `HazardManager`, `NPCHerding`, `AI_SupervisorAPI`, `GazeAndController`, `TelemetryLogger`, or `ExperimenterController` systems. There is no connected live Gemini guidance, corridor cost scoring, A/B/C guidance selection, gaze measurement, participant progression, CSV trial export, or successful-evacuation state. Sample analytics data is not evidence of gameplay-generated telemetry.

## 8. Build and verification boundaries

The committed `ProjectSettings/EditorBuildSettings.asset` has an empty scene list. Add `Assets/ScifiOfficeLite/Scene/Demo.unity` to the active build profile before building; restart in a standalone/Android player requires the scene to have a valid build index.

For an Android build, install Android Build Support with matching SDK/NDK/OpenJDK modules for the Editor version actually used. Tools installed on one laptop are not repository dependencies available on every machine. An APK and mobile performance/input validation have not been established by this repository state.

Health changes compiled successfully in the connected Editor, and the project owner reported the health/restart behavior working in Play Mode. A complete automated playthrough, APK build, and benchmark were not completed as part of this update.

Performance-sensitive areas include the 325,000-cell topology bake, full-grid solver work, per-frame particle scan, growing particle pools, and temperature gizmos. With the saved wall mask of zero, wall collisions do not block the simulation grid. The run is not deterministic because it uses Unity random smoke injection without a controlled seed.

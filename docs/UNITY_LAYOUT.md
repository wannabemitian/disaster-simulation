# Unity layout: Smart-Evac blockout

This layout translates `architecture.md` and `technical.md` into one scene. The repository does not yet contain a Unity project or the gameplay scripts described in those specifications. The supplied builder creates geometry and reference objects; it does not implement trials, movement, navigation, telemetry, AI requests, or hazards.

## Build the scene

1. Open your Unity 3D project (Built-in or URP).
2. Copy `unity_staging/Editor/SmartEvacLayoutBuilder.cs` into `Assets/Editor/`.
3. Wait for compilation, then select **Smart-Evac → Build Layout Blockout**. The command opens a new scene after offering to save your current scene. It assigns layers 8–12; if another layer already occupies one of these slots, it stops before making changes.
4. Save as `Assets/Scenes/SmartEvac_MainScene.unity`.
5. Select `[Environment]` and use the Scene view's top view to inspect the plan. Use the camera preview to inspect the participant's view.

Generated materials live in `Assets/SmartEvac/` and are reused on subsequent builds. The builder has no external package dependencies. Unity compilation and visual checks must be performed in your Editor; they have not been run in this repository.

## Floor plan

One Unity unit represents one metre. Y is up, +Z is forward, and the intersection centre is the origin. All measurements below are world coordinates.

```text
                         Center exit (0, 24.5)
                                  │
                         hazard anchor (0, 19)
                                  │ 6 m wide
                                  │
Left exit ────────────────── [ decision ] ────────────────── Right exit
(-24.5, 0)                    [ 6 × 6 m ]                     (24.5, 0)
                                  │
                             crowd previews
                                  │ 6 m wide
                         trial start z = -17
                                  │
                         player spawn (0, -19)
```

| Element | Position / extent | Purpose |
| --- | --- | --- |
| Approach hallway | x = −3…3, z = −21…−3 | Gives participant time to observe crowd |
| Intersection | x/z = −3…3 | Shared decision region |
| Three branches | From intersection edge to 25 m from centre | Identical 22 m branch floors |
| Walls | 3.5 m high, 0.3 m thick | Clear boundaries; ceilings omitted for editing |
| Branch commitment triggers | 3.4 m from centre | Explicit forward branch selection |
| Exit triggers | 24.5 m from centre | Equal nominal route length, within specified 20–30 m |
| Hazard anchors | 19 m from centre | Replace orange placeholders with fire effects |
| Smoke anchors | 17 m from centre | Attach smoke effects |
| Crowd destinations | 23 m from centre | Reachable targets beyond hazards and before exit triggers |
| Player | Feet at (0, 0, −19), eye height 1.65 m | Faces intersection along +Z |

Equal branch lengths control distance, but left/right require a turn while centre is straight. Counterbalance safe routes and crowd routes across trials; equal geometry alone does not remove directional bias.

## Connect the experiment

Manager objects are empty placeholders named for the scripts in `technical.md`. Add those components when implemented, then wire:

| Component | Scene references |
| --- | --- |
| `HazardManager` | Three `Hazard_Fire_*` roots; three guidance roots; separately handle paired smoke roots |
| `NPCHerding` | `CrowdSpawnOrigin`, `RouteTargets/*TrapPoint`, your NPC prefab |
| `GazeAndController` | Player CharacterController, Main Camera, signage layer 8, crowd layer 9 |
| `TelemetryLogger` | Intersection, branch commitment, trial start, and exit trigger relay components |
| `AI_SupervisorAPI` | Add TextMeshPro warning panel under HUD canvas and two AudioSources; import staged audio |

The 12 blue capsules are placement previews. Replace them with spawned NavMeshAgents rather than keeping both populations. Bake navigation on floors and walls using your project's navigation workflow. Keep trap routes traversable so crowd agents can reach their assigned destinations.

All fire and smoke roots start inactive. Trial initialization should activate exactly two paired fire/smoke roots, assign one safe route, and send the crowd down one hazardous route. Orange markers are visual placeholders without colliders; they must not obstruct navigation.

Mode A starts active with a centre-facing EXIT plate. Mode B contains a floor arrow whose parent pivot is at the intersection centre: rotate it to face the selected safe route. Mode C contains an empty screen-space overlay canvas. Add a readable upper-centre warning panel that leaves the junction visible, a CanvasScaler, TextMeshPro text, and directional audio. Enable only the selected guidance mode. Do not show safe-route labels, route costs, or researcher telemetry to participants.

The documents specify static signage toward the burning crowd route, but hazards are randomized. Decide whether static signs remain physically fixed with a constrained crowd assignment, or are repositioned between trials to match the selected crowd route; record that policy in the protocol. The generated sign is only a centre-route placeholder.

## Measurement details to resolve before trials

- Stop decision timing on a valid branch commitment trigger. Leaving the intersection back toward spawn should not count as a choice. Track only the player's colliders, and guard against repeated trigger events.
- Complete a trial only once at an exit trigger. NPCs reaching the same trigger must not write telemetry.
- Screen-space HUD text cannot be measured by the specified world-space physics raycast. Define a separate HUD attention measure or use world-space guidance. Camera-centre dwell is an attention proxy rather than eye tracking.
- The supplied gaze pseudocode prioritizes signage and ignores walls, so it can count signs behind crowds or walls. Use a nearest-hit query including occluding geometry, then classify the visible hit if visibility is part of the intended measure.
- Keep arrow/HUD timing consistent at intersection entry. Prefetch warnings at reset, with the deterministic fallback ready before entry.

Before collecting data, walk all three routes, confirm wall collision, check twelve agents can reach each destination, test reset across all modes, verify only one safe route per trial, and inspect one output CSV row per player completion. This blockout requires gameplay components before those checks can pass.

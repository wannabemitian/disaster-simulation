# Smart-Evac: Unity Implementation & Asset Integration Guide

This guide details how external assets and specifications map directly into the Unity Editor.

For a concrete floor plan and an importable scene blockout builder, start with [Unity Layout](UNITY_LAYOUT.md).

---

## 1. Physics Layers & Collision Matrix Configuration

Configure the Unity Physics Layers under **Edit $\rightarrow$ Project Settings $\rightarrow$ Tags and Layers**:

| Layer Index | Layer Name | Assigned To | Interacts With |
| :--- | :--- | :--- | :--- |
| **Layer 8** | `Signage_UI` | Static exit signs, glowing floor arrows | Hit by Gaze Raycast |
| **Layer 9** | `Crowd_NPC` | NavMeshAgent capsule crowd prefabs | Hit by Gaze Raycast |
| **Layer 10** | `Hazard_Fire` | Fire particle triggers & light volumes | Ignored by Gaze Raycast |
| **Layer 11** | `Hazard_Smoke` | Smoke particle emitters & fog volumes | Ignored by Gaze Raycast |
| **Layer 12** | `Trigger_Zones` | Intersection trigger box & Corridor exit boxes | Player CharacterController |
| **Layer 0** | `Default` | Architecture geometry (floors, walls, ceilings) | Ignored by Gaze Raycast |

### Gaze Raycast LayerMask Setup in `GazeAndController.cs`
In the Unity Inspector on the Player GameObject:
- Set `signageLayer` $\rightarrow$ Check only **Layer 8 (`Signage_UI`)**.
- Set `crowdLayer` $\rightarrow$ Check only **Layer 9 (`Crowd_NPC`)**.
- Set `maxRayDistance` $\rightarrow$ `35.0`.

---

## 2. Audio Asset Staging & Import

The audio assets generated via `tools/audio/generate_audio_assets.py` reside in `assets_staging/audio/`.
To import into Unity:
1. Copy the contents of `assets_staging/audio/` to `Assets/Audio/`:
   ```bash
   mkdir -p Assets/Audio
   cp assets_staging/audio/*.wav Assets/Audio/
   ```
2. In the Unity Project window, select all imported `.wav` files:
   - **Load Type**: `Decompress On Load` (eliminates playback latency).
   - **Preload Audio Data**: Checked.
   - **Compression Format**: `PCM`.

3. Assign Audio Clips in `AI_SupervisorAPI` Inspector:
   - `Alert Chime Source` $\rightarrow$ AudioSource component with `alert_chime.wav`.
   - `Voice Clip Source` $\rightarrow$ Dedicated AudioSource component.
   - `Left Safe Voice Clip` $\rightarrow$ `Voice_EvacuateLeft.wav`.
   - `Center Safe Voice Clip` $\rightarrow$ `Voice_EvacuateCenter.wav`.
   - `Right Safe Voice Clip` $\rightarrow$ `Voice_EvacuateRight.wav`.

---

## 3. Telemetry Output & File I/O Safety

Per `technical.md` Section 2:
```csharp
string csvPath = Path.Combine(Application.dataPath, "../ExperimentData.csv");
```
- The telemetry file `ExperimentData.csv` is written to the **project root directory** (one level above `Assets/`).
- **Why this matters**: Writing files into `Assets/` while the Unity Editor is running forces Unity to perform AssetDatabase refreshes, introducing stutter and garbage-collection spikes during trials. Writing to the project root avoids all background re-import interruptions.

---

## 4. NavMesh Baking for NPC Herding

1. Mark hallway floors and staircases as **Navigation Static**.
2. Mark fire hazard zones as high-cost or impassable areas if needed, but note that `NPCHerding.cs` explicitly targets the burning trap corridor to model irrational panic.
3. Open **Window $\rightarrow$ AI $\rightarrow$ Navigation** and click **Bake**.
4. Attach `NavMeshAgent` to the crowd prefab capsule:
   - `Speed`: $5.5\text{ m/s}$ (slightly faster than player walking speed $4.5\text{ m/s}$ to simulate sudden panic rush).
   - `Angular Speed`: $360^\circ/\text{s}$.
   - `Acceleration`: $12.0\text{ m/s}^2$.

---

## 5. Gemini API Configuration & Security

To protect your API key from being committed to version control:
1. Copy `.env.example` to `.env` and add your `GEMINI_API_KEY`.
2. For in-editor execution, read the key from an ignored config file or environment variable:
   ```csharp
   string apiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
   ```
3. Never hardcode sensitive API keys directly in committed scene files or public scripts.

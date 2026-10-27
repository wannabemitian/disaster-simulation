# Smart-Evac: Technical Implementation Specification

## 1. Mathematical Formalism: Path Traversal Cost Model

Path safety is evaluated using a weighted graph traversal cost function:

$$W(\text{path}) = d_{\text{exit}} + (\alpha \cdot H_{\text{fire}}) + (\beta \cdot S_{\text{smoke}})$$

### Parameter Specifications

| Symbol | Parameter Name | Range | Unit / Meaning | Description |
| :--- | :--- | :--- | :--- | :--- |
| $d_{\text{exit}}$ | Base Distance | $[20.0, 30.0]$ | Meters | Euclidean/NavMesh distance from intersection center to corridor exit. |
| $H_{\text{fire}}$ | Fire Hazard Metric | $[0.0, 1.0]$ | Normalized Density | $0.0$ if corridor is clear; $1.0$ if active fire hazard is present. |
| $S_{\text{smoke}}$ | Smoke Hazard Metric | $[0.0, 1.0]$ | Normalized Opacity | $0.0$ if clear; $[0.7, 1.0]$ if dense toxic particulate fog is present. |
| $\alpha$ | Fire Cost Penalty Weight | $\ge 0.0$ | Scalar (Default: $100.0$) | Heavily penalizes flame presence in human pathing utility. |
| $\beta$ | Smoke Cost Penalty Weight | $\ge 0.0$ | Scalar (Default: $50.0$) | Penalizes reduced visibility and respiratory hazard. |

### Path Cost Evaluation Example
* **Safe Corridor (e.g., Right)**:
  $$W(\text{Right}) = 25.0 + (100.0 \times 0.0) + (50.0 \times 0.0) = \mathbf{25.0}$$
* **Burning Trap Corridor (e.g., Center)**:
  $$W(\text{Center}) = 25.0 + (100.0 \times 1.0) + (50.0 \times 0.8) = 25.0 + 100.0 + 40.0 = \mathbf{165.0}$$
* **Secondary Trap Corridor (e.g., Left)**:
  $$W(\text{Left}) = 25.0 + (100.0 \times 1.0) + (50.0 \times 1.0) = \mathbf{175.0}$$

The path with $\min(W)$ is designated as the ground-truth safe route.

---

## 2. Telemetry Schema (`ExperimentData.csv`)

### File Path
```csharp
string csvPath = Path.Combine(Application.dataPath, "../ExperimentData.csv");
```
Saving outside the `Assets/` directory prevents editor compilation lag and AssetDatabase re-import locks.

### Column Definitions

| Column | Data Type | Permitted Values | Description |
| :--- | :--- | :--- | :--- |
| `Participant_ID` | `string` | `P001`, `P002`, ... | Monotonically incremented ID, managed via static integer. |
| `UI_Mode` | `string` | `Static`, `DynamicArrow`, `AI_HUD` | Active guidance condition for the trial. |
| `Safe_Path_Assigned` | `string` | `Left`, `Center`, `Right` | Procedurally designated safe corridor. |
| `Player_Chosen_Path` | `string` | `Left`, `Center`, `Right` | Corridor in which player crossed the exit trigger collider. |
| `Chose_Safe_Path` | `bool` | `True`, `False` | Evaluates if `Player_Chosen_Path == Safe_Path_Assigned`. |
| `Followed_Crowd_Into_Trap`| `bool` | `True`, `False` | Evaluates if `Player_Chosen_Path == Crowd_Target_Path`. |
| `Decision_Latency_Sec` | `float` (f2) | $\ge 0.00$ | Elapsed time between entering and exiting intersection trigger box. |
| `Gaze_Time_On_UI_Sec` | `float` (f2) | $\ge 0.00$ | Cumulative seconds camera raycast was incident on `Signage_UI` layer. |
| `Gaze_Time_On_Crowd_Sec`| `float` (f2) | $\ge 0.00$ | Cumulative seconds camera raycast was incident on `Crowd_NPC` layer. |
| `Total_Evac_Time_Sec` | `float` (f2) | $\ge 0.00$ | Time elapsed from trial start/movement to touching exit trigger. |

### Header Row
```csv
Participant_ID,UI_Mode,Safe_Path_Assigned,Player_Chosen_Path,Chose_Safe_Path,Followed_Crowd_Into_Trap,Decision_Latency_Sec,Gaze_Time_On_UI_Sec,Gaze_Time_On_Crowd_Sec,Total_Evac_Time_Sec
```

---

## 3. Core Script Architecture & Class Specifications

### 3.1 `HazardManager.cs`
Controls path assignment, activates particle hazards, and updates spatial signage.

```csharp
public enum CorridorID { Left, Center, Right }
public enum UIMode { Static, DynamicArrow, AI_HUD }

public class HazardManager : MonoBehaviour
{
    [Header("Cost Weights")]
    public float alpha = 100f;
    public float beta = 50f;

    [Header("Hazard References")]
    public GameObject leftFireTrap;
    public GameObject centerFireTrap;
    public GameObject rightFireTrap;

    [Header("Guidance Mode Visuals")]
    public GameObject modeAStaticSigns;
    public GameObject modeBDynamicArrows;
    public GameObject modeCHudCanvas;

    public CorridorID SafeCorridor { get; private set; }
    public CorridorID CrowdTargetCorridor { get; private set; }
    public UIMode CurrentMode = UIMode.Static;

    public void GenerateLayout();
    public void SetUIMode(UIMode mode);
    public float CalculatePathCost(CorridorID corridor, float fire, float smoke, float dist);
}
```

### 3.2 `NPCHerding.cs`
Controls the 10–15 panicking capsules simulating social herding.

```csharp
public class NPCHerding : MonoBehaviour
{
    [Header("Crowd Settings")]
    public GameObject npcPrefab;
    public int crowdCount = 12;
    public Transform spawnOrigin;
    public float spawnRadius = 2.5f;

    [Header("Waypoint Targets")]
    public Transform leftTrapPoint;
    public Transform centerTrapPoint;
    public Transform rightTrapPoint;

    private List<NavMeshAgent> spawnedAgents = new List<NavMeshAgent>();

    public void SpawnAndHerdCrowd(CorridorID targetTrap);
    public void ResetCrowd();
}
```

### 3.3 `AI_SupervisorAPI.cs`
Communicates asynchronously with the Gemini Flash REST endpoint with instant zero-latency caching.

```csharp
[System.Serializable]
public class GeminiContentRequest
{
    public Content[] contents;
    [System.Serializable]
    public class Content { public Part[] parts; }
    [System.Serializable]
    public class Part { public string text; }
}

public class AI_SupervisorAPI : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiKey = "YOUR_GEMINI_API_KEY";
    private const string API_URL_BASE = 
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";

    [Header("Audio Components")]
    public AudioSource alertChimeSource;
    public AudioSource voiceClipSource;
    public AudioClip leftSafeVoiceClip;
    public AudioClip centerSafeVoiceClip;
    public AudioClip rightSafeVoiceClip;

    [Header("UI Text Display")]
    public TextMeshProUGUI hudWarningText;

    private string cachedWarning = "";
    private bool isPreFetched = false;

    // Called on Scene/Trial Start
    public void PreFetchWarning(CorridorID safeCorridor, string sensorStatus);
    
    // Called when player enters intersection collider
    public void TriggerDisplayCachedAlert(CorridorID safeCorridor);
}
```

#### Gemini Prompt Template
```text
System Instruction:
"You are Smart-Evac, an emergency AI supervisor. An evacuation is in progress. 
Sensors detect fire and toxic smoke in two paths. Output exactly ONE urgent sentence directing the occupant to the safe path and instructing them to ignore the panicking crowd."

User Prompt:
"Sensor Status: Left: [FIRE/SMOKE/CLEAR], Center: [FIRE/SMOKE/CLEAR], Right: [FIRE/SMOKE/CLEAR]. Designated safe exit: [SAFE_PATH]."
```

### 3.4 `GazeAndController.cs`
Lightweight First-Person Controller and LayerMask-based Attention Raycasting.

```csharp
public class GazeAndController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float mouseSensitivity = 2.0f;
    public CharacterController characterController;
    public Transform cameraTransform;

    [Header("Attention Tracking")]
    public LayerMask signageLayer;   // Layer 8: Signage_UI
    public LayerMask crowdLayer;     // Layer 9: Crowd_NPC
    public float maxRayDistance = 35f;

    public float GazeTimeUI { get; private set; }
    public float GazeTimeCrowd { get; private set; }

    void Update()
    {
        HandleMovement();
        HandleGazeRaycast();
    }

    private void HandleGazeRaycast()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRayDistance, signageLayer))
        {
            GazeTimeUI += Time.deltaTime;
        }
        else if (Physics.Raycast(ray, out hit, maxRayDistance, crowdLayer))
        {
            GazeTimeCrowd += Time.deltaTime;
        }
    }

    public void ResetGazeMetrics();
}
```

### 3.5 `TelemetryLogger.cs` & `ExperimenterController.cs`
Orchestrates metrics aggregation and manual researcher keybinds.

```csharp
public class ExperimenterController : MonoBehaviour
{
    public static int currentParticipantID = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) FindObjectOfType<HazardManager>().SetUIMode(UIMode.Static);
        if (Input.GetKeyDown(KeyCode.Alpha2)) FindObjectOfType<HazardManager>().SetUIMode(UIMode.DynamicArrow);
        if (Input.GetKeyDown(KeyCode.Alpha3)) FindObjectOfType<HazardManager>().SetUIMode(UIMode.AI_HUD);
        
        if (Input.GetKeyDown(KeyCode.R)) ResetCurrentTrial();
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            currentParticipantID++;
            Debug.Log($"[Smart-Evac] Next Participant ID: P{currentParticipantID:D3}");
        }
    }

    public void ResetCurrentTrial();
}

public class TelemetryLogger : MonoBehaviour
{
    private float decisionStartTime;
    private float totalStartTime;
    private bool insideIntersection = false;
    public float DecisionLatency { get; private set; }

    public void OnEnterIntersectionTrigger();
    public void OnExitIntersectionTrigger();
    public void OnCorridorExitReached(CorridorID chosenCorridor);
    private void WriteRowToCSV(string rowContent);
}
```

---

## 4. Unity Scene Hierarchy & Setup Blueprint

```text
SmartEvac_MainScene
├── [Managers]
│   ├── HazardManager (Script)
│   ├── AI_SupervisorAPI (Script, AudioSources)
│   ├── TelemetryLogger (Script)
│   └── ExperimenterController (Script)
├── [Environment]
│   ├── MainHallway (Colliders, Walls, Floor)
│   ├── Intersection_Node
│   │   └── Intersection_TriggerZone (Trigger BoxCollider, Layer: Trigger_Zones)
│   ├── Corridor_Left
│   │   ├── Hazard_Fire_Left (ParticleSystem, Lights, Colliders)
│   │   └── Exit_Trigger_Left (Trigger BoxCollider -> CorridorID.Left)
│   ├── Corridor_Center
│   │   ├── Hazard_Fire_Center
│   │   └── Exit_Trigger_Center (Trigger BoxCollider -> CorridorID.Center)
│   └── Corridor_Right
│       ├── Hazard_Fire_Right
│       └── Exit_Trigger_Right (Trigger BoxCollider -> CorridorID.Right)
├── [Crowd_Group]
│   ├── CrowdSpawnOrigin
│   └── NPCHerdingManager (Script)
├── [Guidance_UI]
│   ├── ModeA_StaticSigns (Layer: Signage_UI)
│   ├── ModeB_DynamicArrows (Layer: Signage_UI, Dynamic rotation script)
│   └── ModeC_HUDCanvas (Canvas ScreenSpace-Overlay)
│       └── WarningAlertText (TextMeshProUGUI)
└── [Player_FirstPerson]
    ├── CharacterController
    ├── GazeAndController (Script)
    └── Main Camera (Raycast Source)
```

---

## 5. Audio Pipeline Configuration (Mode C)

1. **Alert Chime**: Short, piercing 800Hz two-tone alarm ($120\text{ ms}$) played immediately upon crossing intersection threshold.
2. **Voice Synthesis Mappings**:
   * Safe Corridor `Left` $\rightarrow$ `Voice_EvacuateLeft.wav`
   * Safe Corridor `Center` $\rightarrow$ `Voice_EvacuateCenter.wav`
   * Safe Corridor `Right` $\rightarrow$ `Voice_EvacuateRight.wav`
3. **Execution Sequence**:
   `AudioSource.PlayOneShot(alertChime)` $\rightarrow$ Delay $150\text{ ms}$ $\rightarrow$ `AudioSource.PlayOneShot(selectedVoiceClip)`.

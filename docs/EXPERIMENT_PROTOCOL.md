# Smart-Evac: Experimental Research Protocol & Operational Manual

## 1. Research Overview & Theoretical Framework

### 1.1 Objective
The **Smart-Evac** experiment measures the empirical tension between **Algorithmic AI Guidance** and **Social Herding (Crowd Conformity)** in high-stress emergency egress scenarios.

When trapped in emergency situations (e.g., building fires), human evacuees exhibit strong social proof tendencies: they frequently follow fleeing crowds into dead-ends or fire traps, ignoring exit signage. This study quantifies the threshold at which multimodal AI warnings (visual HUD + directional voice + alert chime) overcome crowd conformity bias.

### 1.2 Experimental Design
* **Design Type**: Within-subjects, counterbalanced repeated-measures design.
* **Independent Variable**: Guidance Condition (3 Levels):
  * **Mode A (Control - Static Wall Signs)**: Fixed green EXIT signs pointing down the hallway where the crowd runs.
  * **Mode B (Spatial UI - Dynamic Floor Arrows)**: 3D illuminated chevron arrows rotating dynamically toward the designated safe corridor.
  * **Mode C (Multimodal AI HUD + Audio)**: Live generative AI text warning + 800Hz alert chime + directional voice cue (*"Warning: Evacuate via [Direction] corridor"*).
* **Dependent Variables**:
  1. **Compliance Rate (`Chose_Safe_Path`)**: Binary (True/False).
  2. **Social Herding Susceptibility (`Followed_Crowd_Into_Trap`)**: Binary (True/False).
  3. **Decision Latency (`Decision_Latency_Sec`)**: Float (seconds elapsed in decision zone).
  4. **Visual Attention Allocation (`Gaze_Time_On_UI_Sec` vs `Gaze_Time_On_Crowd_Sec`)**: Gaze Raycast dwell time.
  5. **Overall Evacuation Duration (`Total_Evac_Time_Sec`)**: Start movement to exit trigger.

---

## 2. Counterbalancing & Latin Square Matrix

To prevent order effects (fatigue, environment familiarity, or learning bias), participants are systematically assigned one of three condition sequences according to a Latin Square:

| Participant ID Modulo | Sequence Order | Block 1 | Block 2 | Block 3 |
| :--- | :--- | :--- | :--- | :--- |
| **$P \pmod 3 == 1$** (P001, P004, P007...) | **Group 1 (ABC)** | Mode A (Static) | Mode B (Dynamic) | Mode C (AI HUD) |
| **$P \pmod 3 == 2$** (P002, P005, P008...) | **Group 2 (BCA)** | Mode B (Dynamic) | Mode C (AI HUD) | Mode A (Static) |
| **$P \pmod 3 == 0$** (P003, P006, P009...) | **Group 3 (CAB)** | Mode C (AI HUD) | Mode A (Static) | Mode B (Dynamic) |

> [!IMPORTANT]
> Always verify the assigned sequence before beginning a participant's session to ensure balanced distribution.

---

## 3. Experimenter Hotkey Operations

The researcher controls the simulation state during runtime using dedicated hotkeys:

| Key | Action | Description |
| :--- | :--- | :--- |
| `1` | **Set Mode A** | Activates Static Wall Signs (Control). |
| `2` | **Set Mode B** | Activates Dynamic Glowing Floor Arrows. |
| `3` | **Set Mode C** | Activates Live Gemini AI HUD Canvas and Audio Triggers. |
| `R` | **Reset Trial** | Resets player to spawn, randomizes hazard locations, re-spawns crowd flock, and pre-fetches AI warning. |
| `P` | **Next Participant** | Increments static `currentParticipantID` (e.g. `P001` $\rightarrow$ `P002`). |

---

## 4. Step-by-Step Experimental Procedure

### 4.1 Phase 1: Pre-Trial Preparation (5 minutes)
1. Verify computer audio is unmuted and headphones are placed on participant.
2. Ensure `ExperimentData.csv` is accessible in the project root directory.
3. Confirm Gemini API key is configured or offline fallback circuit breaker is ready.
4. Have participant read and sign the Informed Consent Form (`docs/ETHICS_AND_CONSENT.md`).
5. Open Unity Play Mode or launch standalone executable.

### 4.2 Phase 2: Participant Briefing Script
Read the following instructions verbatim to the participant:

> *"You are about to participate in an emergency evacuation simulation. You are currently located inside an office facility where a fire alarm has triggered. Your sole objective is to navigate out of the building safely through one of the designated corridors as quickly as possible.*
> 
> *Use the **W, A, S, D** keys to walk, and use the **mouse** to look around. You may observe other occupants in the corridor. You may also observe signage or automated building alerts. When you reach an intersection, decide which corridor to enter and proceed to the end.*
> 
> *You will complete three separate rounds. After each round, please wait for the researcher to prompt the next run."*

### 4.3 Phase 3: Trial Execution (10 minutes)
1. Look up the assigned condition for Trial 1 according to the Latin Square table.
2. Press `1`, `2`, or `3` to activate the corresponding guidance mode.
3. Press `R` to initialize the trial.
4. Instruct the participant to begin walking.
5. As the participant crosses the corridor exit, observe the Unity Console output:
   `[Smart-Evac] Trial logged for P001 | Mode: AI_HUD | Chose Safe: True`
6. Repeat for Trial 2 and Trial 3 with the appropriate mode keys.
7. Once all 3 trials are logged, press `P` to increment the Participant ID for the next participant.

### 4.4 Phase 4: Post-Trial Questionnaire (5 minutes)
Administer the post-experimental survey to measure subjective trust and perceived pressure.

---

## 5. Post-Experiment Subjective Questionnaire

Rate each statement on a 5-point Likert scale (**1 = Strongly Disagree**, **5 = Strongly Agree**):

### Subscale A: Trust in Algorithmic Guidance (Mode C)
1. *"I trusted the AI HUD alerts more than the behavior of the other people running."* (1 - 5)
2. *"The AI guidance provided clear, actionable directions during the evacuation."* (1 - 5)
3. *"I felt confident that following the AI alert would lead me to a safe exit."* (1 - 5)

### Subscale B: Social Herding Pressure
4. *"When I saw the crowd running into a corridor, I felt a strong urge to follow them."* (1 - 5)
5. *"I assumed the crowd knew the safest way out of the building."* (1 - 5)

### Subscale C: Cognitive Workload & Stress (NASA-TLX Short)
6. *"Making a decision at the intersection was mentally stressful."* (1 - 5)
7. *"I felt rushed and under time pressure when selecting a corridor."* (1 - 5)

---

## 6. Post-Run Telemetry Validation & Analysis

Immediately upon concluding an experimental session:
```bash
# 1. Validate CSV telemetry structure
python3 analysis/schema_validator.py ExperimentData.csv

# 2. Run statistical aggregation and generate summary report
python3 analysis/analyze_telemetry.py ExperimentData.csv
```
The script will print summary metrics and output an updated Markdown report to `analysis/reports/telemetry_summary.md`.

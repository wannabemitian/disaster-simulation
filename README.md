# Smart-Evac: Human-AI Trust vs. Social Herding Testbed

[![Data Validation](https://github.com/wannabemitian/disaster-simulation/actions/workflows/ci.yml/badge.svg)](https://github.com/wannabemitian/disaster-simulation/actions/workflows/ci.yml)
[![Python 3.9+](https://img.shields.io/badge/Python-3.9%2B-blue.svg)](https://www.python.org/)
[![Unity 2022.3+ LTS](https://img.shields.io/badge/Unity-2022.3%2B%20LTS-black.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> A 3D experimental simulation platform engineered to investigate **Human-AI Trust vs. Social Herding Behavior** in emergency evacuation scenarios.

---

## 1. Scientific Overview & Objective

In life-threatening disasters (e.g., structural fires), human evacuees frequently succumb to **social herding**—following fleeing crowds into dead-ends or fire traps even when standard emergency signage indicates safer alternate routes. 

**Smart-Evac** investigates whether and when participants trust algorithmic guidance over observable human behavior:
* **The Conflict**: At a corridor intersection, a panicking crowd of 10–15 occupants sprints into a smoke-filled, burning hallway.
* **The Intervention**: The participant is presented with one of three guidance paradigms:
  * **Mode A (Control - Static Wall Signs)**: Traditional green EXIT signs.
  * **Mode B (Spatial UI - Dynamic Floor Arrows)**: 3D illuminated chevrons dynamically directing toward the safe corridor.
  * **Mode C (Multimodal AI HUD + Voice)**: Zero-latency cached Gemini AI HUD warning paired with an 800Hz alert chime and synthesized voice direction.
* **The Metrics**: Quantifies choice compliance, herding susceptibility, decision latency, and visual attention allocation (gaze raycast dwell time on crowd vs. UI).

---

## 2. System Architecture & Specifications

For comprehensive technical and architectural documentation, refer to:
* [System Architecture Specification](file:///home/jyothipavanavik/unity_project_1/architecture.md): Subsystem decomposition, state machine, zero-latency pre-fetch pipeline, and physics layer matrix.
* [Technical Implementation Specification](file:///home/jyothipavanavik/unity_project_1/technical.md): Path traversal cost model, telemetry schema, C# class contracts, and Gemini prompt templates.
* [Experimental Research Protocol](file:///home/jyothipavanavik/unity_project_1/docs/EXPERIMENT_PROTOCOL.md): Latin-Square counterbalancing, researcher checklist, and participant questionnaires.
* [Unity Integration Guide](file:///home/jyothipavanavik/unity_project_1/docs/UNITY_INTEGRATION_GUIDE.md): Layer masks, NavMesh setup, and AudioSource wiring.

---

## 3. Repository Structure

```text
disaster-simulation/
├── .github/
│   └── workflows/
│       └── ci.yml                     # Continuous integration & data validation pipeline
├── analysis/                          # Telemetry analytics, validation & simulation
│   ├── analyze_telemetry.py           # Statistical evaluation & markdown report generator
│   ├── generate_sample_data.py        # Synthetic multi-participant telemetry generator
│   ├── requirements.txt               # Optional Python packages (pandas, scipy, matplotlib)
│   ├── sample_ExperimentData.csv      # 30-participant Latin-Square benchmark dataset
│   ├── schema_validator.py            # Strict schema validator against technical.md
│   ├── test_analysis_pipeline.py      # End-to-end unit test suite
│   └── reports/                       # Generated analysis summaries and tables
│       └── telemetry_summary.md
├── assets_staging/                    # External staged assets for Unity import
│   └── audio/                         # Synthesized WAV audio files (PCM 16-bit 44.1kHz)
│       ├── alert_chime.wav            # 800Hz/1000Hz two-tone emergency alarm (120ms)
│       ├── Voice_EvacuateLeft.wav     # Left corridor guidance voice clip
│       ├── Voice_EvacuateCenter.wav   # Center corridor guidance voice clip
│       └── Voice_EvacuateRight.wav    # Right corridor guidance voice clip
├── docs/                              # Experimental and integration documentation
│   ├── ETHICS_AND_CONSENT.md          # Human subjects IRB consent form template
│   ├── EXPERIMENT_PROTOCOL.md         # Operational researcher manual & Latin Square
│   └── UNITY_INTEGRATION_GUIDE.md     # Layer masks, AudioSources, and NavMesh baking
├── tools/                             # Research and API utility scripts
│   ├── audio/
│   │   └── generate_audio_assets.py   # Procedural synthesizer for chimes & voice cues
│   └── gemini/
│       └── test_gemini_api.py         # Gemini Flash REST tester & latency benchmark
├── .env.example                       # Environment configuration template
├── .gitattributes                     # Git LFS & line ending normalizations
├── .gitignore                         # Unity, Python, and local telemetry ignore rules
├── architecture.md                    # System architecture specification
├── technical.md                       # Technical implementation specification
└── README.md                          # Master project documentation
```

---

## 4. Outside-Unity Tooling & Workflow

All tools outside Unity run on standard Python 3.9+ without requiring complex external dependencies.

### 4.1 Telemetry Validation & Statistical Analytics (`analysis/`)

The experiment outputs trial telemetry to `ExperimentData.csv` at the project root.

```bash
# 1. Validate the CSV schema against technical.md specifications
python3 analysis/schema_validator.py ExperimentData.csv

# 2. Run statistical analysis (Chi-Square tests, latencies, gaze allocation)
python3 analysis/analyze_telemetry.py ExperimentData.csv

# 3. Generate a synthetic 30-participant test dataset if running outside Unity
python3 analysis/generate_sample_data.py --participants 30 --output analysis/sample_ExperimentData.csv

# 4. Execute the automated analytics test suite
python3 analysis/test_analysis_pipeline.py
```

#### Sample Analysis Output
```text
==============================================================================
                    SMART-EVAC EXPERIMENTAL TELEMETRY ANALYSIS
==============================================================================
Total Participants: 30   | Total Trials: 90  
------------------------------------------------------------------------------
UI Guidance Mode   | Safe Route %   | Crowd Trap %   | Dec. Latency (s) | Gaze UI Ratio
------------------------------------------------------------------------------
Static             | 30.0% (9/30)   | 66.7% (20/30)  | 3.49 ± 0.60      | 24.3%       
DynamicArrow       | 66.7% (20/30)  | 30.0% (9/30)   | 2.74 ± 0.53      | 51.0%       
AI_HUD             | 80.0% (24/30)  | 10.0% (3/30)   | 1.82 ± 0.42      | 75.9%       
------------------------------------------------------------------------------
Statistical Hypothesis Testing:
  • Safe Route Choice Dependence:   Chi2(2) = 16.61, p = 0.0003 *** Significant (p < 0.001)
  • Social Herding Trap Following: Chi2(2) = 21.63, p = 0.0001 *** Significant (p < 0.001)
==============================================================================
```

### 4.2 Audio Asset Synthesizer (`tools/audio/`)

Generates the audio assets required for Mode C in `assets_staging/audio/`:
```bash
python3 tools/audio/generate_audio_assets.py
```
- **Alert Chime**: Synthesizes an 800Hz $\rightarrow$ 1000Hz two-tone chime ($120\text{ ms}$) with anti-click envelope shaping.
- **Voice Cues**: Generates directional voice warnings (`Left`, `Center`, `Right`) via `ffmpeg`/`libflite`.

### 4.3 Gemini Flash API Pre-Fetch & Prompt Tester (`tools/gemini/`)

Tests the prompt payload and measures round-trip API latency outside Unity:
```bash
# Test with live Gemini Flash API
python3 tools/gemini/test_gemini_api.py --api-key YOUR_GEMINI_KEY

# Test offline fallback circuit breaker
python3 tools/gemini/test_gemini_api.py --offline
```

---

## 5. Experimenter Guide & Hotkeys

During Unity runtime, the experimenter controls the simulation state via keyboard hotkeys:

| Key | Function | Effect |
| :---: | :--- | :--- |
| `1` | **Mode A (Static Signs)** | Activates static EXIT signage (Control condition). |
| `2` | **Mode B (Dynamic Arrows)** | Activates glowing 3D floor arrows pointing to safe corridor. |
| `3` | **Mode C (Live AI HUD)** | Activates Gemini AI HUD warning text and audio triggers. |
| `R` | **Reset Current Trial** | Resets player, re-spawns crowd flock, and randomizes hazards. |
| `P` | **Next Participant** | Increments participant counter (`P001` $\rightarrow$ `P002`). |

Refer to [EXPERIMENT_PROTOCOL.md](file:///home/jyothipavanavik/unity_project_1/docs/EXPERIMENT_PROTOCOL.md) for the Latin-Square counterbalancing matrix and participant briefing scripts.

---

## 6. Git Branching & Contribution Workflow

This project follows a structured feature-branch workflow:
* `main`: Production-ready, verified experimental release.
* `develop`: Integration branch for active feature merges.
* `Avikshith`: Current active working branch for architectural tooling and experiment scaffolding.
* `feature/*`: Dedicated topic branches (e.g. `feature/core-mechanics`).

# Smart-Evac: Empirical Telemetry Analysis Report

- **Total Participants Evaluated**: 30
- **Total Experimental Trials**: 90
- **Independent Variable**: UI Guidance Mode (`Static`, `DynamicArrow`, `AI_HUD`)

## 1. Primary Behavioral Findings

| UI Guidance Mode | Total Trials | Chose Safe Path | Followed Crowd into Trap | Mean Latency (s) | Mean UI Gaze (s) | Mean Crowd Gaze (s) | Attention Ratio (UI%) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Static** | 30 | 30.0% (9) | 66.7% (20) | 3.49 ± 0.60 | 0.78s | 2.42s | 24.3% |
| **DynamicArrow** | 30 | 66.7% (20) | 30.0% (9) | 2.74 ± 0.53 | 1.54s | 1.54s | 51.0% |
| **AI_HUD** | 30 | 80.0% (24) | 10.0% (3) | 1.82 ± 0.42 | 2.18s | 0.68s | 75.9% |

## 2. Hypothesis Testing & Statistical Significance

1. **Safe Path Choice vs. UI Mode**: $\chi^2(2) = 16.61, p = 0.0003$.
   - *Result*: **Statistically Significant**. The guidance condition decisively influences whether participants evade hazard traps.
2. **Herding Behavior vs. UI Mode**: $\chi^2(2) = 21.63, p = 0.0001$.
   - *Result*: **Statistically Significant**. Generative AI HUD guidance significantly suppresses the tendency to follow panicking crowds.

## 3. Interpretation & Theoretical Implications

- **Mode A (Static Signs)** exhibits the highest rate of social herding, indicating that static emergency signage fails when conflicting human motion is observed.
- **Mode B (Dynamic Arrows)** provides clear spatial direction, lifting safe choices above chance, but retains moderate herding pull.
- **Mode C (AI HUD + Audio)** achieves superior compliance with lowest decision latency, validating the hypothesis that multimodal conversational AI overcomes social herding pressure.

#!/usr/bin/env python3
"""
generate_sample_data.py
-----------------------
Generates synthetic experimental telemetry adhering to the Smart-Evac schema.
Simulates realistic behavioral distributions across the three independent experimental conditions:
  - Mode A: Static Wall Signs (Control - high social herding susceptibility)
  - Mode B: Dynamic Glowing Floor Arrows (Spatial UI - moderate compliance)
  - Mode C: Live Gemini AI HUD + Audio Chime/Voice (Generative AI - high compliance)

Usage:
    python3 generate_sample_data.py [--participants 30] [--output ./sample_ExperimentData.csv]
"""

import os
import sys
import random
import argparse
import csv

PATHS = ["Left", "Center", "Right"]
MODES = ["Static", "DynamicArrow", "AI_HUD"]


def generate_dataset(num_participants: int = 30, output_path: str = None) -> str:
    if output_path is None:
        output_path = os.path.join(os.path.dirname(__file__), "sample_ExperimentData.csv")

    random.seed(42)  # Deterministic seed for reproducible testing

    rows = []
    # Latin square counterbalanced mode orderings to eliminate condition sequence bias
    latin_square = [
        ["Static", "DynamicArrow", "AI_HUD"],
        ["DynamicArrow", "AI_HUD", "Static"],
        ["AI_HUD", "Static", "DynamicArrow"],
    ]

    for p_idx in range(1, num_participants + 1):
        pid = f"P{p_idx:03d}"
        order = latin_square[(p_idx - 1) % len(latin_square)]

        for mode in order:
            # Procedurally assign safe corridor (1 safe, 2 traps)
            safe_path = random.choice(PATHS)
            trap_paths = [p for p in PATHS if p != safe_path]
            crowd_trap = random.choice(trap_paths)

            # Behavioral modeling based on experimental mode
            if mode == "Static":
                # Control condition: High vulnerability to panicking crowd
                # ~68% follow crowd into trap, 22% choose safe path, 10% other trap
                roll = random.random()
                if roll < 0.68:
                    chosen_path = crowd_trap
                    followed_crowd = True
                    chose_safe = False
                elif roll < 0.90:
                    chosen_path = safe_path
                    followed_crowd = False
                    chose_safe = True
                else:
                    other_trap = [p for p in trap_paths if p != crowd_trap][0]
                    chosen_path = other_trap
                    followed_crowd = False
                    chose_safe = False

                decision_latency = round(random.gauss(3.8, 0.7), 2)
                gaze_ui = round(max(0.2, random.gauss(0.8, 0.3)), 2)
                gaze_crowd = round(max(0.5, random.gauss(2.4, 0.5)), 2)
                total_time = round(decision_latency + random.gauss(8.5, 1.2), 2)

            elif mode == "DynamicArrow":
                # Spatial UI condition: Visual floor arrows overcome crowd ~64% of the time
                roll = random.random()
                if roll < 0.64:
                    chosen_path = safe_path
                    followed_crowd = False
                    chose_safe = True
                elif roll < 0.90:
                    chosen_path = crowd_trap
                    followed_crowd = True
                    chose_safe = False
                else:
                    other_trap = [p for p in trap_paths if p != crowd_trap][0]
                    chosen_path = other_trap
                    followed_crowd = False
                    chose_safe = False

                decision_latency = round(random.gauss(2.7, 0.5), 2)
                gaze_ui = round(max(0.3, random.gauss(1.6, 0.4)), 2)
                gaze_crowd = round(max(0.3, random.gauss(1.5, 0.4)), 2)
                total_time = round(decision_latency + random.gauss(7.8, 1.0), 2)

            else:  # AI_HUD
                # Mode C: Audio chime + voice prompt + HUD alert drives high trust (~88% safe compliance)
                roll = random.random()
                if roll < 0.88:
                    chosen_path = safe_path
                    followed_crowd = False
                    chose_safe = True
                elif roll < 0.96:
                    chosen_path = crowd_trap
                    followed_crowd = True
                    chose_safe = False
                else:
                    other_trap = [p for p in trap_paths if p != crowd_trap][0]
                    chosen_path = other_trap
                    followed_crowd = False
                    chose_safe = False

                decision_latency = round(random.gauss(1.9, 0.4), 2)
                gaze_ui = round(max(0.5, random.gauss(2.2, 0.4)), 2)
                gaze_crowd = round(max(0.1, random.gauss(0.7, 0.25)), 2)
                total_time = round(decision_latency + random.gauss(6.9, 0.8), 2)

            # Ensure bounds and sanity
            decision_latency = max(0.5, decision_latency)
            total_time = max(decision_latency + 2.0, total_time)

            rows.append({
                "Participant_ID": pid,
                "UI_Mode": mode,
                "Safe_Path_Assigned": safe_path,
                "Player_Chosen_Path": chosen_path,
                "Chose_Safe_Path": str(chose_safe),
                "Followed_Crowd_Into_Trap": str(followed_crowd),
                "Decision_Latency_Sec": f"{decision_latency:.2f}",
                "Gaze_Time_On_UI_Sec": f"{gaze_ui:.2f}",
                "Gaze_Time_On_Crowd_Sec": f"{gaze_crowd:.2f}",
                "Total_Evac_Time_Sec": f"{total_time:.2f}",
            })

    # Write CSV
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    fieldnames = [
        "Participant_ID",
        "UI_Mode",
        "Safe_Path_Assigned",
        "Player_Chosen_Path",
        "Chose_Safe_Path",
        "Followed_Crowd_Into_Trap",
        "Decision_Latency_Sec",
        "Gaze_Time_On_UI_Sec",
        "Gaze_Time_On_Crowd_Sec",
        "Total_Evac_Time_Sec",
    ]

    with open(output_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)

    print(f"[✓] Generated {len(rows)} simulated trials for {num_participants} participants.")
    print(f"[✓] File saved to: {output_path}")
    return output_path


def main():
    parser = argparse.ArgumentParser(description="Generate synthetic Smart-Evac telemetry data.")
    parser.add_argument("--participants", type=int, default=30, help="Number of participants to simulate (default: 30)")
    parser.add_argument("--output", type=str, default=None, help="Output CSV path")
    args = parser.parse_args()

    generate_dataset(num_participants=args.participants, output_path=args.output)


if __name__ == "__main__":
    main()

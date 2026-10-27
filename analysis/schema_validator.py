#!/usr/bin/env python3
"""
schema_validator.py
-------------------
Validates ExperimentData.csv telemetry logs against the Smart-Evac Technical Specification (Section 2).

Usage:
    python3 schema_validator.py [path/to/ExperimentData.csv]
"""

import sys
import os
import csv
import re
from typing import List, Dict, Any, Tuple

EXPECTED_COLUMNS = [
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

PERMITTED_UI_MODES = {"Static", "DynamicArrow", "AI_HUD"}
PERMITTED_PATHS = {"Left", "Center", "Right"}
BOOLEAN_STRINGS = {"True", "False"}


def parse_bool(val: str) -> bool:
    if val.strip() in ("True", "true", "1"):
        return True
    if val.strip() in ("False", "false", "0"):
        return False
    raise ValueError(f"Invalid boolean string: {val}")


def validate_csv(filepath: str) -> Tuple[bool, List[str], List[str]]:
    """
    Validates a CSV file against the Smart-Evac schema.
    Returns: (is_valid, errors, warnings)
    """
    errors: List[str] = []
    warnings: List[str] = []

    if not os.path.isfile(filepath):
        return False, [f"File not found: {filepath}"], []

    with open(filepath, "r", encoding="utf-8-sig") as f:
        reader = csv.reader(f)
        try:
            header = next(reader)
        except StopIteration:
            return False, ["CSV file is completely empty."], []

        # Validate header columns
        cleaned_header = [col.strip() for col in header]
        if cleaned_header != EXPECTED_COLUMNS:
            missing = set(EXPECTED_COLUMNS) - set(cleaned_header)
            extra = set(cleaned_header) - set(EXPECTED_COLUMNS)
            order_mismatch = (set(cleaned_header) == set(EXPECTED_COLUMNS)) and (cleaned_header != EXPECTED_COLUMNS)

            msg = "Header does not match schema specification."
            if missing:
                msg += f" Missing columns: {sorted(list(missing))}."
            if extra:
                msg += f" Unexpected columns: {sorted(list(extra))}."
            if order_mismatch:
                msg += " Column ordering differs from specification."
            errors.append(msg)

        row_index = 1
        participant_counts: Dict[str, int] = {}

        for row in reader:
            row_index += 1
            if not row or all(c.strip() == "" for c in row):
                warnings.append(f"Line {row_index}: Empty or blank row skipped.")
                continue

            if len(row) != len(EXPECTED_COLUMNS):
                errors.append(
                    f"Line {row_index}: Column count mismatch (expected {len(EXPECTED_COLUMNS)}, got {len(row)})."
                )
                continue

            row_dict = dict(zip(cleaned_header, [c.strip() for c in row]))

            # 1. Participant_ID
            pid = row_dict.get("Participant_ID", "")
            if not re.match(r"^P\d{3,}$", pid):
                warnings.append(f"Line {row_index}: Participant_ID '{pid}' does not follow 'P001' convention.")
            participant_counts[pid] = participant_counts.get(pid, 0) + 1

            # 2. UI_Mode
            mode = row_dict.get("UI_Mode", "")
            if mode not in PERMITTED_UI_MODES:
                errors.append(
                    f"Line {row_index}: Invalid UI_Mode '{mode}'. Permitted: {sorted(list(PERMITTED_UI_MODES))}."
                )

            # 3. Safe_Path_Assigned
            safe_path = row_dict.get("Safe_Path_Assigned", "")
            if safe_path not in PERMITTED_PATHS:
                errors.append(
                    f"Line {row_index}: Invalid Safe_Path_Assigned '{safe_path}'. Permitted: {sorted(list(PERMITTED_PATHS))}."
                )

            # 4. Player_Chosen_Path
            chosen_path = row_dict.get("Player_Chosen_Path", "")
            if chosen_path not in PERMITTED_PATHS:
                errors.append(
                    f"Line {row_index}: Invalid Player_Chosen_Path '{chosen_path}'. Permitted: {sorted(list(PERMITTED_PATHS))}."
                )

            # 5. Chose_Safe_Path
            chose_safe_str = row_dict.get("Chose_Safe_Path", "")
            if chose_safe_str not in BOOLEAN_STRINGS:
                errors.append(f"Line {row_index}: Chose_Safe_Path '{chose_safe_str}' is not True/False.")
            else:
                chose_safe = parse_bool(chose_safe_str)
                expected_safe = (chosen_path == safe_path)
                if chose_safe != expected_safe:
                    errors.append(
                        f"Line {row_index}: Logical inconsistency: Chose_Safe_Path is {chose_safe}, "
                        f"but Player_Chosen_Path ('{chosen_path}') == Safe_Path_Assigned ('{safe_path}') is {expected_safe}."
                    )

            # 6. Followed_Crowd_Into_Trap
            herded_str = row_dict.get("Followed_Crowd_Into_Trap", "")
            if herded_str not in BOOLEAN_STRINGS:
                errors.append(f"Line {row_index}: Followed_Crowd_Into_Trap '{herded_str}' is not True/False.")
            else:
                herded = parse_bool(herded_str)
                # If player chose safe path, they cannot have followed crowd into trap (since crowd goes to trap)
                if chose_safe_str in BOOLEAN_STRINGS and parse_bool(chose_safe_str) and herded:
                    errors.append(
                        f"Line {row_index}: Contradiction: Player marked as both Chose_Safe_Path=True and Followed_Crowd_Into_Trap=True."
                    )

            # 7-10. Numerical Timers
            numerical_fields = [
                ("Decision_Latency_Sec", 0.0),
                ("Gaze_Time_On_UI_Sec", 0.0),
                ("Gaze_Time_On_Crowd_Sec", 0.0),
                ("Total_Evac_Time_Sec", 0.0),
            ]

            parsed_nums: Dict[str, float] = {}
            for col_name, min_val in numerical_fields:
                val_str = row_dict.get(col_name, "")
                try:
                    val = float(val_str)
                    if val < min_val:
                        errors.append(f"Line {row_index}: {col_name} value {val} is less than {min_val}.")
                    parsed_nums[col_name] = val
                except ValueError:
                    errors.append(f"Line {row_index}: {col_name} value '{val_str}' is not a valid float.")

            # Sanity checks across timers
            if "Decision_Latency_Sec" in parsed_nums and "Total_Evac_Time_Sec" in parsed_nums:
                if parsed_nums["Decision_Latency_Sec"] > parsed_nums["Total_Evac_Time_Sec"]:
                    errors.append(
                        f"Line {row_index}: Decision_Latency_Sec ({parsed_nums['Decision_Latency_Sec']:.2f}s) "
                        f"cannot exceed Total_Evac_Time_Sec ({parsed_nums['Total_Evac_Time_Sec']:.2f}s)."
                    )

        if row_index == 1:
            warnings.append("CSV contains a valid header, but zero data rows.")

    is_valid = (len(errors) == 0)
    return is_valid, errors, warnings


def main():
    default_path = os.path.join(os.path.dirname(__file__), "..", "ExperimentData.csv")
    filepath = sys.argv[1] if len(sys.argv) > 1 else default_path

    print("=" * 65)
    print("  Smart-Evac Telemetry Schema Validator")
    print(f"  Target File: {filepath}")
    print("=" * 65)

    is_valid, errors, warnings = validate_csv(filepath)

    if warnings:
        print(f"\n[!] Warnings ({len(warnings)}):")
        for w in warnings[:15]:
            print(f"    - {w}")
        if len(warnings) > 15:
            print(f"    ... and {len(warnings) - 15} more warnings.")

    if not is_valid:
        print(f"\n[X] Validation FAILED with {len(errors)} error(s):")
        for e in errors[:25]:
            print(f"    - {e}")
        if len(errors) > 25:
            print(f"    ... and {len(errors) - 25} more errors.")
        sys.exit(1)
    else:
        print("\n[✓] Validation PASSED. Telemetry format strictly adheres to technical.md specification.")
        sys.exit(0)


if __name__ == "__main__":
    main()

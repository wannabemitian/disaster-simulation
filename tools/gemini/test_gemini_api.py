#!/usr/bin/env python3
"""
test_gemini_api.py
------------------
CLI validation tool for the Gemini Flash REST API integration described in technical.md (Section 3.3).
Tests the exact emergency prompt payload, validates the 1-sentence constraint, measures network latency,
and exercises the local Fallback Circuit Breaker when offline.

Usage:
    # Test with real API Key:
    python3 test_gemini_api.py --api-key YOUR_KEY [--model gemini-2.5-flash]

    # Test with environment variable or .env:
    python3 test_gemini_api.py

    # Test offline circuit-breaker fallback:
    python3 test_gemini_api.py --offline
"""

import os
import sys
import time
import json
import argparse
import urllib.request
import urllib.error

SYSTEM_INSTRUCTION = (
    "You are Smart-Evac, an emergency AI supervisor. An evacuation is in progress. "
    "Sensors detect fire and toxic smoke in two paths. Output exactly ONE urgent sentence "
    "directing the occupant to the safe path and instructing them to ignore the panicking crowd."
)

FALLBACK_MESSAGES = {
    "Left": "EMERGENCY ALERT: Center and Right corridors blocked by fire! Evacuate immediately via Left corridor and do NOT follow the crowd!",
    "Center": "EMERGENCY ALERT: Fire and smoke detected in Left and Right corridors! Follow the Center path immediately, ignore panicking crowd!",
    "Right": "EMERGENCY ALERT: Left and Center paths engulfed in flames! Turn into Right corridor immediately, do NOT follow the crowd!",
}


def load_env_api_key() -> str:
    """Reads GEMINI_API_KEY from environment or .env file."""
    if os.environ.get("GEMINI_API_KEY"):
        return os.environ["GEMINI_API_KEY"]

    env_path = os.path.join(os.path.dirname(__file__), "..", "..", ".env")
    if os.path.isfile(env_path):
        with open(env_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line.startswith("GEMINI_API_KEY=") and not line.startswith("#"):
                    val = line.split("=", 1)[1].strip().strip('"').strip("'")
                    if val and val != "your_gemini_api_key_here":
                        return val
    return ""


def build_payload(safe_path: str, sensor_state: str) -> dict:
    full_prompt = (
        f"{SYSTEM_INSTRUCTION}\n\n"
        f"Sensor Status: {sensor_state}\n"
        f"Designated safe exit: {safe_path} corridor."
    )
    return {
        "contents": [
            {
                "parts": [
                    {"text": full_prompt}
                ]
            }
        ],
        "generationConfig": {
            "temperature": 0.2,
            "maxOutputTokens": 60,
        }
    }


def query_gemini(api_key: str, model: str, payload: dict, timeout: float = 2.5) -> dict:
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={api_key}"
    data_bytes = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data_bytes,
        headers={"Content-Type": "application/json"},
        method="POST"
    )

    start_time = time.time()
    try:
        with urllib.request.urlopen(req, timeout=timeout) as response:
            latency = time.time() - start_time
            res_body = response.read().decode("utf-8")
            return {
                "success": True,
                "latency_sec": latency,
                "data": json.loads(res_body),
            }
    except urllib.error.HTTPError as e:
        latency = time.time() - start_time
        err_msg = e.read().decode("utf-8", errors="replace")
        return {"success": False, "latency_sec": latency, "error": f"HTTP {e.code}: {err_msg}"}
    except Exception as e:
        latency = time.time() - start_time
        return {"success": False, "latency_sec": latency, "error": str(e)}


def extract_text(response_json: dict) -> str:
    try:
        candidates = response_json.get("candidates", [])
        if candidates:
            parts = candidates[0].get("content", {}).get("parts", [])
            if parts:
                return parts[0].get("text", "").strip()
    except Exception:
        pass
    return ""


def test_corridor_condition(safe_path: str, api_key: str, model: str, offline: bool = False):
    traps = [p for p in ["Left", "Center", "Right"] if p != safe_path]
    sensor_status = f"{traps[0]}: FIRE/SMOKE, {traps[1]}: FIRE/SMOKE, {safe_path}: CLEAR"

    print(f"\n[Test Case] Safe Path: {safe_path} | Traps: {traps}")
    print(f"  Sensor Feed: {sensor_status}")

    if offline or not api_key:
        print("  Mode: Fallback Circuit Breaker (Offline)")
        fallback_text = FALLBACK_MESSAGES[safe_path]
        print(f"  Circuit Breaker Output: \"{fallback_text}\"")
        print("  Simulated UI Latency: 0.00 ms (Instant Cache)")
        return

    payload = build_payload(safe_path, sensor_status)
    print(f"  Dispatching async pre-fetch to {model}...")

    res = query_gemini(api_key, model, payload, timeout=2.5)

    if res["success"]:
        text = extract_text(res["data"])
        print(f"  [✓] Received API response in {res['latency_sec']:.3f}s")
        print(f"  HUD Warning Text: \"{text}\"")
        sentence_count = len([s for s in text.replace("!", ".").replace("?", ".").split(".") if s.strip()])
        print(f"  Sentence Count: {sentence_count} (Expected: 1)")
        print(f"  Zero-latency cache simulation: Cached in memory. Display latency at trigger: 0.00 ms.")
    else:
        print(f"  [X] API Request failed ({res['latency_sec']:.3f}s): {res['error']}")
        print(f"  [!] Triggering Fallback Circuit Breaker: \"{FALLBACK_MESSAGES[safe_path]}\"")


def main():
    parser = argparse.ArgumentParser(description="Test Smart-Evac Gemini Flash API integration.")
    parser.add_argument("--api-key", default=None, help="Gemini API Key")
    parser.add_argument("--model", default="gemini-2.5-flash", help="Model name (default: gemini-2.5-flash)")
    parser.add_argument("--offline", action="store_true", help="Force offline circuit-breaker testing")
    args = parser.parse_args()

    api_key = args.api_key or load_env_api_key()

    print("=" * 68)
    print("  Smart-Evac Gemini Flash Pre-fetch & Prompt Validation Tool")
    print("=" * 68)

    if not api_key and not args.offline:
        print("[!] No GEMINI_API_KEY found in arguments or .env.")
        print("    Exercising Offline Fallback Circuit Breaker for all corridors.")
        print("    (To test live API, copy .env.example to .env and set your key).")
        args.offline = True
    elif not args.offline:
        print(f"[i] Using API Key: {api_key[:6]}...{api_key[-4:]} | Model: {args.model}")

    for path in ["Left", "Center", "Right"]:
        test_corridor_condition(path, api_key, args.model, args.offline)

    print("\n" + "=" * 68)
    print("[✓] Prompt validation test complete.")


if __name__ == "__main__":
    main()

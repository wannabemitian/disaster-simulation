#!/usr/bin/env python3
"""
generate_audio_assets.py
------------------------
Procedurally synthesizes the Mode C audio assets specified in technical.md (Section 5):
  1. alert_chime.wav: 800Hz/1000Hz two-tone emergency alert chime (120 ms duration).
  2. Voice_EvacuateLeft.wav: Direct guidance voice cue for Left corridor.
  3. Voice_EvacuateCenter.wav: Direct guidance voice cue for Center corridor.
  4. Voice_EvacuateRight.wav: Direct guidance voice cue for Right corridor.

Outputs WAV files (PCM 16-bit, 44.1 kHz, mono) fully compatible with Unity AudioSource.

Usage:
    python3 generate_audio_assets.py [--output-dir ./assets_staging/audio]
"""

import os
import sys
import wave
import struct
import math
import subprocess
import shutil
import argparse


def generate_two_tone_chime(output_filepath: str, sample_rate: int = 44100):
    """
    Synthesizes an 800Hz -> 1000Hz two-tone chime totaling 120 ms.
    Includes an attack/decay envelope to eliminate clipping clicks in Unity.
    """
    duration_tone1 = 0.060  # 60 ms
    duration_tone2 = 0.060  # 60 ms
    freq1 = 800.0           # 800 Hz
    freq2 = 1000.0          # 1000 Hz
    total_duration = duration_tone1 + duration_tone2
    total_samples = int(total_duration * sample_rate)

    samples = []
    for i in range(total_samples):
        t = i / sample_rate
        if t < duration_tone1:
            freq = freq1
        else:
            freq = freq2

        # Envelope shaping: 5ms fade-in, 10ms fade-out
        envelope = 1.0
        if t < 0.005:
            envelope = t / 0.005
        elif t > (total_duration - 0.010):
            envelope = (total_duration - t) / 0.010

        value = math.sin(2.0 * math.pi * freq * t) * envelope
        int_sample = int(value * 32767.0 * 0.95)  # 95% full scale
        samples.append(int_sample)

    with wave.open(output_filepath, "wb") as wf:
        wf.setnchannels(1)  # Mono
        wf.setsampwidth(2)  # 16-bit PCM
        wf.setframerate(sample_rate)
        raw_bytes = struct.pack(f"<{len(samples)}h", *samples)
        wf.writeframes(raw_bytes)

    print(f"  [✓] Generated two-tone chime: {output_filepath} ({total_duration*1000:.0f}ms)")


def synthesize_voice_clip_flite(text: str, output_filepath: str) -> bool:
    """Attempts to synthesize voice clip using ffmpeg with libflite."""
    if not shutil.which("ffmpeg"):
        return False

    cmd = [
        "ffmpeg",
        "-f", "lavfi",
        "-i", f"flite=text='{text}':voice=kal16",
        "-ar", "44100",
        "-ac", "1",
        "-y",
        output_filepath
    ]
    try:
        res = subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        return res.returncode == 0
    except Exception:
        return False


def synthesize_synthetic_tone_cue(direction: str, output_filepath: str, sample_rate: int = 44100):
    """
    Fallback synthesis: Creates a distinctive frequency-modulated tone sequence
    representing the directional cue (Left: low-to-high, Center: steady chord, Right: high-to-low).
    """
    duration = 0.6  # 600 ms
    total_samples = int(duration * sample_rate)
    samples = []

    for i in range(total_samples):
        t = i / sample_rate
        if direction.lower() == "left":
            freq = 440.0 + (300.0 * (t / duration))
        elif direction.lower() == "right":
            freq = 740.0 - (300.0 * (t / duration))
        else:  # Center
            freq = 587.33  # D5 tone

        # Sine wave with smooth envelope
        envelope = math.sin(math.pi * (t / duration))
        val = math.sin(2.0 * math.pi * freq * t) * envelope
        samples.append(int(val * 32767.0 * 0.85))

    with wave.open(output_filepath, "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(sample_rate)
        raw_bytes = struct.pack(f"<{len(samples)}h", *samples)
        wf.writeframes(raw_bytes)


def main():
    parser = argparse.ArgumentParser(description="Synthesize audio assets for Smart-Evac Mode C.")
    parser.add_argument(
        "--output-dir",
        default=os.path.join(os.path.dirname(__file__), "..", "..", "assets_staging", "audio"),
        help="Directory to save generated .wav files",
    )
    args = parser.parse_args()

    out_dir = os.path.abspath(args.output_dir)
    os.makedirs(out_dir, exist_ok=True)

    print("=" * 65)
    print("  Smart-Evac Audio Asset Synthesizer")
    print(f"  Target Destination: {out_dir}")
    print("=" * 65)

    # 1. Alert chime
    chime_path = os.path.join(out_dir, "alert_chime.wav")
    generate_two_tone_chime(chime_path)

    # 2. Voice Clips (technical.md Section 5)
    voice_definitions = [
        ("Left", "Warning! Evacuate via Left corridor. Avoid the crowd.", "Voice_EvacuateLeft.wav"),
        ("Center", "Warning! Evacuate via Center corridor. Avoid the crowd.", "Voice_EvacuateCenter.wav"),
        ("Right", "Warning! Evacuate via Right corridor. Avoid the crowd.", "Voice_EvacuateRight.wav"),
    ]

    for direction, script_text, filename in voice_definitions:
        filepath = os.path.join(out_dir, filename)
        success = synthesize_voice_clip_flite(script_text, filepath)
        if success:
            print(f"  [✓] Synthesized spoken voice cue (flite): {filename}")
        else:
            synthesize_synthetic_tone_cue(direction, filepath)
            print(f"  [✓] Synthesized directional audio cue (pure wav): {filename}")

    print("\n[✓] Audio synthesis complete. Assets are ready for Unity import into Assets/Audio/.")


if __name__ == "__main__":
    main()

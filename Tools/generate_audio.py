#!/usr/bin/env python3
"""Generate the six indoor-range weapon/feedback WAVs and their Unity metadata.

Run with Python 3 from any working directory:
    python Tools/generate_audio.py

Only files under Assets/Audio, Assets/Audio.meta, and this script's explicit
output paths are written. Synthesis is deterministic and uses only the Python
standard library. Every output is mono, 16-bit PCM at 44,100 Hz.
"""

import math
from pathlib import Path
import random
import struct
import wave

SAMPLE_RATE = 44100
PROJECT_ROOT = Path(__file__).resolve().parents[1]
ASSETS_DIR = PROJECT_ROOT / "Assets"
OUTPUT_DIR = ASSETS_DIR / "Audio"
TAU = 2.0 * math.pi

# Fixed GUIDs make the generated Unity assets reproducible across runs.
FOLDER_GUID = "9e19b3c672cf4f2a8f455e46c0a4e831"
GUIDS = {
    "RifleShot.wav": "0b3e8694ad7c4f23a579f9bc3ee41841",
    "SidearmShot.wav": "61e29a4b3d5c4f889d0e5d11f422fe6c",
    "Reload.wav": "f28954ed3c7a4ac6984b278d9e51a701",
    "WeaponSwitch.wav": "385be4f0e1924d109824b5149f4d2a7e",
    "Hit.wav": "b452f8847d144b8991ea053b7a95ddc6",
    "Kill.wav": "774891af7e6c4e669d2b5b413bc5a90f",
}

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

# Unity 2021 AudioImporter serialization. PCM + decompress-on-load suits short
# effects; normalization is disabled so the conservative authored gains remain.
AUDIO_META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 6
  defaultSettings:
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  preloadAudioData: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def lowpass(samples, cutoff):
    """One-pole low-pass filter for soft, non-fatiguing noise spectra."""
    alpha = 1.0 - math.exp(-TAU * cutoff / SAMPLE_RATE)
    result = []
    previous = 0.0
    for sample in samples:
        previous += alpha * (sample - previous)
        result.append(previous)
    return result


def highpass(samples, cutoff):
    low = lowpass(samples, cutoff)
    return [sample - low_sample for sample, low_sample in zip(samples, low)]


def bandpass(samples, low_cutoff, high_cutoff):
    return highpass(lowpass(samples, high_cutoff), low_cutoff)


def envelope(time, attack, decay):
    if time < 0.0:
        return 0.0
    return (1.0 - math.exp(-time / attack)) * math.exp(-time / decay)


def add_room(samples, taps, cutoff=2600.0):
    """Add a few short, subdued indoor-range reflections (no long reverb tail)."""
    result = list(samples)
    reflected = lowpass(samples, cutoff)
    for delay_seconds, gain in taps:
        delay = int(round(delay_seconds * SAMPLE_RATE))
        for index in range(delay, len(result)):
            result[index] += gain * reflected[index - delay]
    return result


def finish(samples, target_peak):
    """Remove subsonic DC, fade the tail, and leave generous clipping headroom."""
    result = highpass(samples, 25.0)
    fade_length = min(int(round(0.018 * SAMPLE_RATE)), len(result) // 4)
    for offset in range(fade_length):
        index = len(result) - fade_length + offset
        gain = 0.5 + 0.5 * math.cos(math.pi * (offset + 1) / fade_length)
        result[index] *= gain
    result[-1] = 0.0

    peak = max(abs(sample) for sample in result)
    if peak == 0.0:
        raise ValueError("Cannot normalize a silent sound")
    scale = target_peak / peak
    return [sample * scale for sample in result]


def make_rifle_shot():
    """Explosive low-frequency rifle report with a restrained, short crack."""
    duration = 0.44
    count = int(round(duration * SAMPLE_RATE))
    rng = random.Random(7101)
    white = [rng.uniform(-1.0, 1.0) for _ in range(count)]
    body_noise = bandpass(white, 55.0, 3400.0)
    crack_noise = bandpass(white, 900.0, 6100.0)
    low_noise = lowpass(white, 480.0)

    samples = []
    body_phase = 0.0
    sub_phase = 0.0
    for index in range(count):
        time = index / SAMPLE_RATE
        body_frequency = 52.0 + 74.0 * math.exp(-time / 0.055)
        sub_frequency = 43.0 + 18.0 * math.exp(-time / 0.090)
        body_phase += TAU * body_frequency / SAMPLE_RATE
        sub_phase += TAU * sub_frequency / SAMPLE_RATE

        body = 0.95 * math.sin(body_phase) * envelope(time, 0.0007, 0.074)
        sub = 0.42 * math.sin(sub_phase) * envelope(time, 0.0011, 0.135)
        blast = 0.82 * body_noise[index] * envelope(time, 0.00035, 0.034)
        crack = 0.22 * crack_noise[index] * envelope(time, 0.00025, 0.008)
        texture = 0.24 * low_noise[index] * envelope(time, 0.0008, 0.090)
        samples.append(body + sub + blast + crack + texture)

    samples = add_room(samples, ((0.029, 0.15), (0.051, 0.09), (0.083, 0.045)))
    return finish(samples, 0.74)


def make_sidearm_shot():
    """Short, crisp sidearm report, with noise transients instead of a beep."""
    duration = 0.24
    count = int(round(duration * SAMPLE_RATE))
    rng = random.Random(7202)
    white = [rng.uniform(-1.0, 1.0) for _ in range(count)]
    crack_noise = bandpass(white, 620.0, 6800.0)
    middle_noise = bandpass(white, 90.0, 2400.0)

    samples = []
    phase = 0.0
    for index in range(count):
        time = index / SAMPLE_RATE
        frequency = 82.0 + 116.0 * math.exp(-time / 0.030)
        phase += TAU * frequency / SAMPLE_RATE
        body = 0.62 * math.sin(phase) * envelope(time, 0.00045, 0.043)
        crack = 0.82 * crack_noise[index] * envelope(time, 0.00022, 0.0105)
        middle = 0.40 * middle_noise[index] * envelope(time, 0.00035, 0.024)
        samples.append(body + crack + middle)

    samples = add_room(samples, ((0.024, 0.10), (0.049, 0.055), (0.076, 0.025)))
    return finish(samples, 0.66)


def make_reload():
    """Two distinct mechanical latches: magazine release, then magazine seat."""
    duration = 0.48
    count = int(round(duration * SAMPLE_RATE))
    rng = random.Random(7303)
    white = [rng.uniform(-1.0, 1.0) for _ in range(count)]
    noise = bandpass(white, 170.0, 4300.0)
    samples = [0.0] * count

    events = (
        # start, modes (frequency, amplitude, decay), noise strength
        (0.012, ((315.0, 0.72, 0.043), (925.0, 0.34, 0.025),
                 (1710.0, 0.15, 0.013)), 0.62),
        (0.262, ((235.0, 0.88, 0.061), (710.0, 0.39, 0.034),
                 (1320.0, 0.17, 0.018)), 0.72),
    )
    for start, modes, noise_strength in events:
        for index in range(count):
            time = index / SAMPLE_RATE - start
            if time < 0.0:
                continue
            attack = 1.0 - math.exp(-time / 0.00045)
            click = noise_strength * noise[index] * envelope(time, 0.0002, 0.0065)
            for frequency, amplitude, decay in modes:
                click += (amplitude * math.sin(TAU * frequency * time)
                          * math.exp(-time / decay) * attack)
            samples[index] += click

    samples = add_room(samples, ((0.031, 0.055), (0.058, 0.025)))
    return finish(samples, 0.54)


def make_weapon_switch():
    """A brief textured mechanical slide ending in a small receiver latch."""
    duration = 0.18
    count = int(round(duration * SAMPLE_RATE))
    rng = random.Random(7404)
    white = [rng.uniform(-1.0, 1.0) for _ in range(count)]
    slide_noise = bandpass(white, 320.0, 3900.0)
    latch_noise = bandpass(white, 180.0, 3000.0)
    samples = []

    slide_duration = 0.112
    for index in range(count):
        time = index / SAMPLE_RATE
        slide = 0.0
        if 0.0 <= time < slide_duration:
            gate = math.sin(math.pi * time / slide_duration) ** 0.8
            friction = 0.78 + 0.22 * math.sin(TAU * 53.0 * time)
            slide = 0.58 * slide_noise[index] * gate * friction
            slide += 0.10 * math.sin(TAU * 135.0 * time) * gate

        latch_time = time - 0.116
        latch = 0.0
        if latch_time >= 0.0:
            attack = 1.0 - math.exp(-latch_time / 0.00035)
            latch = 0.52 * latch_noise[index] * envelope(latch_time, 0.0002, 0.0045)
            latch += 0.62 * math.sin(TAU * 285.0 * latch_time) * math.exp(-latch_time / 0.024) * attack
            latch += 0.22 * math.sin(TAU * 790.0 * latch_time) * math.exp(-latch_time / 0.015) * attack
        samples.append(slide + latch)

    samples = add_room(samples, ((0.027, 0.035),))
    return finish(samples, 0.46)


def make_hit():
    """Short steel-target impact with quickly damped inharmonic resonances."""
    duration = 0.18
    count = int(round(duration * SAMPLE_RATE))
    rng = random.Random(7505)
    white = [rng.uniform(-1.0, 1.0) for _ in range(count)]
    noise = bandpass(white, 240.0, 5400.0)
    samples = []
    modes = (
        (185.0, 0.30, 0.030),
        (640.0, 0.72, 0.068),
        (973.0, 0.43, 0.050),
        (1517.0, 0.24, 0.033),
        (2243.0, 0.11, 0.020),
    )
    for index in range(count):
        time = index / SAMPLE_RATE
        attack = 1.0 - math.exp(-time / 0.0003)
        impact = 0.68 * noise[index] * envelope(time, 0.00018, 0.0045)
        for frequency, amplitude, decay in modes:
            impact += (amplitude * math.sin(TAU * frequency * time)
                       * math.exp(-time / decay) * attack)
        samples.append(impact)

    samples = add_room(samples, ((0.021, 0.065), (0.043, 0.030)))
    return finish(samples, 0.54)


def make_kill():
    """A clear two-stage rising confirmation, percussive rather than shrill."""
    duration = 0.36
    count = int(round(duration * SAMPLE_RATE))
    rng = random.Random(7606)
    white = [rng.uniform(-1.0, 1.0) for _ in range(count)]
    noise = bandpass(white, 300.0, 3400.0)
    samples = [0.0] * count
    notes = (
        (0.000, 392.00, 0.105, 0.95),
        (0.102, 587.33, 0.150, 1.10),
    )
    for start, frequency, decay, strength in notes:
        for index in range(count):
            time = index / SAMPLE_RATE - start
            if time < 0.0:
                continue
            attack = 1.0 - math.exp(-time / 0.0012)
            fundamental = (0.58 * math.sin(TAU * frequency * time)
                           * math.exp(-time / decay) * attack)
            lower = (0.22 * math.sin(TAU * frequency * 0.5 * time)
                     * math.exp(-time / (decay * 0.72)) * attack)
            harmonic = (0.17 * math.sin(TAU * frequency * 2.006 * time)
                        * math.exp(-time / 0.060) * attack)
            harmonic += (0.065 * math.sin(TAU * frequency * 3.97 * time)
                         * math.exp(-time / 0.032) * attack)
            tick = 0.18 * noise[index] * envelope(time, 0.00025, 0.004)
            samples[index] += strength * (fundamental + lower + harmonic + tick)

    samples = add_room(samples, ((0.033, 0.060), (0.061, 0.028)))
    return finish(samples, 0.60)


SOUNDS = (
    ("RifleShot.wav", make_rifle_shot),
    ("SidearmShot.wav", make_sidearm_shot),
    ("Reload.wav", make_reload),
    ("WeaponSwitch.wav", make_weapon_switch),
    ("Hit.wav", make_hit),
    ("Kill.wav", make_kill),
)


def write_text(path, content):
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write(content)


def write_wav(path, samples):
    frames = bytearray()
    for sample in samples:
        # Rounding and clamping guarantee signed 16-bit PCM without overflow.
        value = int(round(max(-1.0, min(1.0, sample)) * 32767.0))
        frames.extend(struct.pack("<h", value))
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(bytes(frames))


def main():
    OUTPUT_DIR.mkdir(parents=False, exist_ok=True)
    write_text(ASSETS_DIR / "Audio.meta", FOLDER_META.format(guid=FOLDER_GUID))

    for filename, synthesizer in SOUNDS:
        samples = synthesizer()
        write_wav(OUTPUT_DIR / filename, samples)
        write_text(OUTPUT_DIR / (filename + ".meta"), AUDIO_META.format(guid=GUIDS[filename]))
        peak = max(abs(sample) for sample in samples)
        print("{}: {:.3f} s, peak {:.3f} ({:.1f} dBFS)".format(
            filename, len(samples) / SAMPLE_RATE, peak, 20.0 * math.log10(peak)))


if __name__ == "__main__":
    main()

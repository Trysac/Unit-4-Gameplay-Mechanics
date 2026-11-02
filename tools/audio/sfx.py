"""Sound effect recipes for Ring Out. Each function returns a rendered Track."""

from synth import Track, note_freq, saw, square, triangle


def pulse(duty):
    return lambda p: square(p, duty)


def bump():
    """Two balls knock together: a short rubbery thud. The game scales it with the impact."""
    t = Track(0.2)
    t.add_tone(0, 0.06, 210, triangle, 0.55, freq_end=90, attack=0.001, release=0.05)
    t.add_noise(0, 0.04, 0.12, smoothing=0.6, decay_power=2.0, seed=1)
    return t


def hit():
    """A hard collision: a crack over a deep knock."""
    t = Track(0.35)
    t.add_noise(0, 0.08, 0.4, smoothing=0.15, decay_power=2.2, seed=2)
    t.add_tone(0, 0.12, 160, triangle, 0.6, freq_end=55, attack=0.001, release=0.08)
    return t


def knock():
    """The knockback power-up sends an enemy flying: a punchy boom with a zap."""
    t = Track(0.55)
    t.add_tone(0, 0.18, 120, triangle, 0.7, freq_end=40, attack=0.001, release=0.12)
    t.add_tone(0, 0.12, 900, pulse(0.25), 0.12, freq_end=1800, release=0.06)
    t.add_noise(0, 0.15, 0.35, smoothing=0.4, decay_power=2.0, seed=3)
    return t


def ring_out():
    """An enemy falls off: a falling whistle."""
    t = Track(0.9)
    t.add_tone(0, 0.7, 1300, triangle, 0.22, freq_end=260, release=0.15, vibrato=0.01)
    t.add_tone(0.05, 0.6, 650, pulse(0.125), 0.06, freq_end=130, release=0.15)
    return t


def fall():
    """The player falls: a long wailing drop."""
    t = Track(1.6)
    t.add_tone(0, 1.3, 700, saw, 0.2, freq_end=70, release=0.25, vibrato=0.03)
    t.add_tone(0, 1.3, 350, triangle, 0.3, freq_end=40, release=0.25)
    return t


def dash():
    """A dash: a quick rising whoosh."""
    t = Track(0.35)
    t.add_noise(0, 0.25, 0.32, smoothing=0.7, decay_power=1.2, seed=4)
    t.add_tone(0, 0.18, 220, triangle, 0.18, freq_end=520, release=0.08)
    return t


def smash():
    """The smash lands: a heavy ground slam with a rumble."""
    t = Track(1.2)
    t.add_tone(0, 0.5, 110, triangle, 0.8, freq_end=30, attack=0.001, release=0.4)
    t.add_noise(0, 0.9, 0.55, smoothing=0.8, decay_power=1.7, seed=5)
    t.add_noise(0, 0.12, 0.35, smoothing=0.1, decay_power=2.5, seed=6)
    return t


def rocket():
    """A rocket leaves: a fizzing hiss with a rising tone."""
    t = Track(0.45)
    t.add_noise(0, 0.35, 0.22, smoothing=0.3, decay_power=1.0, seed=7)
    t.add_tone(0, 0.3, 300, pulse(0.5), 0.08, freq_end=1200, release=0.05)
    return t


def rocket_hit():
    """A rocket hits: a small crunchy blast."""
    t = Track(0.5)
    t.add_noise(0, 0.35, 0.5, smoothing=0.55, decay_power=2.0, seed=8)
    t.add_tone(0, 0.2, 140, triangle, 0.45, freq_end=45, release=0.1)
    return t


def split():
    """A splitter breaks in two: a crack and two blips."""
    t = Track(0.45)
    t.add_noise(0, 0.06, 0.35, smoothing=0.1, decay_power=2.0, seed=9)
    t.add_tone(0.05, 0.05, note_freq("E6"), pulse(0.25), 0.16, release=0.04)
    t.add_tone(0.12, 0.05, note_freq("B5"), pulse(0.25), 0.16, release=0.06)
    return t


def boss_charge():
    """The boss winds up a charge: a low growl that rises."""
    t = Track(0.8)
    t.add_tone(0, 0.6, 55, saw, 0.3, freq_end=110, release=0.15, vibrato=0.05)
    t.add_tone(0, 0.6, 82, pulse(0.5), 0.12, freq_end=165, release=0.15)
    return t


def powerup():
    """A power-up is picked up: a bright rising arpeggio."""
    t = Track(0.6)
    for i, name in enumerate(["C5", "E5", "G5", "C6", "E6"]):
        t.add_tone(i * 0.05, 0.07, note_freq(name), pulse(0.25), 0.18, release=0.12)
    t.add_tone(0, 0.3, note_freq("C4"), triangle, 0.25, release=0.2)
    return t


def power_end():
    """A power-up runs out."""
    t = Track(0.35)
    t.add_tone(0, 0.08, note_freq("G5"), triangle, 0.28, release=0.04)
    t.add_tone(0.1, 0.12, note_freq("C5"), triangle, 0.28, release=0.1)
    return t


def chain():
    """A ring-out chain grows: three quick rising notes."""
    t = Track(0.4)
    for i, name in enumerate(["E6", "G#6", "B6"]):
        t.add_tone(i * 0.055, 0.06, note_freq(name), pulse(0.25), 0.18, release=0.08)
    return t


def wave_start():
    """A wave begins: a two-note horn."""
    t = Track(0.9)
    t.add_tone(0, 0.22, note_freq("E4"), saw, 0.2, release=0.05)
    t.add_tone(0, 0.22, note_freq("B4"), pulse(0.5), 0.1, release=0.05)
    t.add_tone(0.28, 0.4, note_freq("A4"), saw, 0.22, release=0.15)
    t.add_tone(0.28, 0.4, note_freq("E5"), pulse(0.5), 0.1, release=0.15)
    return t


def boss_wave():
    """A boss wave begins: a slow, low horn over a drum hit."""
    t = Track(1.6)
    t.add_tone(0, 0.6, note_freq("E2"), saw, 0.3, release=0.2)
    t.add_tone(0, 0.6, note_freq("B2"), pulse(0.5), 0.14, release=0.2)
    t.add_tone(0.7, 0.7, note_freq("D#2"), saw, 0.3, release=0.25)
    t.add_tone(0.7, 0.7, note_freq("A#2"), pulse(0.5), 0.14, release=0.25)
    t.add_tone(0, 0.25, 90, triangle, 0.6, freq_end=40, release=0.1)
    return t


def fight():
    """The countdown ends."""
    t = Track(0.45)
    t.add_tone(0, 0.08, note_freq("B5"), pulse(0.25), 0.2, release=0.03)
    t.add_tone(0.1, 0.2, note_freq("E6"), pulse(0.25), 0.22, release=0.12)
    return t


def wave_clear():
    """A wave is cleared: a short bright fanfare."""
    t = Track(1.0)
    for name, at, length in [("E5", 0.0, 0.08), ("G#5", 0.1, 0.08), ("B5", 0.2, 0.08), ("E6", 0.3, 0.4)]:
        t.add_tone(at, length, note_freq(name), pulse(0.25), 0.2, release=0.12)
    t.add_tone(0.3, 0.4, note_freq("E3"), triangle, 0.4, release=0.2)
    return t


def new_record():
    """A new best: a longer fanfare."""
    t = Track(1.6)
    for name, at, length in [("E5", 0.0, 0.1), ("G#5", 0.12, 0.1), ("B5", 0.24, 0.1),
                             ("E6", 0.36, 0.2), ("B5", 0.6, 0.1), ("E6", 0.72, 0.6)]:
        t.add_tone(at, length, note_freq(name), pulse(0.25), 0.22, release=0.15)
    t.add_tone(0.72, 0.6, note_freq("E3"), triangle, 0.45, release=0.3)
    return t


def game_over():
    """The run ends: a slow falling line."""
    t = Track(1.4)
    for name, at, length in [("B4", 0.0, 0.2), ("G4", 0.24, 0.2), ("E4", 0.48, 0.6)]:
        t.add_tone(at, length, note_freq(name), triangle, 0.35, release=0.2)
    t.add_tone(0.48, 0.6, note_freq("E2"), triangle, 0.4, release=0.3)
    return t


def move():
    """Menu cursor."""
    t = Track(0.06)
    t.add_tone(0, 0.02, 1100, pulse(0.5), 0.14, release=0.02)
    return t


def select():
    """Menu confirm."""
    t = Track(0.16)
    t.add_tone(0, 0.04, note_freq("B5"), pulse(0.25), 0.18, release=0.02)
    t.add_tone(0.05, 0.06, note_freq("E6"), pulse(0.25), 0.18, release=0.05)
    return t


def pause():
    """The game pauses or resumes."""
    t = Track(0.25)
    t.add_tone(0, 0.08, note_freq("E5"), triangle, 0.3, release=0.05)
    t.add_tone(0.09, 0.08, note_freq("B4"), triangle, 0.3, release=0.08)
    return t


EFFECTS = {
    "bump.ogg": bump,
    "hit.ogg": hit,
    "knock.ogg": knock,
    "ring_out.ogg": ring_out,
    "fall.ogg": fall,
    "dash.ogg": dash,
    "smash.ogg": smash,
    "rocket.ogg": rocket,
    "rocket_hit.ogg": rocket_hit,
    "split.ogg": split,
    "boss_charge.ogg": boss_charge,
    "powerup.ogg": powerup,
    "power_end.ogg": power_end,
    "chain.ogg": chain,
    "wave_start.ogg": wave_start,
    "boss_wave.ogg": boss_wave,
    "fight.ogg": fight,
    "wave_clear.ogg": wave_clear,
    "new_record.ogg": new_record,
    "game_over.ogg": game_over,
    "move.ogg": move,
    "select.ogg": select,
    "pause.ogg": pause,
}

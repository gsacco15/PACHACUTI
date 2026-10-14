# Pitch slice — implementation notes and questions for design

Building the slice surfaced places where the Pitch Slice Design Document is silent,
ambiguous, or contradicts itself. Each is implemented one way (so the slice plays end to end)
and listed here for a design decision. Numbers live in `Assets/Pachacuti/Scripts/Core/Tuning.cs`;
anything marked `ASSUMPTION` there is listed below.

## Findings that need a decision

1. **The slice night's sky (§8, §9, §12).** Computed for Cusco, 16 Nov 1532 (Julian calendar —
   it predates the Gregorian reform), 22:00 local mean time:
   - the Pleiades are **51° up in the north-north-east**, not "low in the east" (they were low in
     the east around 18:00–19:00);
   - **α and β Centauri — Yacana's eyes — are below the horizon** until about 02:15.
   Options: (a) keep 22:00 (implemented; the vision's llama rises from the south-east horizon
   "out of the Milky Way", which the vision may do — it is impossible on purpose); (b) move the
   night to ~04:30, when Yacana stands in the south-south-east and the Pleiades set in the
   west-north-west; (c) ~19:00 for "Pleiades low in the east", Yacana still down. The sky-map's
   llama is a map marker on the road and is unaffected. Verified by tests against Meeus.
2. **Quipu cord 2 (§7 table).** "Hundreds: 1 single over a thousands cluster of 1 / Tens: 2
   singles / Units: gap / Value 1,200" does not add up. Implemented from the value: thousands 1,
   hundreds 2, tens gap, units gap.
3. **The 6 m wall "tuned to need ~75 breath" (§5).** At the spec'd 1.2 m/s and 10/s drain a 6 m
   climb costs 50. Implemented as a per-surface grip difficulty: the Zone A wall drains ×1.5 →
   ~75, and a player who ran 4 s first runs out above the mid-wall ledge and falls back to it
   (tested). Alternatively lower the climb speed or raise the drain globally.
4. **"The tambo at Quispicanchi" on "the north road" (§8, §11).** Quispicanchi is south-east of
   Cusco, on the Collasuyu road; the north (Chinchaysuyu) road runs toward Anta / Jaquijahuana /
   Limatambo. Pillar 1 is "true enough to teach": rename the tambo or point the road south-east.
   Implemented as written.
5. **"Moves the army to the tambo" vs "the llama slides one node" (§8).** Implemented as: the
   order targets the tambo, the gold path lights to it, the llama moves one node (2 → 3) in 4 s,
   the tambo brightens ("they will be fed").
6. **Tupa fled (do-nothing branch) on the sky-map.** §8 specifies spared (a star lights) and
   killed (a star goes out) only. Fled: no change.
7. **"One lethal strike from behind" (§2).** Implemented generally: hold interact for 1 s
   behind an unaware soldier. No knockouts (GDD §7); a body seen by another soldier raises the
   alarm. §6 only scripts the strike on Tupa — confirm the general strike belongs in the slice.
8. **The hide branch's soldier (§6).** He stares at the load (+25), walks to it, kicks it for
   4 s, then returns to normal behaviour; when Manco picks the load up the torch soldier's
   suspicion becomes at least 60.
9. **The empty third cord (§7).** Not quizzed: Ninan says the line after a 2 s look. The Core
   lesson supports quizzing it (any answer is accepted) if wanted.
10. **"No text on screen except subtitles and two prompts" (§2).** Browsers need a click before
    audio and pointer lock, so the web build opens on black with a small "click". The F9
    facilitator toggle shows a one-line confirmation.
11. **Sky-map input without a mouse (§2 "every beat completable with keyboard only").** WASD /
    arrows move the sky cursor; Space or E select; E reads the cord.
12. **"The sky will read it before you can."** If the player never presses interact while the
    cord glows, it reads itself after 20 s (so no tester gets stuck on the one unprompted input).
13. **Engine.** Pitch §1 says the slice is "not reusable code"; with Unity it is the start of
    the production codebase (see `ENGINE_DECISION.md`). GDD §18's "choose Unity → commit to a
    stylized look" now applies.

## Tuning assumptions (not in the doc)

| Value | Used | Why |
|---|---|---|
| Lateral climb speed | 0.7 m/s | shuffle along a wall |
| Crawl speed (tunnel low crawl) | 0.8 m/s | |
| Walk (carrying) breath | +8/s | same as crouch-walk |
| "4 m+ costs a full breath" | meter → 0, 2 s gasp | read literally |
| Soldier vertical sight | ±35° | soldiers don't look straight up at roofs |
| Silent step-down | < 0.75 m | stepping off a curb is silent |
| Noise radii | load drop 10 m, scrape 8 m, strike 4 m | "loud", "audible" |
| Glance / stare cooldowns | 3 s / 2 s | stops a glance every frame |
| Approach give-up | 4 s out of sight | |
| Capture "on touch" | 1.0 m | |
| Soldier speeds | walk 1.5, chase 5.4 m/s | chase slightly faster than Manco's 5.0 run |
| Porter line | 1.2 m/s, 2.6 m spacing, lane ±1 m | the line sets Manco's pace |
| Sky hover voice lines | once per target per 6 s | no spam |
| "Look up" | camera pitched ≥35° up | |
| Reduced-flash strobe | 3 Hz cap | the 12 Hz cap is the normal option |
| Camera "wide" | 4.5 m | Coricancha roof |

## Simplifications in the grey-box

- The Coricancha's curved wall is a flat climbable face; the curve is an art task.
- People are capsules with procedural posture (glance = head turn, stare = face, torch raised,
  head down, crouch, lying); no animation clips yet. Soldiers navigate in straight lines.
- All audio is synthesized at startup (breath, wind, pututu, quena, drones, choir pad, sub-bass,
  grinding, water, harquebus). Soldier barks are subtitles plus a synthesized shout.
- The Milky Way is procedural (galactic-coordinate brightness with the dark clouds), standing in
  for the painted dome texture.
- Qenqo is built at boot rather than streamed; the tunnel still hides the cut.
- Ceque bearings follow Cobo's per-quarter counts (9/9/9/14) with even spacing; one runs exactly
  through Qenqo. Surveyed bearings should come from the archaeology consultant.

## Data and licences

- Stars: HYG v4.1 bright-star extract, CC BY-SA 4.0 (the extracted data file only). If legal
  prefers, regenerate from the Yale Bright Star Catalogue with `tools/build_star_catalog.py`
  adapted to its columns.
- Font: Spectral, SIL OFL 1.1.

## Next steps

1. Open in Unity 6.3 LTS, run both test suites, fix whatever the Editor reports first.
2. Play the 20 minutes on keyboard and gamepad; tune breath and suspicion numbers by feel (week 2–4).
3. Decide items 1–13 above.
4. Art handoff: wall kit and Manco (week 3) — set up Git LFS first.
5. Add a photosensitivity notice before the vision for public builds.

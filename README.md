# PACHACUTI

*You are Manco, the prince nobody counted.* A historical action-strategy game set in the
collapse of the Inca Empire, where the empire's own cosmology — three stacked worlds, a sky
that is a map, stones that remember — is the visual language.

This repository is the **Unity project for the Phase 0 pitch slice**: one Cusco district,
one night, five beats, twenty minutes, browser-playable from a link
(*PACHACUTI — Pitch Slice Design Document*). It is built so the same project grows into
the PC game (*PACHACUTI — Game Design Document*).

> **Engine: Unity 6.3 LTS (URP).** The project moved off Unreal Engine 5 / the Three.js
> throwaway plan — see [docs/ENGINE_DECISION.md](docs/ENGINE_DECISION.md) and
> [docs/TECH_PLAN_UNITY.md](docs/TECH_PLAN_UNITY.md).

## Status — week 1 of the 10-week build plan, plus early systems

The whole 20 minutes is grey-boxed end to end from primitives (§14 week 1: "Primitives for
all seven zones, the beat FSM, placeholder Manco capsule, subtitles"), and the systems the
later weeks call for already exist in first-pass form:

| Beat | What is in |
|---|---|
| Cold open | Black, breathing, fade up on Manco on a roof in fog, a torch column below, pututu, subtitle |
| 1 Rooftops | Three roofs, two context jumps, the 6 m masonry wall with a mid ledge, hang-drops into the compound, hidden breath meter (audio, sway, shaking hands), noise and torchlight stealth, a torch patrol below |
| Compound | The woman hands Manco the load; the porter line forms at the door |
| 2 Street | Hatunrumiyoc with four doorways, the twelve-angled stone, a 3-soldier checkpoint and 2 patrols running the §6 suspicion ladder; the line sets your pace; Tupa's strike / hide / say-the-name branches; capture → reset to the compound door |
| 3 Yard | Ninan's lesson on a real 3D quipu (340, 1,200, and the empty cord), dial input, three tries, never a fail state |
| Tunnel | Drain, grate with a soldier's torch above, low crawl, out at the Coricancha |
| 4 Roof | 15 m climb with a ledge; *look up*; the sky-map over the **real sky for Cusco, 16 Nov 1532** (2,000 stars, precessed); smudged llama → cord reads → thread → order to the tambo → Tupa's star or a star going out |
| 5 Qenqo | 30 s walk on a rail; the touch; the 90 s vision shot list: the one-frame rig swap, the channel, 41 ceque threads, Yacana walking, blocks rising, the time-fold strobe (12 Hz cap, reduced-flash option), Cajamarca, the gold room, the return |
| Title | PACHACUTI — *Cusco, 1532. He was sixteen.* Playtest log printed to the console |

Placeholder everything visual and audible: primitives, procedural textures, synthesized
audio, IMGUI subtitles. Art, animation, VO and the LUTs come from people (§13).

**Verification so far.** The engine-free rules (`Pachacuti.Core`) have 77 unit tests that
pass headless. All runtime, editor and test C# compiles against Unity reference assemblies
plus signature-checked package stubs. **The project has not yet been opened in the Unity
Editor** — the build container has no Unity licence. Expect a first-open pass: see
[First open](#first-open).

## First open

1. Install **Unity Hub** and **Unity 6.3 LTS** (any `6000.3.x`) with *Web Build Support*
   (older Hubs call it *WebGL Build Support*). PC builds on a Windows machine need nothing
   extra. If Hub asks which editor to use, pick your 6.3 LTS.
2. *Add project from disk* → this folder. The first import downloads URP 17.3, the Input
   System and the Test Framework, and takes a few minutes.
3. Menu **PACHACUTI → Play Slice** (Ctrl/Cmd+Shift+P), or open
   `Assets/Pachacuti/Scenes/Boot.unity` and press Play. Everything is built from code by
   `SliceDirector`; the scene contains nothing else.
4. Run **Window → General → Test Runner**: EditMode (77 Core tests) and PlayMode (boot,
   every beat reaches the title, the Zone A climb, falling back to the ledge, the gap jump,
   the kinsman hide branch).
5. Commit anything Unity regenerates on first open (e.g. `Packages/packages-lock.json`,
   `Assets/UniversalRenderPipelineGlobalSettings.asset`).

If something is pink, check *Project Settings → Graphics* has `PachacutiURP` assigned
(**PACHACUTI → Validate Project Setup** fixes it).

### On a laptop (e.g. a 16 GB Surface)

The slice is budgeted for an integrated-GPU laptop at 1080p (§13), and the project is small
(primitives, no imported art), so 16 GB is enough for the Editor, Play mode and builds.

- Plug in and set Windows' power mode to *Best performance*; laptops throttle on battery.
- In the Editor, set the Game view to **1920×1080**. A high-DPI panel at its native size
  (a Surface is ~3000×2000) draws about three times the pixels. Builds cap the 3D render at
  1080 lines themselves (`PostFx`); subtitles stay sharp.
- With a discrete GPU (Surface Book 2/3 keyboard base, Surface Laptop Studio): *Settings →
  System → Display → Graphics* → set Unity and your browser to *High performance*. On a
  Surface Book, keep the screen docked while Unity runs: the GPU is in the base.
- Close other heavy apps while building. The first open (package import, shader compile) and
  the first Web build (IL2CPP → WebAssembly) take many minutes; later ones are much quicker.

## Controls (§10)

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Run | Shift (hold) | Left-stick click / LT |
| Crouch | Ctrl (toggle) | B / Circle |
| Climb / jump / drop (context) | Space | A / Cross |
| Interact · strike (hold) · hide (tap) | E | X / Square |
| Drop load | Q | Y / Triangle |
| Sky-map select | Click (cursor: mouse, or WASD/arrows) | A / Cross |
| Quipu dial | 0–9; ↑↓ choose a knot cluster, ←→ turn the dial; E confirms | D-pad |

Facilitator keys, never mentioned to testers: **F1** debug overlay (beat, breath, each
soldier's state and suspicion), **F8** skip to the next beat, **F9** reduced-flash toggle.
On the web build `?reducedflash=1` turns it on from the link.

## Build and share a link

**PACHACUTI → Build → WebGL (browser link)** writes `Builds/WebGL` (Brotli with decompression
fallback, so it works on itch.io, Netlify and GitHub Pages alike) and logs the size against
the 60 MB budget. Upload the folder's contents. Batch mode / CI:

```
Unity -batchmode -quit -projectPath . -executeMethod Pachacuti.EditorTools.SliceBuild.WebGL
```

The *WebGL build & deploy* GitHub workflow (manual trigger) does the same with GameCI and
publishes to GitHub Pages once the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD`
secrets are set. **PACHACUTI → Build → Windows (PC)** builds the desktop player from the same project.

At the end of a run the build prints `[PACHACUTI] PLAYTEST {…}` to the console (the
browser console on WebGL): time per beat, street resets, Tupa outcome, quipu tries per cord,
time from the roof tilt to the first sky interaction, whether the llama was hovered before
the cord arrived, camera-yaw samples during the vision, total time (§15).

## Repository layout

```
Assets/Pachacuti/
  Scenes/Boot.unity            the only scene: one SliceDirector
  Scripts/Core/                engine-free rules (no UnityEngine): Tuning, breath, stealth,
                               suspicion FSM, kinsman, quipu, sky-map, beats, world state,
                               astronomy, vision timeline, playtest log
  Scripts/Runtime/             Unity side: SliceDirector (+ .Beats), Player, Camera, NPC,
                               World (grey-box builders, layout), Sky, Quipu, Vision, Audio, UI
  Scripts/Editor/              project setup, build menu/CLI, model import rules
  Tests/EditMode/              Core tests (also run headless via dotnet)
  Tests/PlayMode/              in-engine smoke tests
  Resources/Pachacuti/         shaders, material template, font, star catalog
  Settings/                    URP pipeline + renderer assets
docs/                          engine decision, Unity tech plan, implementation notes
tools/CoreTests/               dotnet test harness for Core
tools/UnityCompileCheck/       licence-free C# type-check of runtime/editor/tests
tools/gen_meta.py              deterministic .meta files for assets authored outside Unity
tools/build_star_catalog.py    regenerates the star catalog from HYG
```

Headless checks (no Unity needed):

```
dotnet test  tools/CoreTests
dotnet build tools/UnityCompileCheck
dotnet build tools/UnityCompileCheck -p:UnityEditor=true
```

## Credits and licences

- Spectral typeface © The Spectral Project Authors, SIL Open Font License 1.1
  (`Assets/Pachacuti/Resources/Pachacuti/Fonts/Spectral_OFL.txt`).
- Star positions from the HYG Database v4.1 (astronexus), CC BY-SA 4.0
  (`Assets/Pachacuti/Resources/Pachacuti/Sky/bright_stars_LICENSE.txt`).
- Positional astronomy after J. Meeus, *Astronomical Algorithms* (2nd ed.).

# Tech plan (Unity) — replaces Pitch Slice §13

Same goals as §13 — static-hosted, no backend, ships as a link, runs on an integrated-GPU
laptop — built in Unity so the code carries into the PC game.

## Stack

| Layer | §13 (Three.js) | Unity |
|---|---|---|
| Engine / renderer | Three.js, WebGL2 | Unity 6.3 LTS, URP 17.3 (forward), WebGL2 in browsers |
| Build | Vite, vanilla TypeScript | Unity WebGL (IL2CPP → WebAssembly), C# 9 / .NET Standard 2.1 |
| Physics | Rapier capsule + static colliders | PhysX `CharacterController` + box/mesh colliders |
| Animation | AnimationMixer, GLB clips | Mecanim (Animator) from week 3; grey-box poses are procedural (`Humanoid`) |
| Assets | GLB via gltf-transform, Draco/Meshopt, KTX2 | FBX (native) with mesh compression; ASTC/DXT per platform (Unity compresses textures per build target). GLB possible via `com.unity.cloud.gltfast` |
| Audio | Howler / Web Audio bus mixer | `AudioSource`s + `AudioDirector` (ambience layers, breath, ducking); an AudioMixer when real audio lands |
| Stars | star catalog JSON + astronomy-engine | HYG bright-star extract (CSV) + `Pachacuti.Core.Astronomy` (Meeus: Julian calendar, proper motion, precession, alt/az) — tested |
| Post | EffectComposer: bloom (vision only), vignette, grain, LUT per rig | URP Volumes: Night / Vision / Day profiles swapped by weight in one frame; LUTs via Color Lookup when the colourist delivers them |
| UI | DOM/canvas | IMGUI (`Overlay`): subtitles, two prompts, quipu numerals, sky cursor. UI Toolkit when UI art exists |
| Hosting | itch.io / Netlify | Vercel (deploys the `webgl-build` branch that CI pushes). Brotli + decompression fallback also covers itch.io, Netlify and GitHub Pages |

## Architecture (§13 file plan → this project)

| §13 | Here |
|---|---|
| `main.ts` boot, loop, scene switch | `Runtime/SliceDirector.cs` (+ `SliceDirector.Beats.cs`) |
| `core/input.ts` | `Runtime/Services/GameInput.cs` (Input System, keyboard/mouse + gamepad, one context action per key) |
| `core/audio.ts` | `Runtime/Audio/AudioDirector.cs`, `Synth.cs` (placeholder sound design) |
| `core/state.ts` — ONE plain object | `Core/Story/WorldState.cs` (serializes to JSON at the end) |
| `core/fsm.ts` | `Core/Story/BeatMachine.cs` |
| `world/level.ts` (loads district GLB, colliders, climbable by material name) | `Runtime/World/DistrictBuilder.cs`, `QenqoBuilder.cs`, `Layout.cs` (grey-box from code); `Editor/ModelImportRules.cs` tags `inca_masonry_*` meshes climbable when art arrives |
| `world/climb.ts` | `Runtime/Player/Manco.cs` + `World/ClimbSurface.cs`, `TraversalLink.cs`; numbers in `Core/Tuning.cs`, `Core/Breath/BreathModel.cs` |
| `world/stealth.ts` | `Core/Stealth/StealthRules.cs`, `Runtime/Stealth/Torch.cs` (light radius, noise bus, ≤8-light budget) |
| `world/npc/soldier.ts` | `Core/Stealth/SoldierBrain.cs` (the §6 table) + `Runtime/NPC/Soldier.cs` (senses, acts it out) |
| `world/npc/scripted.ts` | beat coroutines in `SliceDirector.Beats.cs`; `Core/Story/KinsmanEncounter.cs` |
| `quipu/cord.ts` | `Core/Quipu/*` (encoding, lesson) + `Runtime/Quipu/QuipuView.cs` |
| `sky/skymap.ts` | `Core/Sky/SkyMapModel.cs` + `Runtime/Sky/SkyDome.cs`, `SkyMapView.cs` |
| `vision/qenqo.ts` | `Core/Vision/VisionTimeline.cs`, `CequeLayout.cs` + `Runtime/Vision/VisionDirector.cs` |
| `ui/subtitles.ts, prompts.ts` | `Runtime/UI/Overlay.cs` |

**Rule kept from GDD §18:** rules live in `Pachacuti.Core` with `noEngineReferences` and are
tested headless (`dotnet test tools/CoreTests`); the Unity layer senses the scene, calls Core,
and acts the result out. The full game's strategy sim goes in Core the same way.

## Performance budget (§13, 1080p, integrated GPU) and how it is held

| Item | Budget | How |
|---|---|---|
| Draw calls | ≤300 street, ≤150 roof, ≤200 vision | Static batching of the district at boot; SRP Batcher; combined meshes for stars, crowds, threads |
| Triangles | ≤600k | Primitives now; LODs on the hero kit |
| Textures | ≤120 MB VRAM | Procedural 256² surface maps now; compressed textures per target later |
| Lights | ≤8 point lights + 1 directional | `Torch.ApplyBudget` keeps the nearest 8; URP asset: 8 per-object lights, no additional-light shadows |
| Shadows | one cascade, moon, 2k | URP asset: 1 cascade, 2048, 60 m. **Moon shadows off in the grey-box** until the art pass (the first web build lacked the shadowed Lit variant) |
| Frame | 16 ms (vision 20 ms with bloom) | Bloom only in the Vision profile; MSAA 2× in the browser quality level |
| Resolution | 1080p | Browsers render at the device pixel ratio (~3000×2000 on a Surface); `PostFx` sets the URP render scale so the 3D view is ≤1080 lines in builds (`Tuning.RenderMaxHeight`); the UI stays native. In the Editor, set the Game view to 1920×1080 |
| Download | <60 MB | Reported by `SliceBuild` after every build |

Quality levels: **Browser** (default for WebGL) and **Desktop**.

## Who builds what (§13, unchanged in spirit)

- **Claude Code:** Core rules, all runtime systems, the grey-box, editor/build tooling, tests,
  the playtest logger, CI.
- **People:** hero meshes (wall kit, Qenqo, Manco), animation, audio/VO, LUTs, every
  "does it feel right" pass — and the first open in the Unity Editor.

## Asset pipeline

- Art goes under `Assets/Pachacuti/Art/`. `ModelImportRules` adds colliders, compresses meshes and
  puts any mesh using an `inca_masonry_*` material on the **Climbable** layer.
- Climbing currently uses authored `ClimbSurface` rectangles on grey-box faces. When the wall kit
  lands (week 4), generate ClimbSurfaces from the kit's masonry faces (or move to raycast grips on
  the Climbable layer) — the breath/ledge/mantle rules in `Manco` stay the same.
- Turn on **Git LFS** for `*.fbx, *.png, *.psd, *.wav, *.ogg` before the first art commit.
- Assets authored outside the Editor need `.meta` files: `python3 tools/gen_meta.py`.

## Build plan status (§14)

| Week | Plan | Status |
|---|---|---|
| 1 | Grey-box everything, beat FSM, placeholder Manco, subtitles | **Done in code**, pending first Editor run |
| 2 | Climb and breath tuned on the Zone A wall; fog, moon, one torch | Systems in; **tuning needs hands on a controller** |
| 3 | Soldiers and the street: suspicion FSM, light volumes, noise, chase/reset, porter line | First pass in |
| 4 | Tupa and the choice | First pass in (strike / hide / name) |
| 5 | The quipu | First pass in; test on 5 people who have never seen a quipu |
| 6 | The sky | First pass in (real sky, four interactions) |
| 7 | The vision | Shot list blocked in with the rig swap; needs the polish pass and audio |
| 8–10 | Art/audio pass, playtest, publisher cut | Not started |

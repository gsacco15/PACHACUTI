# PACHACUTI — notes for Claude Code

Unity 6.3 LTS (URP) project for the PACHACUTI pitch slice. Design sources: the Pitch Slice
Design Document and the Game Design Document (cite sections as §N in code comments, as the
existing code does). Engine rationale: `docs/ENGINE_DECISION.md`; Unity mapping of the slice
tech plan: `docs/TECH_PLAN_UNITY.md`; open design questions: `docs/SLICE_IMPLEMENTATION_NOTES.md`.
The work list from grey-box to a finished look and feel, in order: `docs/REAL_GAME_ROADMAP.md`.

## Rules of the codebase

- **Rules go in `Assets/Pachacuti/Scripts/Core`** (`Pachacuti.Core`, `noEngineReferences: true`):
  no `UnityEngine`, C# 9, testable headless. The Unity layer (`Scripts/Runtime`) senses the scene,
  calls Core, and acts the result out.
- **Every tuning number lives in `Core/Tuning.cs`** with its § reference. Numbers not in the
  design doc are marked `ASSUMPTION` and listed in `docs/SLICE_IMPLEMENTATION_NOTES.md`.
- **One MonoBehaviour per file, file name = class name** (Unity requirement).
- The grey-box is built from code: `World/Layout.cs` holds coordinates (+Z north, +X east,
  metres, ground y = 0); `DistrictBuilder` / `QenqoBuilder` build from it. Keep beats and builders
  reading the same `Layout` constants.
- Climbable = a `ClimbSurface` on a masonry block (grey-box) or the `Climbable` layer via
  `inca_masonry_*` material names (art, see `Editor/ModelImportRules.cs`).
- No on-screen text beyond subtitles, the two control prompts, the quipu numerals and the sky
  cursor (§10, §12). Facilitator-only keys: F1 debug, F8 skip beat, F9 reduced flash.
- Shaders and materials loaded at runtime must live under `Resources/Pachacuti/` or they are
  stripped from builds.

## Before committing

```
dotnet test  tools/CoreTests                                   # Core unit tests (must pass)
dotnet build tools/UnityCompileCheck                           # runtime type-check
dotnet build tools/UnityCompileCheck -p:UnityEditor=true       # + editor scripts and PlayMode tests
python3 tools/gen_meta.py                                      # .meta for any new asset/folder
```

The compile check uses Unity 2021.3 reference assemblies plus `tools/UnityCompileCheck/Stubs`
(Input System and URP members, copied from the package sources at the pinned versions). If you
use a new package API, add it to the stubs from the real source, never from memory. APIs newer
than 2021.3 show as false errors; prefer long-stable APIs. The Unity Editor is the real check —
PlayMode tests in `Tests/PlayMode` drive the slice through `SliceDirector.DebugInput` /
`RequestSkip()`.

Never hand-edit GUIDs: `tools/gen_meta.py` derives them from the asset path, and
`Scenes/Boot.unity` references `SliceDirector.cs` by that GUID.

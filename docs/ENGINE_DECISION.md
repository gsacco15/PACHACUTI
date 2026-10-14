# Engine decision: Unity

**Decision (Oct 2026): Unity is the primary development engine for PACHACUTI**, for the pitch
slice and the game. Target: **Unity 6.3 LTS** with the **Universal Render Pipeline (URP)**.

This supersedes GDD §18 ("Unreal Engine 5 for the shipped game; a Three.js web vertical slice
for the pitch; Unity only if the chosen studio is a Unity shop") and Pitch Slice §13 ("build the
slice in Three.js … nothing is reused in the shipped game").

## Why

1. **Browser compatibility.** Unity builds natively for the web (WebGL/WebAssembly), so players
   open the slice from a link with nothing to install. UE5 needs GPU Pixel Streaming for the
   browser — a server per concurrent player.
2. **Hardware.** Development starts on an older Surface Book (16 GB RAM, limited GPU). Unity
   and URP are much lighter to run and iterate in than UE5.
3. **Scalability.** One project grows from a lightweight browser demo into a higher-fidelity
   PC game: same scenes, systems and code, with per-platform quality levels.
4. **Less rework.** Building in Unity now avoids writing the gameplay twice (Three.js prototype,
   then a production engine).
5. **Visual quality.** Unity supports detailed 3D environments, realistic lighting, character
   animation and large historical settings — with more optimization and custom work than UE5
   would need for the same look (see risks).

## What changes in the design documents

| Document | Was | Now |
|---|---|---|
| GDD §18 | UE5 shipped game; Three.js pitch slice | Unity 6.3 LTS + URP for both |
| GDD §18 "Why not Unity" | stylized look if Unity | **Commit to a stylized look** (the GDD's own condition for choosing Unity); answers GDD §19 open question "photoreal or stylized" and Pitch §16 open question 1 |
| Pitch §1 "What the slice is not" | "Not reusable code. Everything here can be thrown away." | The slice **is** the start of the production codebase. Code quality bar rises: engine-free rules in `Pachacuti.Core` with tests, systems behind clean seams. Grey-box *content* is still throwaway. |
| Pitch §2 hard constraints | "Single HTML entry point, Three.js, no backend, static files" | Unity WebGL build: an `index.html` + `Build/` folder, no backend, static files on itch.io / Netlify / GitHub Pages. Under 60 MB stays. |
| Pitch §13 | Three.js stack and `src/*.ts` architecture | Unity equivalents: [TECH_PLAN_UNITY.md](TECH_PLAN_UNITY.md) |
| GDD §18 architecture | "plain C++/C# … tested headless" | Kept, in C#: `Pachacuti.Core` has no engine references and runs its tests under plain .NET |

## Risks accepted, and mitigations

| Risk | Mitigation |
|---|---|
| No Nanite/Lumen: megalith-scale geometry and the vision's light need bespoke work | Stylized art direction; LODs and baked lighting for hero sites; the vision is emissive + bloom + a rig swap (already how the slice is built) |
| WebGL limits: memory, no multithreading by default, shader-variant size, mobile browsers | 60 MB budget enforced at build; one URP asset tuned for integrated GPUs; desktop browsers only for the slice (§2) |
| Large crowds and the 1536 siege (GDD §18 risk) | DOTS/Entities or GPU-instanced crowds when Act III is scoped; the Cajamarca shot already uses one combined mesh for 3,000 figures |
| Fewer AAA historical-action studios on Unity (GDD §18) | Evaluate partner studios on Unity capability during pre-production |
| Unity licensing/runtime fee changes | Track Unity's terms at each milestone; Personal/Pro tier as revenue requires |

## Not decided here

- Final art pipeline format (FBX native vs GLB through glTFast) — see the tech plan.
- Console port (GDD phase 3) — Unity supports it; scope later.

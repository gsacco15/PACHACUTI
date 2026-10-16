# From grey-box to a real game: the work list

The slice plays end to end as a grey-box (week 1 of the 10-week plan, §14). This is everything
between that and something that looks and feels like a finished game, in the order to do it.

Tags: **[Claude]** code, shaders, scene work Claude Code can do alone. **[Claude + free assets]**
Claude can do it with CC0 downloads (Poly Haven has an API; Freesound needs a login).
**[You]** needs the owner (a login, a purchase, a decision). **[People]** needs a specialist;
Claude can make a stand-in first.

Rules that still apply (`CLAUDE.md`): rules and tuning in `Pachacuti.Core`, numbers in `Tuning.cs`,
grey-box beats keep working at every step, screenshots before and after each visual change,
the browser build stays under 60 MB and 60 fps at 1080p on an integrated GPU (§13).

---

## Phase 0 — foundations (do first; everything else depends on it)

1. **[Claude] Turn the code-built district into real scenes and prefabs.** Today `DistrictBuilder`
   and `QenqoBuilder` build the world at runtime, so nothing can be lightmapped, hand-placed or
   art-swapped. Add an editor command that runs the builders once and saves the result as
   `Scenes/District.unity` and `Scenes/Qenqo.unity` (prefabs per building), loaded additively by
   `Boot`. Keep `Layout.cs` as the source of positions for beats.
2. **[Claude] Git LFS** for `*.png *.jpg *.exr *.tga *.psd *.fbx *.glb *.wav *.ogg *.mp3` before the
   first art commit (`git lfs install`, `.gitattributes`). Watch the GitHub LFS quota.
3. **[Claude] Build and deploy on every push** (the WebGL workflow is manual today), so the Vercel
   link always shows the latest version. Keep the Library cache.
4. **[Claude] Performance harness:** an in-game FPS/frame-time readout on F1, and a short script
   that flies a camera through each beat and logs frame times, run on the Surface Laptop 4 in Edge.
5. **[Claude] Texture and mesh import rules** for the web: max 2K textures (1K for props),
   compressed formats, mesh compression, LOD groups. Report build size after every build.
6. **[You] Remove the Unity AI packages** and do not use the ✦ AI button in this project (they
   broke compilation with a duplicate assembly).

## Phase 1 — light and atmosphere (biggest visual gain per hour)

7. **[Claude] Moon shadows back on** (they are off because the web build lacked the shadowed Lit
   variant): find the stripped variant, include it, verify in a browser build.
8. **[Claude] Baked lighting** for the static district (after step 1): lightmaps for ambient
   occlusion and bounce light, light probes for people, a few reflection probes for wet stone.
9. **[Claude] Real torches:** flame particles (flipbook), embers, smoke, flickering light with a
   soft cookie, soot decals on walls above torch brackets.
10. **[Claude] Better fog:** height fog that pools in streets, slow drifting noise, fog cards in
    alleys, light shafts through fog from the moon and torches (cheap screen-space version).
11. **[Claude] Night sky polish:** a visible moon disc with halo at its real 1532 position,
    thin moving clouds, gentle star twinkle, a painted Milky Way texture in place of the
    procedural band.
12. **[Claude] Grading and post:** a night colour-grading LUT, bloom on fire only, subtle grain,
    SSAO if the Surface can afford it in the browser (measure), no heavy blur.
13. **[Claude] Cold breath vapour:** Cusco at night is cold; Manco's breath shows as vapour,
    stronger when winded. Ties the look to the breath meter.

## Phase 2 — the world (environment art)

14. **[Claude + free assets] Inca masonry.** Generate fitted polygonal walls block by block
    (bevelled edges, tight joints, slight bulge per stone) for Hatunrumiyoc and the palace;
    coursed ashlar for the Coricancha. Scanned rock textures from Poly Haven (CC0) with normal,
    roughness and AO maps.
15. **[Claude] Trapezoidal doorways, niches and windows** everywhere: the most recognisable Inca
    shape. Today's doors are rectangles.
16. **[Claude + free assets] Adobe and plaster** upper walls on stone footings, with cracks and
    weathering decals; **thatch** (ichu grass) with real thickness, overhangs and loose straw.
17. **[You] Decide the roofs.** Cusco's houses had steep gabled thatch roofs; the slice's rooftop
    run assumes walkable roofs. Either keep walkable flat roofs and terraces (stylised), or make
    the run along ridges, wall tops and terraces (closer to history).
18. **[Claude] The street:** fitted paving, a stone water channel, mud and puddles that reflect
    torchlight, doorway curtains, a soldier's checkpoint with weapons and loads.
19. **[Claude + free assets] Props:** aríbalo jars, baskets, maize sacks, textiles, ladders, fire
    pits, quipu racks, tools, ropes, a llama or two in the compound.
20. **[Claude + free assets] Plants and ground:** ichu grass tufts, moss in joints, molle trees,
    cactus at Qenqo, scattered stones.
21. **[Claude] Backdrop:** Sacsayhuamán's zig-zag walls on the hill, mountains, city rooftops
    fading into fog, distant fires and smoke columns (a city under occupation).
22. **[Claude + free assets] Qenqo:** the carved limestone outcrop with the zigzag channel, the
    cave altar and the standing stone, from reference photos; rock scans for the surface.
23. **[Claude] The Coricancha:** the curved wall, fine ashlar, the gold band at the roofline.
24. **[Claude] Level of detail and culling:** LODs on walls and props, occlusion culling, GPU
    instancing for repeated stones and plants.

## Phase 3 — people

25. **[People, Claude stand-in first] Manco:** a rigged young man as a porter: unku tunic, cloak,
    headband, sandals. Stand-in: a free rigged humanoid (Mixamo, needs **[You]** to download) with
    period-coloured clothing materials. Final: a character artist.
26. **[Claude + Mixamo] Animations:** idle, walk, run, crouch-walk, carry-walk, jump, drop, land,
    hang, shimmy, climb up, mantle, winded/gasping, strike, hide, pick up and drop the load.
    An Animator driven by the existing `Manco` states; foot IK on uneven ground, hand IK on ledges.
27. **[Claude] Climbing that matches the art:** handholds on real masonry, climb animations that
    land on them, a short reach-and-pull when mantling.
28. **[People, Claude stand-in] Soldiers** (Quizquiz's army): distinct dress, star-headed maces,
    spears, slings, shields; torch carriers. Animations for glance, stare, approach, alert, chase.
29. **[People, Claude stand-in] The cast:** the woman, Tupa, Ninan (an old quipu keeper),
    porters, townspeople. Keep faces in shadow or at distance until real heads exist.
30. **[Claude] Crowds** for Cajamarca and the porter line: instanced animated figures.
31. **[Claude] Soldier navigation** with the AI Navigation package (navmesh) instead of straight
    lines; search patterns when suspicious.

## Phase 4 — camera and feel ("it feels like a real game")

32. **[Claude] Camera:** Cinemachine follow camera with collision (never inside walls), smoothing,
    slight look-ahead, FOV lift when running, small shake on hard landings.
33. **[Claude] Movement feel:** acceleration curves, coyote time and input buffering on jumps,
    ledge magnetism, footsteps synced to animation.
34. **[Claude] Breath feedback:** subtle vignette and desaturation as breath drops, heartbeat,
    controller rumble, all quiet when breath is full.
35. **[Claude] Stealth readability:** torchlight pools that clearly read as "seen here", soldier
    head turns and barks that telegraph suspicion before it is too late.

## Phase 5 — sound and music (half of "feels real")

36. **[Claude + free assets] Real recordings in place of the synthesised placeholders:** footsteps
    per surface (stone, thatch, adobe, earth, water), cloth, torch crackle, wind, distant dogs and
    voices, water in the tunnel, a real pututu (conch) and quena.
37. **[Claude] Spatial audio:** 3D sources, reverb zones (street, compound, tunnel, Qenqo cave),
    an AudioMixer with ducking under dialogue and the vision.
38. **[People, Claude stand-in] Voices:** Quechua lines with subtitles; placeholder voices can be
    generated for timing, final lines need actors.
39. **[People] Music:** a score with Andean instruments; until then, licensed CC0/CC-BY stems.

## Phase 6 — story presentation

40. **[Claude] Cutscenes with Timeline:** cold open, the woman and the load, Tupa, Ninan's lesson,
    the Coricancha look-up, the vision; camera moves, animation, sound in sync.
41. **[Claude] The vision as a showpiece:** the liquid-light channel, golden ceque threads, the
    dark llama rising out of the Milky Way, the time-fold strobe (12 Hz cap), Cajamarca, the gold
    room. Shader and particle work, within the photosensitivity rules.

## Phase 7 — interface and the web page

42. **[Claude] A branded web loading page** (custom WebGL template): title art, a progress bar,
    a "click to begin" with headphones advice, the photosensitivity notice before the vision.
43. **[Claude] Menus outside play:** title, settings (quality, volume, mouse sensitivity,
    invert Y, subtitle size, reduced flash), pause, credits. In play, still only subtitles and
    the two prompts (§2, §10).
44. **[Claude] Controller prompts** with button icons for keyboard and gamepad.

## Phase 8 — performance, testing, pitch

45. **[Claude] Hit the budget** in the browser on the Surface: 60 fps at 1080p, under 60 MB;
    stream Qenqo separately (Addressables) if the download grows.
46. **[Claude] Bug and polish passes** per beat; PlayMode tests kept green.
47. **[You + 5 testers] Playtest** with the built-in playtest log (§15); fix what they trip on.
48. **[Claude] Capture:** Unity Recorder for a trailer, a photo mode for pitch screenshots.

## Decisions waiting on you

- The four design questions in `SLICE_IMPLEMENTATION_NOTES.md` (the hour of the night, quipu
  cord 2, the 6 m wall, Quispicanchi).
- Roofs (step 17).
- How realistic people should look: close-ups need a character artist; at night and at a
  distance, stand-ins can carry the pitch.
- Budget for people: character artist, animator or mocap, composer, Quechua voice actors,
  a historian's pass.

## Suggested order for the first two weeks

Steps 1–4, then 9–13 (light and atmosphere), then 14–16 on the rooftops only, then 25–26 with a
stand-in Manco, then 32–33. That gives one beat (the rooftops) that looks and feels like a real
game; repeat the same recipe beat by beat.

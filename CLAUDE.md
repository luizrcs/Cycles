# Cycles

A first-person horror/escape game in Portuguese (pt-BR), built in Unity. You wake up aboard a damaged early-1900s ocean liner called the **Adsum** (Titanic-style interiors — see `Assets/Models/Reference/titanic-corridor.jpg`). The ship's lifeboats are broken. You must explore a procedurally generated maze of ship corridors and cabins, collect the three items needed to repair a lifeboat — **Oars (Remos), Nails (Pregos), Compass (Bússola)** — and escape through the exit door before time runs out.

The twist that gives the game its name: the ship is stuck in a **time loop**. The thing hunting you — the "AntiPlayer" — is *you*, a past/future version of yourself replaying your own movements. Stay too long and a new "you" arrives, causing a paradox that erases you from existence.

## Core fantasy / feel (DO NOT CHANGE)

- Slow-burn nautical dread: creaking wood, ocean ambience, suspense drones, sparse warm lamps in dark corridors.
- The hunter is not a monster — it is literally your own ghost retracing your steps. The player should gradually realize that the footsteps behind them follow *their own* route.
- Menus are diegetic-feeling and minimal: camera pans across a 3D scene with a lantern, text fades, narrated storyboard intro. All transitions are slow fades to black.
- Language: all UI/narration is Portuguese.

## Scene flow

```
MainMenu ──"NOVO JOGO"──► (storyboard + narration, skippable via "PULAR") ──► PreGame ──► Game
   │                                                                                      │
   └──"Créditos"──► Credits ──"VOLTAR"──► MainMenu                          ┌─────────────┤
                                                                            ▼             ▼
                                                                         GameOver        Win
                                                                            └──"MENU PRINCIPAL"──► MainMenu
```

1. **MainMenu** — title "Cycles", "Luiz Rodrigo • 2021", buttons NOVO JOGO / Créditos. 3D backdrop with a point light + lantern. Clicking New Game: buttons fade out, camera animator plays `MoveCamera`, lantern plays `MoveLantern`, then `StoryboardCamera`/`StoryboardLantern`; six storyboard panels (`Storyboard_0..5` animators) play at 3/7/14/21/28/35 s while `NarrationController` plays six narration clips. A PULAR (skip) button fades in after 4 s. After ~40 s → fade → PreGame.
2. **PreGame** — mission briefing screen: "Os botes salva-vidas do Adsum estão danificados." / "Colete esses itens para consertar e escapar..." Item silhouettes are revealed inside rectangles (images fade), then auto-loads Game after ~10 s.
3. **Game** — the actual gameplay (see below). HUD: objectives panel with 3 checkmarks, messages "Há algo de errado neste navio..." and "Colete os itens restantes e escape!".
4. **GameOver** — "FIM" + reason text that slowly grows: caught (Reason 0) or time paradox (Reason 1). Back-to-menu button fades in after 4 s.
5. **Win** — "Você escapou!" + back to menu.
6. **Credits** — credits image, VOLTAR button.

Cross-scene state is passed via static classes: `GameOver.Reason`, `ExitDoorController.EndGame`.

## Gameplay loop (Game scene)

- **Deck generation** (`DeckGenerator` pure-C# + `DeckGeneration` MonoBehaviour): a grid maze of corridors (3×6 room slots, room distance 6 cells) on a Width×Height int matrix. Vertical/horizontal corridors with 10% random closure, dead-end pruning, then cabins (rooms) attached with door cells (matrix encodes door corner/rotation as values 4–11, room anchors as 12+). Regenerates until ≥3 rooms exist. `DeckGeneration` instantiates floor/ceiling/wall/door prefab variants by neighbor analysis, places the **exit door** on a border wall, registers the **entry door** (at hardcoded world pos 3.75, 71.25), and randomly assigns the 3 collectibles to 3 distinct rooms. Cell size is 7.5 world units; walkable Y is 4.75.
- **Intro**: player walks in automatically through the inner door ("AnyoneThere" voice line: player asks if anyone's there), then control unlocks.
- **Player**: CharacterController + mouse-look first person (`PlayerMovement`, `FirstPersonController`). Footstep sounds, visible body model (RenderPeople "Nathan").
- **PlayerPath**: records the player's position+rotation 100×/second, bit-packed into a `ulong` queue (time<<40 | x<<24 | z<<8 | yRot). This is the time-loop mechanic's backbone.
- **AntiPlayer** (`AntiPlayerFollow`): after 120 s, it spawns onto the player's recorded path and replays it exactly, 120 s delayed (State 1 = follow path). After the first encounter it respawns at the map corner farthest from the player and switches to State 2 = random maze roaming.
- **Detection** (`DetectPlayer`): AntiPlayer raycasts forward; if it sees the player ("AntiPlayerDetector" tag), control locks, it runs at the player, camera is forced to look at it (`GameLogic.PlayPreBattleEffects` — "Quem é você?" voice line). At <5 m the "battle" plays: fade to black + punch sounds.
  - **First encounter**: you survive — message text fades in/out, AntiPlayer respawns far away and roams, "minha cabeça" voice line, control returns.
  - **Second encounter**: game over (Reason 0).
- **Timer** (`PlayerTimer`): 240 s limit. On timeout: time-paradox audio, fade, game over (Reason 1).
- **Collectibles**: trigger-collected, checkmark fades in on HUD; collecting all 3 plays "GetOut" voice line and sets `ExitDoorController.EndGame = true` — the exit door starts glowing (`ExitDoorGlow` animator) and its trigger now opens it → Win.
- **Ambience**: ocean + suspense loops, random creaks every ~5 s, random color variation for tulips/beds in cabins, random paintings (`PaintingChooser`).

## Architecture notes

- Scripts live in `Assets/Scripts/{Character,Environment,GameLogic,UI}`. No assemblies, no namespaces, classic MonoBehaviour style (2021-era).
- "Container" components (`GameLogicContainer`, `CollectibleContainer`, `EnterDoorContainer`) sit on the Deck object so runtime-instantiated prefabs can find scene references via `transform.parent.parent.GetComponent<...>()` — a workaround for prefabs not being able to reference scene objects.
- All fades/animations of UI are hand-rolled coroutines with `WaitForSeconds(0.05f)` steps; scene-wide choreography is timed with hardcoded `WaitForSeconds` chains matched to audio clip lengths.
- `GameLogic` orchestrates battles via an int `battleState` machine polled in `Update`.
- Rendering: URP (was URP 12/2021-era assets), 3 quality tiers in `Assets/Settings`. Lights are baked into prefabs: `CeilingLight` (spot, intensity 60!, range 30), wall lamps (spot 5), rooms (point 20). `m_Lightmapping: 4` = realtime only.

## Known issues & plans (living section — updated as work progresses)

### Just happened
- Project was upgraded Unity 2021 → **Unity 6.4 (6000.4.10f1)** without ever opening/running it.

### Upgrade verification results (2026-06-11)
- Scripts compile fine (only 2 `rigidbody` name-hiding warnings).
- Game logic survives: maze generates (983 tiles), player walk-in works, movement unlocks, fades work.
- **Rendering is 100% broken: black screen.** Root cause: `Assets/Settings/ForwardRenderer.asset` is the deprecated 2021 `ForwardRendererData`; Unity 6's runtime auto-upgrade fails (console spams "Forward Renderer Data has been deprecated" every frame) and the camera ends up with **no renderer** → renders nothing. Fix: create native Unity 6 `UniversalRendererData` + new pipeline assets.
- NullReferenceException spam is from the **ProBuilder package editor overlay** (editor-only, harmless to the game; consider removing ProBuilder from manifest since no asset appears to use it).
- One "BoxCollider does not support negative scale or size" warning during deck instantiation — some tile prefab has negative scale; investigate.
- Editor-mode MainMenu screenshot is pure white; play-mode is pure black (renderer missing both ways).

### Diagnosed problems
- **"Half the lights are off"**: URP asset uses old Forward renderer with `m_AdditionalLightsPerObjectLimit: 4`. Corridor meshes near many lamps silently drop lights beyond 4. Intensities were cranked (spot 60) to compensate, which will blow out once the limit is fixed. **Plan: switch to Forward+** (no per-object limit in Unity 6), re-tune light intensities to sane physically-plausible values, enable additional-light shadows where cheap, add SSAO + volume post-processing (tonemapping, bloom for lamps/glow, vignette, film grain fits the horror mood).
- 2021-era URP assets predate Unity 6 features: no HDR output config, no Forward+, old ForwardRenderer.asset, `DefaultVolumeProfile.asset` sitting loose in Assets root.

### Modernization pass — done 2026-06-11
All scripts rewritten in place (class names and public field names kept so scene/prefab
references stay intact; all choreography timings preserved). New shared helper
`Assets/Scripts/UI/Fades.cs` (time-based `Fades.Graphic`/`Fades.Volume`) replaces every
hand-rolled `WaitForSeconds(0.05)` step loop, same durations.

Key fixes (each verified in play mode):
- **AntiPlayer ghost mode** (`AntiPlayerFollow.SetGhost`): renderers+colliders disabled until
  the 120 s path replay engages (`Engaged` property). Fixes the spawn-overlap shove AND makes the
  double "enter the ship 120 s after you", matching the fiction. The scene parks it at (-20,-10).
- **Encounter on proximity**: `FollowPath` stops ≥3 m from the player; `DetectPlayer` adds
  `TouchesPlayer()` (<3.5 m while following) alongside the forward raycast (now capped at 30 m),
  gated on `Engaged`. `MoveTowardsPlayer` never steps inside 1.5 m of the player (no physics shove).
- **PlayerTimer** counts scene-relative elapsed time (absolute `Time.time` made the paradox fire
  instantly on every second playthrough).
- **`ExitDoorController.EndGame` reset in `GameLogic.Start`** (stale static = exit open on replay).
- **AntiPlayer is tagged "Player"** so doors open for it — but that let it collect items and win
  the game; `Collectible`/`ExitDoorTrigger` now check `GetComponent<PlayerMovement>()` instead of tag.
- `Roam()` infinite `while(true)` → bounded 64-attempt loop (boxed-in roamer froze the game).
- `GameLogic` no longer polls `battleState` in `Update`; coroutines call the next phase directly.
- `DoorTrigger` open-state now reads the signed Y angle (`Mathf.DeltaAngle`) instead of comparing
  a raw quaternion component.
- `MakeCollectiblesGlow` no longer grows its list every frame; tracks current target, max distance.
- `FirstPersonController`: mouse look no longer multiplied by deltaTime (frame-rate-independent;
  1/60 factor preserves the old feel at the same serialized sensitivity). Also `HideOwnHead()`
  shrinks the player's own head bone — the face used to clip into the camera during walk anims
  (the AntiPlayer instance keeps its head).
- `GameOverController` mojibake fixed (file was not UTF-8); grow animation time-based.
- Animator params via `Animator.StringToHash`; `UnityEngine.Random` instead of `System.Random`;
  `PunchSounds` indexes `% Sounds.Length`; `Collectible` guards double-trigger; unused usings gone.

### End-to-end verification — complete 2026-06-11 (all paths pass)
- Walk-in intro: player lands exactly at (3.75, 4.75, 71.25), control unlocks, no drift. ✔
- First encounter: engage at 120 s → chase → battle → message → respawn far corner → roam,
  player position untouched (no shove). ✔
- Second encounter (roaming double spots you again) → GameOver Reason 0 ("caught"). ✔
- Time-paradox game over at 240 s scene time; accented PT text renders correctly. ✔
- Collect ×3 → "GetOut" → `EndGame=true` → exit-door porthole glows (blue rim) → trigger → Win
  scene (hand-drawn Adsum + lifeboat sketch). ✔
- MainMenu → NOVO JOGO → camera/lantern pan → 6 storyboard panels + narration → PreGame briefing
  (item reveals) → Game. ✔  GameOver → MENU PRINCIPAL → MainMenu. ✔
- Lantern (MainMenu) tuned 0.25→0.9: 3.0 blew out the storyboard paper; 0.9 keeps desk shadows.
- NOTE for testing: `Time.timeScale` set via editor code persists across play sessions — reset to 1.

### Housekeeping
- **ProBuilder removed from manifest** (2026-06-11): unused by any asset; its editor overlay
  NRE-spammed the console in Unity 6.4.
- The "BoxCollider does not support negative scale" warning comes from `Room_B(Clone)/Oars`
  (Room_B is a mirrored room variant; `Wall_Door_B` is mirrored x:-1 too). Harmless — Unity
  forces the box positive and the Oars trigger verifiably still collects.
- Shadow pressure: with soft shadows on every CeilingLight, 48+ shadow maps competed for the
  4096 atlas → pipeline assets use shadowDistance 30 and tier res 512/256 (fog hides the cutoff).
- `Assets/Screenshots/` holds debug captures from this work session — safe to delete anytime.
- Old pipeline assets (`ForwardRenderer.asset`, `UniversalRP-*Quality.asset`) are no longer
  referenced; delete once confident.

### Code bugs found during reading (original list, all addressed above)
- `DoorTrigger`: compares `localRotation.y` (quaternion component!) to 0 to decide open state — works only by accident; also OnTriggerExit always plays "InnerDoorClose" for inner doors even if never opened.
- `PlayerTimer` / `AntiPlayerFollow` use absolute `Time.time` (240 s / 120 s *since app start, not scene load*) — replaying from menu after a first run makes the timer/anti-player timing wrong or instantly expire. Same for `PlayerPath` encoding absolute time.
- `AntiPlayerFollow.Roam()` has a `while(true)` that can hard-freeze if the roamer ends up in a cell with no open neighbors; also `rigidbody.MovePosition` teleports (no interpolation) in State 1 — follow movement is 100 Hz teleport steps.
- `DetectPlayer` raycast has no max distance/layer mask — can "see" through anything that lacks colliders, and hits whatever is first regardless of walls.
- `GameLogic.Update()` polls `battleState == 2/4` and calls setup repeatedly within the same frame chain — works due to immediate state bump but fragile.
- `MakeCollectiblesGlow` adds animators to `glowing` list every frame while looking at a collectible (unbounded list growth).
- `ChangeColor`/`PaintingChooser`/`ChangeColor` instantiate material copies (`renderer.material`) — leaks; fine for scope but should use MaterialPropertyBlock or sharedMaterial copies.
- `NarrationController`/all UI: legacy `UnityEngine.UI.Text` mixed with TMP; Input Manager (old input); `FirstPersonController` mouse-look multiplies by `Time.deltaTime` (frame-rate-dependent feel).
- Encoding in `PlayerPath` assumes positive coords and wraps at 6553.5 units — fine for this map, but undocumented.
- GameOverController strings have mojibake (`Voc�`) in source — file encoding issue (must be saved as UTF-8).

### Rendering overhaul — done 2026-06-11 (tune further after code fixes)
- New `Assets/Settings/CyclesRenderer.asset` (UniversalRendererData, **Forward+**, SSAO feature intensity 1.5/radius 0.35).
- New pipeline assets `CyclesRP-High/Medium/Low.asset` (HDR on, soft shadows, additional-light shadows High/Medium, shadow distance 60, MSAA 4x on High, reflection probe blending+box projection). Wired to Graphics default + all 3 quality levels. Old `ForwardRenderer.asset`/`UniversalRP-*.asset` retired (still on disk, delete after a few sessions).
- `DefaultVolumeProfile.asset` moved to `Assets/Settings/`, filled with base grade. **The Game scene has its own "Post-process Volume" using `SampleSceneProfile.asset`** (author's original intent: ACES + bloom + vignette) — upgraded in place: ACES, bloom 0.7/threshold 1, vignette 0.33, ColorAdjustments (exposure −0.2, contrast +15, saturation −10), WhiteBalance +12 warm, FilmGrain Thin1 0.25.
- Light tuning (prefabs): CeilingLight point 60/30 → **4/range 11, warm, soft shadows**; Wall_A sconce 5/10 → **1.5/range 6, warm, no shadows** (they tile the maze, keep cheap); Room_A/B spots 20 → 3.5/8, FrameLight 5 → 3.5.
- Lampshade.mat: Unlit → Lit + warm emission ×2.2 (feeds bloom).
- Scene settings: Game = flat dark-warm ambient (0.07), dark exp² fog 0.018, "Directional Light Up ×4" reduced 0.2 → 0.08 warm; MainMenu = lantern point light 3.0 warm + soft shadows, ambient 0.04; every camera in all 6 scenes now has renderPostProcessing=true.
- YAML note: in Light serialization `m_Type` 0=Spot, 1=Directional, 2=Point (don't misread).
- The "black sphere" seen in early captures = the porthole window on doors. Not a bug.

### Newly discovered gameplay bug (critical)
- **AntiPlayer physics-shove**: in follow state it `MovePosition`s along the recorded path; when it reaches the (idle or slower) player it overlaps the CharacterController and physics shoves the player violently across the maze (observed: player pushed from spawn to x=63, later through a room, accidentally collecting an item). Fix: trigger the encounter on proximity, not only via the forward raycast; and stop path-replay movement when within encounter range.

### Modernization plan (style A)
- Keep public-field wiring (scene references) but mark with `[SerializeField]`, add namespaces? — *decision: keep no-namespace to avoid breaking scene/prefab script bindings; modernize internals only.*
- Replace absolute-time logic with scene-relative timing.
- Replace polling with events/callbacks where safe (no behavioral change).
- Keep all hardcoded choreography timings — they are tuned to the audio clips.

### Author's apparent future plans (inferred)
- `RandomSoundsController.WhatSound`/`GameLogic.WhatSound` clip exists but is never played — likely meant for a random scare moment ("que som foi esse?").
- `Storyboard_alt.ogg` — alternate narration take, unused.
- `End.png` storyboard sprite — possibly an outro storyboard never wired up.
- Only one deck/level ("Adsum"); structure (PreGame briefing per level, narration clips array) suggests more levels/ships were envisioned.

## Session 2 (2026-06-11, later) — post-FX really live + atmosphere & playability

### Root cause of "I see no visual changes"
The code-created `CyclesRenderer.asset` had **`postProcessData: {fileID: 0}`** — URP silently
skips the entire post-processing pass when that reference is null. Every grade (ACES, bloom,
vignette, grain) was configured but never executed. Fixed by assigning the package's
`PostProcessData.asset`. Lesson: **when creating URP renderer data from code, always assign
postProcessData; verify FX visually (vignette corners / grain), never trust settings alone.**

### "Renders at low resolution" — editor-side, two causes (both fixed programmatically)
1. Game view had **Low Resolution Aspect Ratios** enabled (renders at reduced DPI on scaled
   Windows displays).
2. Game view **zoom was 1.5×** (every pixel magnified 50%).
Pipeline renderScale was always 1.0; builds were never affected.

### Change log — user-directed
- Post-processing actually rendering (ACES/bloom/vignette/grain/SSAO verified in screenshots).
- AA: MSAA 4 all tiers + SMAA-high on every camera (round doorknobs/lamp domes).
- Two-profile grading: `SampleSceneProfile` = Game scene horror grade (exp −0.2, contrast +15,
  WB +12 warm, vignette .33, grain .25); `DefaultVolumeProfile` = neutral menu grade (exp 0,
  WB off, vignette .22, grain .18) → menus readable, PreGame no longer dark.
- MainMenu lantern 0.9 → **0.55** softer warm: storyboard paper readable, desk shadows kept.
- PBR material pass (no model/texture changes): Wall smooth .38 (semi-gloss paint), floors .45
  (varnish), Wall_Bar de-metalled (was metal .8!) → varnished hardwood, Golden_Bit → true brass
  (metal 1, smooth .78), **Ceiling_Light dome + base were Unlit flat color → Lit + warm emission /
  bronze** (big part of the old "PS3" look), Frame .45, Painting canvas .12.
- Intentional light failures (`DeckGeneration.ApplyLightVariation`): per corridor lamp 15% dead
  (light off + emission killed), 12% flickering (`FlickeringLight`: perlin waver + random
  near-blackouts, drives light AND glass emission). Rooms exempt so items stay findable.
  Verified: 33 dead / 9 flickering / 157 lit on one generation.
- Ship sway (`CameraSway` on PlayerCamera): roll ±1.4° @9 s around the ship's length axis
  (world Z — full roll looking down lengthwise corridors, becomes pitch looking across),
  heave ±4.5 cm @6.5 s, walk bob 3.5 cm that fades in/out with real velocity. Implemented on a
  runtime "CameraSwayRig" parent so it can't fight mouse-look or drift.
- Night exterior: new `Assets/Materials/NightSky.mat` (procedural, near-black ground, dark blue
  sky) as Game scene skybox + fog 0.02 — portholes show night, not void.
- Door jitter fix: new `DoorState` (runtime-added to each door, shared by inner/outer triggers):
  occupancy counting + 0.6 s rate limit; closes only when everyone left the trigger.
- Smooth AntiPlayer: replay now drains all due path points per frame (old code consumed 1/frame
  vs 100/s recording — the delay silently grew beyond 120 s) and **glides** to the target at
  8 m/s with slerped yaw + rigidbody interpolation, instead of 100 Hz teleports.
- Testing note: `Time.timeScale` persists across editor play sessions — it was still 4× from an
  earlier test and made scene chains race; always reset.

### Change log — Claude-suggested (acknowledged by user as my mandate to propose)
- Two-profile grading split (menus neutral vs game horror) rather than one global grade.
- Ghost mode + proximity encounter for the AntiPlayer (session 1) — fiction-preserving fix.
- Rooms exempt from light failures (gameplay readability).
- Walk bob tied to CharacterController velocity rather than a constant.
- Emission sync on flickering lamp glass (light + material dim together).
- Removing ProBuilder; retiring 2021 pipeline assets.

### Horror ideas backlog (proposed, NOT implemented — discuss before building)
Victorian/ship-horror references to draw from: *Ghost Ship*, *The Shining* corridors,
*Amnesia*'s sanity system, *Layers of Fear* (ship DLC), 1912 Titanic interiors.
- **Sanity/nausea system**: staying in darkness or seeing the double raises "dread" — drive
  vignette/chromatic aberration/breathing SFX/heartbeat from it (Volume weight blending).
- **Seasickness shader moments**: brief lens distortion + horizon roll amplification when the
  ship lurches; tie to a rare "big wave" event with audio (creak swell + distant impact).
- **Room events** (random per room entry): cold-breath fog, lamp dies as you enter, painting
  changed when you look twice, door slams behind you, muffled footsteps overhead.
- **The double leaves traces**: wet footprints appearing along its replay path; humming the
  player's own walking rhythm from around corners.
- **Radio/gramophone**: a cabin gramophone playing period music that distorts as the double
  approaches (proximity-driven audio filter).
- **Porthole scares**: occasionally a silhouette/wave crash visible through an outer porthole.
- **Breathing/condensation**: cold rooms show faint breath puffs (particles) — sells the cold
  North Atlantic.
- **Dynamic lighting failure cascade**: as the 240 s timer nears its end, corridor lights fail
  progressively — darkness closes in with the paradox.
- **Footstep materiality**: distinct carpet vs bare wood footstep sets (clips already vary).

## Session 3 (2026-06-11, evening) — "make the horror REAL" pass

User verdict on session 2: changes too timid, atmosphere barely moved. Root lesson recorded:
**the brief is a creative mandate, not a bug list — when the user describes an atmosphere,
build the atmosphere, not the minimum diff.**

### What was done (user-directed)
- **Light failures made obvious and physical** (`FlickeringLight` rewritten): 20% dead + 20%
  defective. Two modes modeled on real failing bulbs — *Dimmer* (deep brownouts to 10–30%
  crawling over seconds, occasional full dropouts) and *Flasher* (steady, then violent 8–25 Hz
  sputter bursts, a third ending in a dead second). Healthy bulbs vary ±15% intensity / warmth.
  Lamp glass emission follows its light.
- **Air** (`DustAndMist`, all runtime-procedural): deck-wide drifting mist quads + camera-local
  dust motes; fog recolored from void-black to dark mist (0.045–0.055) density 0.022 so corridor
  ends read as haze, not Minecraft void.
- **Surface imperfection without new art** (`Assets/Textures/Generated/`): code-generated
  tileable detail normal maps — plaster stipple+pores (walls/frames), wood grain+scratches
  (floors, rails, cabinet, desk), fabric weave (carpet) — wired via URP Lit *detail maps*
  (independent tiling; albedo untouched). This is THE technique for faking material detail on
  off-the-shelf models with no authored maps.
- **Period-camera grade**: Game = grain .55 Medium1, vignette .45, chromatic aberration .25,
  exposure −0.35, saturation −15, bloom .85. Menus = grain .4, vignette .32, CA .15. (Old-TV
  direction confirmed by user.)
- **Paper is matte now**: storyboard pages + painting have specular highlights and environment
  reflections OFF — that killed the storyboard glare (it was specular, not light intensity).
- **Porthole night sky fixed**: the author's Glass shadergraph distorts scene-color, so from
  inside it smeared dark wall pixels — exit-door portholes swapped to new clear `PortholeGlass`
  (Lit transparent). New `NightSky.mat` = 6-sided skybox from a code-generated starfield
  (900 stars + faint cloud bands). Verified visible from outside the hull.
- **Fonts fixed**: both TMP assets (Special Elite = the typewriter font, Merriweather) had
  static atlases without accented glyphs → relinked source TTFs and switched to **dynamic atlas
  population** — "Bússola"/"estão" now render in the right font. Special Elite material got a
  soft press-shadow underlay + slight ink erosion (_FaceDilate −0.06).
- **Old-radio narration** (`RadioVoice` on the MainMenu narrator): 400 Hz–3.4 kHz band-pass +
  light distortion.
- **PreGame brightness**: key light 1.0→2.0 angled 25°/−15° for shape, flat warm ambient 0.3
  (items were 3D models lit flat head-on; the dark "Item" Images are covers that fade to reveal).
- **Editor capture traps documented**: PreGame auto-advances in ~12.5 s (slow `timeScale` to
  catch it); `Time.timeScale` persists across play sessions; Game-view zoom/low-res made the
  user think the game rendered at low resolution (fixed: zoom 1×, low-res aspect off).

### Verification status
- Storyboard: matte parchment, readable, desk shows wood grain+scratches. ✔ (screenshot)
- PreGame: briefing with accents correct, oars/compass read well; nails dark→fill raised, not
  yet re-screenshotted. (~✔)
- Game corridor: dead-lamp pockets, vignette/grain/CA, surface detail visible. ✔ (screenshot)
- Mist/dust: systems spawn; velocity-mode console error fixed after the beauty shot — re-verify
  drift visually next session.
- Flicker behaviors: coded to be obvious; **not yet observed over time in play — user should
  judge rates/depths** (all constants at top of `FlickeringLight.cs`).

### Tuning knobs (single values, safe to tweak by hand)
- Dead/flicker rates: `DeckGeneration.ApplyLightVariation` (0.20 / 0.40 rolls).
- Flicker character: constants in `FlickeringLight` (brownout depth/speed, burst rate/length).
- Mist/dust density: `DustAndMist` (MistAlpha .05, DustAlpha .45, emission rates).
- Grade strength: `SampleSceneProfile` (game) / `DefaultVolumeProfile` (menus).
- Detail-map strength: each material's `_DetailNormalMapScale` (walls .55, floors .5, carpet .8).
- Sway: `CameraSway` amplitudes/periods. Ship roll axis assumes length = world Z.

### Next-step roadmap (detailed, in priority order)
1. **Verify in real play** (user): flicker visibility, mist drift, door feel, sway strength,
   radio voice, second-encounter pacing. Adjust knobs above.
2. **Footstep materiality**: carpet vs wood step sounds (clips exist; pick by raycast surface).
3. **Light-failure cascade ending**: as the 240 s timer approaches, corridor lights die in
   waves radiating from the exit — darkness chases the player out (PlayerTimer → event →
   DeckGeneration kills lamps progressively).
4. **The double leaves traces**: faint wet footprint decals spawned along the replayed path
   (a quad + generated footprint texture every ~2 m, fading over 30 s).
5. **Gramophone room**: one random cabin gets a looping period song; low-pass + volume rise as
   the AntiPlayer nears it (it hums along — reuse a narration clip pitched down?).
6. **Sanity/nausea system** (user's idea, design needed): dread meter raised by darkness
   proximity/double sightings → drives a Volume (CA, lens distortion, vignette pulse) +
   heartbeat/breathing loops. Cap so it never blocks navigation.
7. **Porthole scares**: rare silhouette pass / wave splash on outer portholes (sprite + audio).
8. **Room events** on entry (roll per room): lamp dies as you enter, door slams behind,
   cold-breath particles, painting swap when revisited.
9. **Exterior believability**: faint moonlight shaft through portholes (thin spot light per
   exit door), distant ocean-surface plane with scrolling normal map visible only through glass.
10. **Audio pass**: ship groans positionally (creak sources at hull walls instead of 2D),
    distant metal knocks from the double's direction while it roams.

## Session 4 (2026-06-11, night) — "go hard": decay, air, water, type

### USER IDEAS LEDGER (every one of these ships — 100% mandate)
Implemented this session:
1. ✅ Light distribution: ceiling 20% dead / 20% flasher / 20% dimmer / 40% "healthy";
   sconces 50% dead then same 20/20; healthy lamps run a 0–100% luminosity lottery eased
   bright (`pow(rand, 0.35)`). Rooms: bright again, defects allowed, never dead.
2. ✅ Realistic failure behavior (was "always on"): dimmers crawl into deep 10–30% brownouts
   over seconds and sometimes die; flashers sputter hard at 8–25 Hz in bursts.
3. ✅ Light pools stay near their grid cell (ceiling range 7.5 = one cell; sconce 4.5) —
   walking into a dead stretch is now actually dark.
4. ✅ Rooms re-lit (the "dim = bad PS3 rendering" note was right — dim ≠ atmospheric there).
5. ✅ Mist that you can SEE down a corridor + drift synced to the ship's roll period + fog
   pockets with different density per area, some low/some head-height + floor haze.
   (Root-caused why it was invisible before: DustAndMist raced DeckGeneration and spawned
   everything at float.MinValue — now waits for valid deck bounds.)
6. ✅ Ocean below the night sky (was "floating in space"): 400 m water plane, scrolling wave
   normals, heave synced to roll; skybox sides got a real horizon fade, down face is sea haze.
7. ✅ Surface age: everything was "clean perfect modern" — grime/stains/streaks baked into
   wall paint (with blotchy sheen via albedo-alpha smoothness) and into aged COPIES of all
   floor + carpet albedos (originals untouched).
8. ✅ Texture stretching on bad-topology meshes (curved porthole walls): new
   `Cycles/AgedWall` triplanar shader samples grime + detail normals in WORLD space —
   flat, even coverage, UVs ignored. (Compiled first try; SSAO/shadow/depth passes included.)
9. ✅ Font still too perfect: ink-grunge face texture on both TMP fonts (uneven coverage,
   pinholes) + deeper erosion. Verified: "NOVO JOGO" reads hand-stamped.
10. ✅ CA dialed down in menus (0.06) for readability, full character in game (0.22);
    vignette eased both (game .36 / menu .28).

Standing user principles (apply to all future work):
- Go HEAVY on imperfection/atmosphere; stack constant + random + location-specific effects.
- Reference: Alien Isolation tier, not Silent Hill/PS3 tier. "Latest and greatest."
- The environment should "eat from you" — engulfing, unnerving, heavy.
- Research real-world behavior (how lamps fail, how paper reflects, how old ships sound)
  and real techniques from other games/forums before improvising.
- All user ideas ship; Claude adds its own on top; both attributed in this file.

### CLAUDE ADDITIONS (this session)
- Albedo-alpha smoothness trick: dirt kills sheen per-pixel without extra textures.
- Sconce vs ceiling failure rates differ → reads as a circuit dying, not random decay.
- Fog pockets seeded deterministically from deck instance so layouts vary per run.
- Sea mist band outside the hull; star-free haze band at the skybox horizon.
- Cribbed the exact Unity 6 URP keyword set (incl. `_CLUSTER_LIGHT_LOOP` for Forward+)
  from the package's Lit.shader before writing the custom shader — do this for any future
  hand-written URP shader.

### Known/accepted
- Ocean is near-black (moonless North Atlantic) — moon glint pass listed below.
- Fog pockets can be VERY thick when you stand inside one (user asked heavy; knob:
  `DustAndMist` pocket alpha ×2.2).
- "BoxCollider negative scale" warning remains (mirrored Room_B oars — harmless).

### IN-DEPTH TODO (priority order, with implementation notes)
1. **User playtest of session 4** — judge: flicker visibility over a full run, mist density
   in normal corridors vs pockets, sconce mortality (50%) navigability, room brightness,
   menu type legibility at distance, ocean through porthole at gameplay angle.
2. **Moonlight**: one cold dim directional (intensity ~0.15, blue-gray) angled through the
   exit-door portholes + a faint specular band on the ocean. Sells the water at night and
   gives outer corridors a second light color. (Claude)
3. **Light-failure cascade ending**: at T-60 s the paradox approaches — lamps die in waves
   radiating outward from the exit door; by T-10 only sconce embers. Implement: PlayerTimer
   broadcasts remaining time; DeckGeneration sorts lamps by distance-to-exit and schedules
   `KillLamp` with FlickeringLight death-sputters. (Claude — user approved "darkness closes in" direction)
4. **Footstep materiality**: raycast down, carpet vs wood step sets (clips exist in
   Sounds/Character; split by feel). Also the DOUBLE's steps should sound subtly wrong —
   slight pitch-down + reverb. (User: movement feel; Claude: wrong-steps detail)
5. **Wet footprints along the double's replay path**: spawn fading decal quads (generated
   footprint texture) every ~2 m on the path it has already walked. The player can TRACK
   their pursuer — or realize it walked where they're standing. (Claude)
6. **Positional ship audio**: move creaks from 2D random to 3D sources at random hull
   positions; add distant metal knocks from the double's actual direction while roaming;
   low groan swell synced to the big roll. (User: atmosphere; Claude: direction-coded knocks)
7. **Sanity/nausea system** (USER — design before building): dread accumulates in darkness
   and on double sightings; drives heartbeat/breath loops + a Volume with lens distortion /
   CA pulse / desaturation; decays under working lamps. Hard cap so navigation stays possible.
8. **Seasickness moments**: rare "big wave": roll amplitude ×3 for one period, props audio,
   loose items rattle, lamps swing (animate light transforms ±5°). (Claude, extends user's
   ship-roll idea)
9. **Room events** (roll on entry): lamp pops as you enter; door slams behind; cold-breath
   particles in one "cold room"; gramophone room (period song, distorts as the double
   nears); painting swapped when you look twice. (User concept; Claude specifics)
10. **Porthole scares**: outer portholes rarely show a passing silhouette/wave slap with a
    thud. (Claude)
11. **Hero-prop material pass**: beds, cabinets, globe, compass close-ups — same aged
    detail treatment as architecture (they have real albedos; add detail normals + grime
    bake). (User: "go through all assets")
12. **Exit-door moment**: when EndGame triggers, kill ALL corridor lights for 2 s, then
    only the exit's glow remains — a beacon. (Claude)
13. **Sound design of failing lamps**: buzzing loop on flashers (volume follows level),
    'tink' when a dimmer dies. Audio sells electricity better than visuals. (Claude)
14. **Performance audit** after effect stacking: profile worst corridor; budget mist
    overdraw (large soft quads are fill-rate hungry); consider halving pocket particle
    counts on Medium/Low quality tiers. (Claude)

## Session 5 (2026-06-12) — full audit + "the double is the horror now" pass

### Audit of sessions 1–4 claims (static analysis, all VERIFIED in code/assets)
Everything promised on visuals/atmosphere is genuinely present and wired — no gaps found:
- Pipeline: CyclesRenderer Forward+ (m_RenderingMode 2), postProcessData assigned, SSAO 1.5/0.35;
  CyclesRP-High/Medium/Low GUIDs confirmed in QualitySettings tiers + Graphics default.
- Materials: AgedWall shader → Wall.mat; Wall_Aged/Carpet_Aged/Floor_*_Aged albedos and
  Detail_{Plaster,Wood,Fabric}_N detail normals all referenced by the right .mats; InkGrunge in
  both TMP font assets; StarField/Horizon/SeaHaze → NightSky.mat; Waves_N → Ocean.mat.
- Scene wiring: CameraSway + DustAndMist on the player camera (dust correctly camera-local),
  OceanSurface in Game, RadioVoice in MainMenu; DoorState/FlickeringLight runtime-added (correct,
  no scene refs expected). FlickeringLight failure distribution matches the documented 20/20/20 +
  sconce 50% numbers (re-normalized survivor roll in ApplyLightVariation).
- The "real problems" were all on the AI/character side, as suspected.

### Bugs found & fixed (AI/movement)
- **Aggro through walls**: `DetectPlayer.TouchesPlayer` was distance-only. When the replayed path
  passed the player on the other side of a wall (player revisiting an area), the encounter fired
  and `MoveTowardsPlayer` beelined THROUGH the wall (the AntiPlayer's capsule is a trigger — it
  collides with nothing). Fix: every aggro path (sight, touch, stare) now requires `HasClearPath`,
  a 0.45 m SphereCast (triggers ignored) that must reach the player. Since the player is locked
  the moment the chase starts, the straight chase line is guaranteed wall-free.
- **Sight**: single forward ray → 35° vision cone (30 m) + the same clear-path check. The roaming
  double can now actually spot you off-axis, but never through geometry.
- **Replay could skip/cut corners**: old code drained all due points into ONE target and glided
  straight at it — after an encounter pause or any backlog, that line could cross walls. Now every
  due point goes into an ordered `route` queue and the replay walks through ALL of them (base
  5.5 m/s + up to +6 m/s catch-up scaling with backlog). It can lag the 120 s delay but can never
  desync from the player's actual path geometry.
- **Pop-in spawn** replaced by a real entrance (below).

### New: the entrance (fiction-preserving)
At first-point-due minus 2.4 s the double unghosts OUTSIDE the entry door (-2.4, 71.25), the door
plays "InnerDoorOpen" (collider off, same mechanism as the player's own intro), it walks in at
3.2 m/s to (1.4, 71.25), waits for the replay to come due, skips the recorded doorway points
behind it, then replays. Door closes behind it (watchdog in LateUpdate also closes it if an
encounter interrupts the entrance — collider must never stay disabled). It enters the ship the
way you did, 120 s after you.

### New: the double is WRONG (all runtime-added, player instance untouched)
- `Cycles/GlitchShell` shader + `AntiPlayerGlitch`: each SkinnedMeshRenderer gets a shell renderer
  on the SAME skeleton (fresh GO, bones/rootBone shared — follows animation for free). Shell =
  fresnel rim torn into world-Y slices, displaced sideways in time-snapped bursts, per-slice
  red/cyan chromatic ghosts, sparkle at high intensity. Everything quantizes on hashed ticks
  (13/s) — snaps, never eases (Digital Circus "Abstraction" reference, kept human). Plus whole-
  model displacement burst snaps in LateUpdate. Intensity: 0.12 floor when engaged, ~0.85 by
  proximity (<25 m), 1.0 in chase/battle, + StareBoost. Shader registered in
  AlwaysIncludedShaders (guid 3b1f8a2c9d4e4f06a7c2b5d8e1f4a627) so builds don't strip it.
- `AntiPlayerNoise`: synthesized 4 s "broken transmission" loop (ProceduralAudio: hum + crackle +
  hard chunk-repeat stutters + bit-crushed static breaths), positional on the double, linear
  rolloff 2–26 m, louder/angrier when close or chasing, erratic pitch wobble. Audible through
  walls by design — it's the keep-away cue.
- `DreadController` (on player camera, added by DetectPlayer): arrhythmic heartbeat (synthesized
  lub-dub; interval 1.15→0.42 s with intensity; 12% premature beats, 8% skipped), each beat kicks
  the FOV through an under-damped spring (~0.4 s cycle, overshoots below rest — swell/collapse,
  SOMA-style), plus a runtime global Volume (lens distortion −0.32 pulsing, CA 0.9, vignette 0.42)
  whose weight throbs on the beat. Idles at zero when not engaged.
- **Stare rule**: holding the double within 14° of screen center, <35 m, unobstructed (linecast,
  triggers ignored) accumulates ~3 s → `DetectPlayer.ProvokeFromStare()` — it notices and charges,
  even mid-roam, even if it never saw you. Glances decay at 2×. Staring also ramps StareBoost
  (shell glitch worsens as you keep looking — teaches "don't look at it").

### Verification (play mode via Unity MCP, 3 full runs at 3–8× timescale)
- Zero compile errors; zero runtime errors/NREs from any new system across all runs (console
  clean except the two known pre-existing warnings: Room_B negative-scale BoxCollider, shadow
  atlas pressure).
- Components verified spawning: glitch shell renderer cloned (1 per SMR), noise + dread + global
  DreadVolume all present at scene start; double ghost-parked at (-20, -10).
- Full chain verified by state inspection ×3: entrance (engaged, phase transitions) → touch
  encounter WITH clear LoS (also with the player teleported 30 m away — replay walked the
  corridor, no wall cuts) → battle → respawn far corner → roam → 240 s paradox GameOver
  (Reason 1). GameLogic flow unchanged.
- NOT yet eyeballed in person: the entrance walk-in on screen, shell shader look, heartbeat feel
  (MCP latency at high timescale ate the 2-second observation windows). **User playtest is the
  next step** — all tuning knobs below.

### Tuning knobs (session 5)
- Glitch look: `GlitchShell.shader` properties (_SliceScale 14, _TickRate 13, _Inflate) and
  `AntiPlayerGlitch.TargetIntensity` (0.12 floor / 0.85 proximity ceiling / burst rates in
  LateUpdate).
- Noise: `AntiPlayerNoise` (volumes 0.55 idle / 0.95 aggro, rolloff 2–26 m); loop content in
  `ProceduralAudio.MakeGlitchLoop`.
- Heartbeat/pulse: `DreadController` (interval lerp, arrhythmia rolls 12%/8%, spring 230/7, FOV
  kick 28–50, volume overrides).
- Stare: StareLimit 3 s, StareMaxAngle 14°, StareMaxDistance 35 m, decay 2×.
- Replay pace: AntiPlayerFollow BaseReplaySpeed 5.5, catch-up 0.01/point capped +6.
- Entrance: EntranceLead 2.4 s, EntranceSpeed 3.2.

### Session-5 testing traps (learned)
- Editor-code `Time.timeScale` persists across play sessions (re-confirmed; reset to 1 at end).
- MCP execute_code round-trips cost 1–2 s real each — at 6–8× timescale that's 10+ scene-seconds
  per call; pause-step or slow-mo (timeScale 0.3) around narrow event windows instead.
- Batch-mode compile checks are impossible while the editor has the project open (lockfile).

## Session 6 (2026-06-12, later) — playtest feedback round 1: "the loop notices"

User playtested session 5. Verdicts and the resulting work (all USER ideas unless marked):

### Fixed from playtest
- **Corner-peek blindness**: stare/sight used a single line (or thick sphere) to ONE body point —
  any partial occlusion killed detection entirely; only a fully exposed player was seen/stareable.
  Bodies are 3D. Now both directions test 5 body points (head/chest/legs/both shoulders, lateral
  offsets perpendicular to the sight line): ANY clear point = visible. Strafe-peeking around a
  corner with half your body out now triggers the stare buildup AND lets the double see you.
  Chase gained SphereCast slide-steering (projects movement along hit normals) so sliver-LoS
  encounters steer around corners instead of clipping them.
- **Battle mix**: heartbeat/pulse/volume/glitch-loop all kept blasting under the blackout and
  drowned the author's punch/stab sounds. Now DetectPlayer.State==2 ducks everything fast
  (DreadController target 0 + downSpeed 1.8, noise lerp 8×). Surviving sets afterShock=1
  (decays ~7 s — heart still hammering as you wake, which the user liked and asked to keep) plus
  a PERMANENT 0.12 heartbeat floor for the rest of the run (Claude idea, user-approved).
- Stare growth confirmed working as intended once visibility was fixed: outline → full
  abstraction over the 3 s, then ProvokeFromStare.

### New: GazeDiscipline — you must watch the corridor (USER design)
The stare/instant-sight economy only works if the player can't cheat by staring at the floor.
`GazeDiscipline` (runtime-added to the camera):
- WRONG gaze = camera pitch beyond ±38° (floor/ceiling) OR the LOOK direction (flattened, the
  direction you look — NOT the corridor cell you stand in, so peeking down a side corridor from
  a corner is explicitly valid) hits geometry within 3.2 m (kept under half corridor width so
  looking across a corridor from its center is always legal). Suspended inside rooms/doorways
  (deck matrix cell != 1) where staring at furniture is the point. Triggers never block the ray.
- Walking backwards accumulates at 0.45× (generous — watching the hallway behind you is honest).
- 1.0 s grace, 2.2 s ramp; decay at 0.5×/s — looking away costs more than it bought (user spec).
- Ramp drives: glitch slice flashes over the view (runtime overlay canvas, point-sampled
  red/cyan slice texture, time-snapped flicker like the shader) + heartbeat/pulse via
  `DreadController.ExternalDread`.
- At full penalty: `DetectPlayer.Ambush()` — the double is simply THERE, right behind you
  (clamped to the wall behind), encounter starts. First time = survivable, so the rule
  teaches itself. Inactive until the double is aboard (first 120 s = free practice) and during
  encounters.
- Design note (user): the double does NOT need scripted random-look-around behavior — gaze
  discipline forces the player camera (and therefore the future double) to face corridors
  naturally. The replay does the rest.
- Verified in play mode: corridor gaze = valid, wall gaze = wrong, floor gaze = wrong, pockets
  density field correct.

### New: MistNausea — the air fights you (USER design)
`DustAndMist.DensityAt(pos)` (0.15 ambient inside hull + quadratic falloff around the 9 fog
pockets) feeds `MistNausea` on the camera: exposure builds with density × time (≈13 s to full
inside a pocket), decays slower (≈22 s), drives slow layered-sine drunk tumble (roll ≤5.5°,
yaw ≤2.6°, pitch ≤1.3°) composed through `CameraSway.ExtraRotation` so nothing fights the rig.
Distinct from the heartbeat anxiety: that is the double, this is the air. Annoying enough to
push you out of pockets, never unplayable.

### New: the four approved immersion ideas (Claude ideas, user-mandated)
1. **Wrong footsteps**: the double's steps pitched 0.82 + Hallway reverb (your steps, but not
   quite). Plus phantom step bursts: every 16–38 s within 32 m, 3–5 footsteps at pitch ~0.7
   play from its position even while it stands still (separate child source, Cave reverb).
2. **ParadoxBleed**: last 60 s, the PLAYER's own body flickers with GlitchShell shells —
   brief at first, longer/more frequent toward the paradox (you are becoming the next double).
   PlayerTimer now exposes `Elapsed` + public `MaxTime` for this.
3. **Aftershock + permanent floor** (see battle mix above).
4. **The trail**: `FootprintTrail` — wet dark glossy footprint quads (code-generated sole
   texture, colliderless, smoothness .92) every 1.7 m wherever the double walks, alternating
   feet, fading over 30 s, capped at 44. Doors it passes linger open ~10 s (`DoorState` close
   delay; the player's own doors still shut promptly) — "I didn't leave that open."

### Teaching the player (current state + proposals)
Already self-teaching: every death-rule's first offense is the survivable first encounter; gaze
ramp gives 2+ s of escalating flashes/heartbeat before the ambush; stare ramps the double's
glitch visibly; nausea builds gradually. Proposals (NOT implemented — discuss):
- The intro voice line set already includes unused `WhatSound` — could gate a one-time PT
  whisper ("não olhe para ele...") on the first stare provocation, recorded later.
- First gaze offense could flash a 1-frame silhouette of the double in the flashes (the overlay
  already speaks the right visual language).
- PreGame briefing could add one line of period-styled text: "Não pare. Não encare. Não olhe
  para trás." — teaches all three rules diegetically before the first run.

### Session 6 tuning knobs
- GazeDiscipline: Grace 1.0 / Ramp 2.2 / DecayRate 0.5 / BackwardsRate 0.45 / PitchLimit 38° /
  OpenDistance 3.2.
- MistNausea: BuildSeconds 13 / DecaySeconds 22 / sine amplitudes in Update.
- DensityAt: ambient 0.15, pocket strength 0.85, falloff radius 7.
- Aftershock: decay 0.14/s, contribution 0.75; floor 0.12.
- Phantom steps: interval 16–38 s, range 32 m, pitch 0.62–0.74.
- Footprints: Stride 1.7 / Life 30 / MaxPrints 44 / BaseAlpha 0.75; door linger 10 s.
- ParadoxBleed: StartAt 180 s, flicker length/frequency scales in Update.

### Verification (session 6)
Zero compile errors; zero runtime errors in play mode; all new components verified attached
(GazeDiscipline/MistNausea/DreadController on camera, ParadoxBleed+1 shell on player,
FootprintTrail/PhantomSteps on the double, GlitchFlash overlay present, step pitch 0.82+reverb
confirmed, pocket density 1.00 center / 0.22 at 5 m / 0.15 ambient). Gaze classification
verified by direct invocation. NOT yet eyeballed: flashes on screen, footprint look, nausea
feel, phantom-step timing — **user playtest round 2**.

## Session 7 (2026-06-12, night) — playtest round 2: "I don't see the effects" → proof, fixes, film

### First: WHY the user saw nothing (important for every future session)
Live scripted verification (force-engaging the double via reflection) proved in the editor that
gaze→flashes→ambush→battle, stare-3s→provoke→GameOver(0), aftershock, the permanent 0.12 heart
floor, respawn/roam ALL fire. **If a playtest shows none of it, the session almost certainly ran
a stale standalone build — editor play needs no rebuild, but a built .exe must be re-built.**
Also remember most effects are gated: nothing at all happens before the double boards at 120 s.

### Real defects the playtest still caught (now fixed + re-verified live)
- **Footprints never spawned**: the floors have NO colliders (the player's Y is forced to 4.75,
  never grounded by physics) so the spawn raycast hit nothing — and the visible floor surface is
  at world y≈0 (model roots stand there; the 4.75 "walk Y" is just the controller pivot).
  Raycast now falls back to y=0. Verified: 31 prints at y=0.02 along a roam path.
- **Mist nausea imperceptible**: built too slowly (ambient mist diluted it) and its ±1° output
  hid under the ship's own ±1.4° sway. Now only density above the ambient floor (0.12) builds
  poison, full effect ≈9 s at a pocket center, decay ≈25 s, amplitudes roll ±7.5°/yaw ±3.2°/
  pitch ±1.8° + lateral head drift ±6 cm (new `CameraSway.ExtraOffset`). Density AND time now
  both visibly matter (USER spec).
- **Gaze rule armed instantly at 120 s**: a player idling at spawn (which faces a wall 2.4 m
  east!) was ambushed with zero input — discovered live when the forced engage triggered a
  legitimate ambush on its own. Now arms 6 s AFTER the double boards (the entry-door creak is
  the warning).
- **Glitch read as "GPU failure"** (USER: "TV going wrong, not a computer"): GlitchShell
  rewritten in analog language — pale desaturated phosphor static + per-pixel snow grain
  (re-rolled per tick), tears whose offset varies ALONG the slice (ripped, not slab), faint
  warm/cool fringe at 0.4 saturation instead of red/cyan, band flutter, rare vertical-hold
  slips (whole picture pops down a frame). GazeDiscipline's flash texture matched to the same
  palette.
- **Sound "everywhere and nowhere"** (USER): two fixes.
  (a) Occlusion: everything the double emits (noise loop, its footsteps, phantom steps) runs
  through AudioLowPassFilters driven by two linecasts ear→head/feet — walls drop cutoff to
  650 Hz and volume to 35%, smoothed at 3.5/s so an opening door audibly "lets the sound in".
  Verified live: cutoff 650 with the double across the deck. If you hear it bright and loud,
  the path to it is OPEN — sound direction is now trustworthy information.
  (b) Positional creaks: RandomSoundsController is 3D and teleports to a random hull point
  (7–22 m, varied height) before each creak. TimeParadox stays 2D (it's inside your head).

### New: FilmDamage — the whole game is a badly preserved reel (USER mandate)
Self-bootstrapping persistent object (`RuntimeInitializeOnLoadMethod`, DontDestroyOnLoad, no
scene edits) active in EVERY scene including menus:
- Luminance flutter (perlin ±4.5% black overlay) + occasional deeper dips.
- Vertical emulsion scratches WITH dark shaded edges and gaps (defects have shading, never
  clean outlines — USER), trembling sideways, born/dying randomly; dark hairs in the gate;
  dust specks flickering frame-to-frame; rare splice jumps (2 white frames then a dark beat,
  every 18–55 s).
- Faded-print grade volume (priority 90, weight .65): lifted blacks, teal-rotted shadows,
  warm highlights — on top of each scene's own grade.
- **Typewriter wear on ALL TMP text everywhere** via TMPro TEXT_CHANGED hook: every glyph
  deterministically offset (±3.5%/5% of font size), tilted ±1.3°, unevenly inked (alpha
  195–255) — the type itself sits wrong; not noise composited over perfect glyphs (USER).
  Covers HUD, menus, narration, GameOver. The overlay canvas sorts at 90, above the HUD, so
  the checklist inherits scratches/flutter too.
- Verified live: FilmDamage/FilmGrade/FilmOverlay spawn, checklist type visibly irregular in
  screenshot, zero console errors.

### USER IDEAS LEDGER — session 7 additions (all shipped unless noted)
1. ✅ Glitch language = dying analog TV, not GPU artifact (rewrite above).
2. ✅ Sound must be directional/propagation-honest; bounce/occlude so corridors shape it.
3. ✅ Old-film treatment everywhere, every scene: not "modern camera filming an old place"
   but "old, poorly preserved camera in an old place".
4. ✅ Fonts/UI must be physically defective (massive, cut-off, shaded defects) — not noise
   over perfect rendering; includes the items checklist (type wear + overlay damage; deeper
   per-item paper aging still possible, see backlog).
5. ✅ Mist nausea must scale with density AND time spent (strengthened, see fixes).
6. ✅ Compounding randomness everywhere — "the game should feel heavy ALL the time;
   psychological thriller; if we lose that we lose the game" (standing principle, now also
   served by film flutter/scratch/splice randomness + positional creaks + phantom steps).
7. Standing process rule: ALWAYS record every user idea in this file, compiled from
   spread-out remarks; user ideas all ship; Claude keeps suggesting its own on top.

### Backlog (new/remaining, in rough priority)
- Checklist paper itself: stained/torn paper texture + checkmarks as rough pencil scrawl.
- Gate weave (whole-frame jitter) needs a fullscreen blit feature — overlay can't shift the
  rendered image; consider URP FullScreenPassRendererFeature with a tiny UV-offset shader.
- Audio: true per-corridor early reflections are out of scope, but a second "echo" voice for
  the noise loop (delayed, quieter, from the nearest corridor opening) would sell bounce.
- Previous backlogs (moonlight, light-failure cascade ending, gramophone, porthole scares,
  room events, hero-prop pass, exit-door blackout moment, lamp buzz SFX, performance audit)
  still stand.

### Session 7 knobs
- FilmDamage: flutter 0.045, dip 0.10, splice every 18–55 s, scratch birth p=0.004/frame/slot,
  hair p=0.0012, dust p=0.06/speck, grade weight 0.65, wear offsets in ApplyWear.
- Occlusion: cutoff 650→22000, floor volume 35%, smooth 3.5/s.
- Creaks: 7–22 m radius, every 5 s.
- Nausea: AmbientFloor 0.12, Build 9 s, Decay 25 s, amplitudes in Update.
- GazeDiscipline ArmDelay 6 s.
- GlitchShell: _SliceScale 22, snow gate 0.92, fringe colors in frag.

## Working agreements

- Never change gameplay feel, menu layout, transitions, narration timing, storyline presentation, or model choices.
- Visual upgrades = pipeline/lighting/post-processing quality, not re-art.
- All user-facing text stays in Portuguese.
- Update this file whenever something new is learned, diagnosed, fixed, or planned.

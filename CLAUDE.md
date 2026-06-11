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

## Working agreements

- Never change gameplay feel, menu layout, transitions, narration timing, storyline presentation, or model choices.
- Visual upgrades = pipeline/lighting/post-processing quality, not re-art.
- All user-facing text stays in Portuguese.
- Update this file whenever something new is learned, diagnosed, fixed, or planned.

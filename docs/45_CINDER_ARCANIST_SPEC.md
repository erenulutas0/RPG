# 45. Cinder Arcanist (Ilya Kest) - the second character, and the character pipeline it sets

Agreed 2026-10-26 with the owner: after the Vanguard, the Cinder Arcanist. Built on the narrative bible (`44`) with the
four adjustments the owner accepted the same night:

1. **One-line premise.** "The sun is dying. The tower burns people's tomorrows to keep it lit. Every time you die it
   forges you again - slightly wrong."
2. **Misforgings.** After each death the hero returns with one visible cosmetic flaw (a crest on the wrong side, a
   gauntlet of another metal, an ember that will not go out in the chest). Collectible, cosmetic only, never balance.
   It makes the bible's pitch ("the tower remembers them incorrectly") something the player sees every run.
3. **Tone 50 / 30 / 20**: melancholy and threat / dark-cute and warm-weird / real humour. The Cinder Mites become the
   tower's sulking mascots.
4. **Five player-facing nouns** at first: the tower, the Long Dimming, tomorrows, the Unfinished, the Return Mark. The
   factions stay in the writers' room.

Also accepted: Ilya's palette moves to ash white (the bible's soot/orange/crimson vanishes on the Foundry's lava and
among the orange Cinder Mites), and the first release has one active skill per character.

## 1. Who Ilya is (player-facing, English)

- **Name on the select screen:** Cinder Arcanist, subtitle "Ilya Kest".
- **Story hook (bible 2.2):** the furnace mage who heard a child's song in the ashes of the first ignition and reported
  only the temperature. Climbs to find the ignition records and the voice. Ties straight into the Foundry, the only
  floor with art: the Ashbound Lullaby, the two little boots in Cinder Walk.
- **Lines (bible):** select "If it burns, it can tell us something." / death "Ah. That one was mine." / victory "Not
  every fire needs a victim."
- **Signature:** glass goggles with a living flame in one lens, one lens cracked. The flame has a face, and in play it
  reacts: pleased when Burn spreads, worried at low health, asleep when idle. Cheap (one small overlay, four faces) and
  the detail fan art will draw.

## 2. How Ilya plays (first numbers; the autopilot measures them)

| Part | V0.2 |
|---|---|
| Maximum health | 90 (Vanguard 100): the glass cannon |
| Starting weapon | Staff (exists) |
| Passive - **Ember Memory** | Ilya's hits apply one Burn stack. A burning enemy that dies passes one stack to up to 4 enemies within 1.6 units; a passed stack does not pass again (one generation, the bible's "recursive limit") |
| Active - **Crucible Vent** (14 s) | Every burning enemy within 3 units erupts: 8 damage per Burn stack it carries to itself and every enemy within 1.2 units of it, then its Burn is spent. Replaces Forge Burst on the one ability button |
| Later | Flame Sigil (25 s zone) once the two-button decision is made |
| Skill dependence | 3 (bible 4.2): auto-only farms the horde; a well-timed Vent should be worth 1.35-1.6x |

**Burn** is a new status: up to 5 stacks, each 3 damage a second, ticking every 0.5 s, every stack's 3 s refreshed by a
new one. It is the first damage-over-time in the game, so it gets its own pure rule (`BurnRule`) shared by the scene and
the simulation, the way `KillBurstRule` is, and reuses `INeighbourhood` for the spread.

Collision to resolve: the bible's achievement reward "Cinder Nova attack skin" uses the name of the Staff's evolution;
rename the skin.

## 3. What has to be built (engineering)

1. `BurnRule` + `BurnStatus` (enemy component), tests, telemetry (`burn_applied`, `burn_spread`, `burn_kill`).
2. Character data: `HeroDefinition` gains a passive id, its art set, its lines, its unlock; `Hero_CinderArcanist.asset`,
   `Ability_CrucibleVent.asset`.
3. Character select on the forge screen; the profile remembers the chosen hero (save version 3 with migration).
4. Unlock (bible section 6, adapted: lore objects do not exist yet and the pit kills nothing - 215 eruptions, 0 kills in
   the owner's run): **clear The Foundry once with the Staff**. Development builds can select Ilya from the start.
5. Presentation: Ilya's art set, the goggles flame, three ember flasks orbiting (procedural, like the Staff's orb),
   Burn on enemies (a small flicker and tint), the Vent's eruption.
6. Measurement: the autopilot plays Ilya on seeds 11-66 against the Vanguard.

## 4. The character pipeline - and why the Vanguard's walk looks wrong

The owner finds the Vanguard's walk and animations poor. The causes, from the code and the art:

1. **Attacks freeze the walk.** `VanguardAnimator.Attacked` shows the idle pose for every Staff or Daggers attack and
   holds it for up to 0.2 s, and the Sword's strike frames have both boots planted. With Tempo levels a Staff attacks
   every half second or faster, so a walking hero spends a large share of its time gliding in the idle pose.
2. **Four walk poses, each a separate AI edit.** Armour and cape details shift from pose to pose, so the figure boils,
   and nothing gives it weight: no bob, no lean, no breath.
3. **Two facings** (rear three-quarter and front three-quarter, mirrored), and attack poses only for the Sword.

Two fixes, to be done in this order:

- **A. Code, small (hours):** keep the walk cycle running while any weapon attacks (only the weapon and arm animate);
  add a vertical bob on the stride, a slight lean into the direction of travel, a squash on each contact and an idle
  breath. Presentation only; the existing animation tests stay as they are (they pin the standing hero).
- **B. Pipeline (the real fix, for every character):** the recommendation is a **cut-out puppet**: the character
  painted once as separate parts (head, torso, cape, upper arms, forearms, hands, thighs, shins, feet) on one sheet, then
  rigged and animated in code. One painting gives every motion - walk, cast, hit, death, idle - smoothly at any frame
  rate and with no boiling, and a new motion costs code, not art. The fallback, if the parts do not come out cleanly, is
  a frame sheet generated as **one image** (all walk poses side by side), which keeps details far more consistent than
  separate edits.

Ilya is the first character on the new pipeline; the Vanguard is redone on it once it is proven.

## 5. Prompt pack for ChatGPT (the owner generates, Claude integrates)

Rules for every prompt: transparent background, no floor, no shadow, no text, no effects unless asked; top-down
three-quarter camera like the existing Vanguard (looking down at about 45 degrees), light from the upper left; painted,
clean edges, readable at 80 pixels tall on a phone.

### P1 - Character design sheet (do this first; it decides the look)

```
Character design sheet for a 2D top-down action roguelite, painted style matching a dark fantasy foundry world.
Character: ILYA KEST, the Cinder Arcanist, a furnace mage and former scientist. Slim, slightly hunched, quick and
restless. Tall ASYMMETRIC hood that leans to one side. Long ASH-WHITE robe (#D8CFC2) with soot-black trims (#201B22)
and scorched hems; ember-orange (#E77232) only as small accents: glowing seams, a belt of three small ember flasks.
Brass-rimmed glass GOGGLES pushed up on the brow: one lens holds a tiny living flame with a simple cute face, the other
lens is cracked. Carries a long cracked wooden staff with a brass crucible head. Expression: small mischievous smile,
tired under-eyes. Show four views side by side on a plain light grey background, same scale, feet on one line: front
three-quarter (facing down-left), rear three-quarter (facing up-right), pure side view, and a close-up of the goggles
with four flame faces (happy, worried, sleepy, angry). Clean silhouette readable at small size. No text labels.
```

### P2 - Cut-out parts sheet (for the puppet; from the approved P1)

```
Using the approved Ilya Kest design, paint the character as a CUT-OUT ANIMATION PARTS SHEET for a 2D puppet rig, rear
three-quarter view facing up-right, top-down camera, light from the upper left. Separate, non-overlapping pieces on a
transparent background, each with generous extra overlap where it joins the next (round joints, no hard cut edges):
head with hood, goggles on the brow; torso with robe chest; robe skirt; cape/hood tail; left upper arm; left forearm;
left hand (open); right upper arm; right forearm; right hand (closed grip); left thigh; left shin with boot; right
thigh; right shin with boot. Same painted style, same scale for every piece as if the character were 1300 pixels tall.
Neutral flat lighting inside each piece, no cast shadows. No staff, no effects, no text.
```

Then the same for the front three-quarter view (facing down-left).

### P3 - Frame sheet (fallback if P2 does not separate cleanly)

```
Using the approved Ilya Kest design, rear three-quarter view facing up-right, top-down camera, light from the upper
left: ONE horizontal sprite sheet of 6 frames on a transparent background, each frame in an equal cell, the character
at the same size and the same floor line in every cell: idle, walk contact A, walk passing A, walk contact B, walk
passing B, casting (both hands forward, palm open). Identical costume details in every frame; only the limbs, robe hem
and hood tail move. Grounded, small steps. No staff in the hands (it is attached in the game), no effects, no shadow,
no text.
```

### P4 - Goggles flame faces (overlay)

```
Four tiny flame sprites, each a single living flame with a simple cute face, on a transparent background, side by
side, same size: happy, worried, sleepy, angry. Ember orange and warm yellow, a thin dark outline so it reads on any
background. Painted style. No text.
```

What the owner sends back: the chosen P1 image, then P2 (or P3) for both facings, and P4. Claude cuts, registers,
rigs and integrates, and shows matched before/after captures.

## 6. Built (2026-10-27, before the art)

- `BurnRule` + `BurnState` (Combat, pure, in the .NET checks) and `BurnStatus` (the scene component an enemy gains on
  its first stack): stacks, ticks, refresh, one-generation spread through the hero's `INeighbourhood`, `Consume` for the
  Vent. Ticks deal the hero's damage, so a burn kill drops and pays like any other.
- `AbilityKind.Vent` on `AbilityRuntime`/`AbilityDefinition`; `AbilityController` spends every burn in reach before any
  eruption lands, then deals `stacks x damage` to each burning enemy and its neighbours within `VentSplashRadius`;
  `Vented` event. `Ability_CrucibleVent.asset`: 8 a stack, 3 units, 14 s.
- `HeroDefinition`: subtitle, `HeroPassive` (Ember Memory), unlock (open from start, or clear The Foundry carrying a
  weapon), three lines. `Hero_CinderArcanist.asset`: 90 health, Staff, Vent, Ember Memory, unlocked by the Staff.
- `CombatSetup`: a hero roster (scene field `_heroes`, the Vanguard first), `Hero` chosen from `development/hero.txt`,
  then the profile; any hero plays on a development build (`DevelopmentStart.AllHeroesOpen`). Floor 1 cleared unlocks a
  hero whose weapon the run carried. Ember Memory hooks `AttackController.StruckWith`, so every weapon's hit burns.
- Profile save version 3: `unlockedHeroIds`, `selectedHeroId`; version 2 migrates to the default hero.
- Forge: `HeroChoiceView` shares the start button's row (left 40 %); a tap moves to the next playable hero.
- Presentation: a burning enemy's hit sparks in ember (#E77232); each Vent eruption draws an ember ring of its splash.
  The hero still wears the Vanguard's art until Ilya's arrives.
- Telemetry: `run_end` gains `burn_kills` and `burn_spreads` (last, only when something burned); `run_start.hero_id`.
- Autopilot: fires the ability when 3 enemies (burning ones for a Vent) stand in reach, or any beside the gate boss;
  the measurement takes `TestResults/horde-hero.txt`.

## 7. Today's art order (2026-10-27, the owner generates in ChatGPT)

Gameplay is shelved for now; the work is characters, their walks, weapons and skills. In order:

1. **Ilya - P1 design sheet** (section 5). Pick one; it fixes the look for everything after.
2. **Vanguard - P5 parts sheet**, rear and front, each from its attached master (`ArtDirection/2026-09-16/
   character-proof-01/vanguard-body-v2.png` rear, `ArtDirection/2026-09-17/vanguard-directions-01/front-idle-v1.png`
   front). The Vanguard keeps its blue steel - blue is what reads on the Foundry's orange - and gains the bible's two
   details: two scorched worker badges on the belt and a helmet visor cracked open on one side.
3. **Ilya - P2 parts sheet** (or P3 frame sheet) from the approved P1, rear then front.
4. **P6 Crucible Vent icon** for the ability button, beside the existing Forge Burst anvil.

### P5 - Vanguard cut-out parts sheet (attach the master)

```
Edit the attached Vanguard sprite into a CUT-OUT ANIMATION PARTS SHEET for a 2D puppet rig. Keep the exact painted
style, blue steel armour, brass trims, silver bevels, navy cape, the same camera (rear three-quarter, top-down, light
from the upper left) and the same scale. Add two small scorched worker badges on the belt and crack the helmet visor
open on one side. Separate the body into non-overlapping pieces on a transparent background, each with generous extra
overlap where it joins the next (round joints, no hard cut edges, the hidden part of each joint painted in): helmet;
torso with shoulder plates; cape (one piece); belt and skirt plates; left upper arm; left forearm with gauntlet; right
upper arm; right forearm with gauntlet (hand closed around nothing); left thigh; left shin with boot; right thigh; right
shin with boot. No sword, no shield, no effects, no shadow, no text.
```

Then the same with the attached front master (front three-quarter, facing down-left).

### P6 - Crucible Vent icon

```
A round ability icon for a dark fantasy mobile game, matching an existing icon of a glowing blue anvil with flames on
a dark navy disc: a cracked brass crucible erupting with ember-orange (#E77232) fire, three small sparks rising, on the
same dark navy disc with a thin brass ring. Bold readable silhouette at 96 pixels. Painted style, transparent
background, no text.
```

## 8. Art built in code (2026-10-27)

The owner was away for the day, so the art order above was built in code and Blender instead of ChatGPT; the prompts
stay for a later painted pass.

- **P6 Crucible Vent icon** - `Tools/ArtReview/build_ability_icons.py` draws a cracked crucible with three flame tongues
  on the Forge Burst icon's own navy field (its anvil painted out), on the same chunky cell grid, so the two buttons
  read as one family. `AbilityDefinition` carries its icon; the ability button shows the hero's own.
- **Painted skill effects** - `Tools/ArtReview/build_skill_vfx.py` (deterministic; a rerun is byte-identical) paints
  three sheets under `Assets/_Project/Art/Effects/Skills/` (Burn, Vent, Forge; 1.59 MiB as ASTC 4x4 together) and
  `SkillEffectArtSet.asset`; `CombatEffectsView._skillArt` uses them when valid and falls back to the pixel art.
  - Burn flames frame the burning enemy (feet, flanks, shoulders, a crown from 3 stacks) instead of sitting on its
    belly; 8-frame loops, all stack counts on one texture so a burning crowd batches; sized by the enemy's art top.
  - The Vent: an 8-frame eruption column (0.40 s), embers that rise beside it, and an ember cast ring at the hero in
    place of the steel seal pulse. Ember Memory's spread is a comet ember cooling to crimson.
  - The Forge Burst: a ring shockwave with heat ripples and a soot rim, steel sparks, and a 0.12 s flash at the hero.
  - Device checks still open: burn readability at 1-2 stacks on Mites and on lava, ASTC banding in the soft gradients,
    frame time with about 40 burning enemies, and occlusion behind Foundry structures.
- **The Vanguard's painted puppet** (section 4 B, behind `development/puppet.txt`) - `Tools/ArtReview/
  build_vanguard_puppet.py` cuts the two painted masters (rear and front three-quarter) into 14 parts each, along their
  painted outlines, under `Assets/_Project/Art/Characters/Vanguard/Puppet/` with `VanguardPuppetArtSet.asset` (122 KiB
  as ASTC 4x4 with the hit-flash copies). Joints that open when a limb bends show the master's own chainmail; the
  standing puppet matches the master to within 0.3 of 255 on average.
  - Motion (`PuppetPose`, `PuppetGait`, `PuppetMotion`, `PuppetRig`, `PuppetAnimator`): a jog that runs in and settles,
    planted boots that hold their floor line (hip drop from each leg's heel and toe), counter-turning shoulders and
    hips, breath and weight shift at rest, the cape and skirt swinging on after a stop; each weapon draws back while its
    cooldown runs out and then chops (sword), thrusts (staff) or jabs (daggers) over the striding legs; the Forge Burst
    crouches, raises and slams; a hit flinches; death staggers, buckles and falls.
  - `Tools/ArtReview/preview_vanguard_puppet.py` drives the real C# pose code outside Unity for review GIFs.
  - Known flaws: on a diagonal the planted boot slides up or down the screen; the far boot sits about 16 texels above
    the near one in the painted stance; changing facing is still an instant switch; the slam lands about 0.1 s after
    the button while the shockwave starts at the press.
- **Ilya rendered from one model** - `Tools/ArtReview/BuildBlenderArcanist.py` builds Ilya procedurally in Blender 4.5
  (toon shading, an inverted-hull outline, a rig with IK, every clip as an action; `ArtDirection/2026-10-27/
  arcanist-blender-01/` holds the .blend, frames.json and a README), `PackArcanistSprites.py` packs 210 frames into
  three atlases under `Assets/_Project/Art/Characters/CinderArcanist/` (2.64 MB as ASTC 4x4, 100 texels a unit) with
  `CinderArcanistArtSet.asset`. Five rendered facings (the west side mirrored): idle 8, walk 8 and walking strike 8
  (driven by distance, 1.2 floor units a stride), strike 5, Vent cast 5, hit 3, death 5. Because every frame comes from
  one model, nothing drifts between frames or facings.
  - `HeroDefinition._renderedArt` + `RenderedCharacterAnimator.TakeOver` (from `HeroLookView.Start`, after
    `CombatSetup` has chosen the hero) draw a hero with its own art; `RenderedCharacterPlayer` (pure) picks motion,
    facing and frame; `RenderedCharacterFlash.shader` gives the white hit flash.
  - Look: the robe ash-white with scorched orange hems, the cowl and mantle soot-dark (an ash cowl read as one pale
    column; `--hood light` rebuilds it), brass goggles with a living lens flame, the crucible staff lighting the cowl
    and shoulder with a warm halo that flares on the strike and the Vent.
  - The evolution flash (`RunFeedbackView`) reaches a hero whose animator hides the body: the rendered hero fades its
    own white silhouette, the puppet swaps in its white parts while the flash is strong.
  - `development/puppet.txt` is the Vanguard's: the puppet attaches in `HeroLookView.Start`, after the hero is known,
    and never takes a hero with its own rendered art.
  - Known gaps: the carried weapon is always the rendered staff; the mirrored west side flips the light.
- **Painted combat feedback** - `Tools/ArtReview/build_combat_feedback_vfx.py` (it reuses build_skill_vfx's painting
  without changing its output) paints one sheet, `Assets/_Project/Art/Effects/Feedback/VFX_Feedback.png` (212 KiB as
  ASTC 4x4), and `CombatFeedbackArtSet.asset`; `CombatEffectsView._feedbackArt` uses it when valid, the pixel art
  stays the fallback.
  - The Daggers' double slash (0.20 s, mirrored toward a target on the hero's left), the crit star (0.20 s), the
    death and arrival smoke (the pixel timings), a spinning coin over the same 0.5 s pop, and damage numerals drawn
    from the generator's own vector strokes (no font): white to pale warm, crits larger in gold, the pixel spacing
    kept (0.1875 units, 0.375 crit). One texture, so every digit on screen can batch.
  - Device checks: normal numbers over lava at a glance, and ASTC blocks on the thin numeral outlines (an uncompressed
    sheet would be about 0.85 MB). GroundDropView's gold-only fallback still uses the pixel coin.

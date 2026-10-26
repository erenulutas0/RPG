# 43. Backlog - gameplay animation, story and lore, character achievements (owner, 2026-10-26)

Three items the owner raised on 2026-10-26, recorded so they are not lost while V0.1 (docs/41, docs/42) is built.
None is scheduled yet; each waits for the owner's ChatGPT research round (lore, achievements) or a slot after the
weapons-and-tomes UI (animation).

## 1. Animation during play

The owner wants the play itself animated. What exists: the Vanguard's swing (VanguardAnimator on `Attacked`), strike
effects per weapon (CombatEffectsView: sword slash, staff ring, double slash, crit star), enemy looks (EnemyLookView),
foundry ambience (flames, banners, gate doors, trolley), the pit's band and grips, the Warden's mark. Gaps seen today:
- hero: idle/run/hit/death states, a reaction to a level-up and to a weapon picked up; three weapons firing need their
  own small motions (only slot 0 swings the body, docs/42 risks);
- enemies: walk cycles, hit flinch, death (bodies today simply stop and vanish after `HordeRule.RetireSeconds`), the
  horde's spawn-in at the edge of the view;
- boss: the Warden's windup before a slam (the mark exists, the body does not tell), its phase-two change, a death;
- feedback: level-up burst, rarity colour on taking a card, evolution moment, chest opening (partly done), XP vacuum;
- performance budget: 30-40 enemies on the S23 (decision 10) - animation must stay within the frame time the density
  probe measures; sprite-sheet animation over per-bone rigs.

## 2. Story and lore - a world players attach to

Goal (owner): a story and lore that bind players, invite fan art and grow a community. Constraints from the game as it
is: the run climbs upward floor by floor (the fiction flipped from "Descent" to an ascent, map-progression vision), floor 1
is the Foundry with the Forge Warden at its gate, later floors stack above it, the finale is a swarm you extract from;
existing names (The Foundry, Ember Hall, Cinder Walk, Slag Gate, The Forge, Captain's Post, Warden's Crucible, Quicksilver
Vaults, Vault Gate, Sentry Hall, Warden's Vault, The Final Swarm; Vanguard, Forge Warden, Grunt, Grunt Captain, Cinder
Mite, Runner, Tank; relics Second Wind, Counterweight; Forge Burst; tomes Force, Tempo, Reach, Precision, Vitality) and
the six planned characters of docs/40 section 4 (Vanguard, Cinder Arcanist, Riftblade, Bastion, Voltwright, Sanguine).
Delivery on a portrait phone has to be light: codex entries unlocked by play, item and card flavour text, short
boss lines, environmental storytelling in the maps. The owner runs a ChatGPT research round and brings the result back.

## 3. Character-based achievements

A list of achievements per character, tied to each one's mechanics (skill dependence 1-5, docs/40 section 4.2) and
doubling as the unlock and quest layer (docs/40 section 13: e.g. Cinder Arcanist unlocked by applying Burn). Needs:
counters in telemetry/profile (most events already exist: kills, boss_end, altar_taken, pit_eruption, horde_sample,
wave_clear, weapon_acquired once PR 8 lands), a profile section that persists them (ProfileSaveData - a save version
bump), and a screen. The owner drafts the list in the same ChatGPT round.

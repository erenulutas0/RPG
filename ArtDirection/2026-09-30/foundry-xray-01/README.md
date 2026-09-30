# Hidden enemy silhouettes in the foundry — 2026-09-30

## Why this package

The depth package (`../foundry-depth-mass-01/`) made structures hide the actors behind them. That is correct, but an
enemy fully behind a boiler became invisible until it stepped out, which is a gameplay-readability loss on a phone.

## What changed

- **`Resources/FoundryActorXray.shader`** (`Cryptforge/Foundry Actor Xray`). Pass 1 is Sprites/Default (same
  includes, flip, renderer colour, external alpha). Pass 2 uses `ZTest Greater`, so it draws only where a structure in
  front wrote its depth: an ember inner outline (about 2.5 screen pixels, taken from the sprite's alpha) over a faint
  fill. Vertex alpha still applies (fades); the hit tint does not, so the silhouette stays one colour. Where nothing
  wrote depth - the open floor, ordinary halls - pass 2 draws nothing.
- **`FoundryVisibilityView`** gives every live enemy body one shared, scene-owned material with that shader the first
  time it sees the enemy, remembers the original material, prunes destroyed enemies, returns the originals when the
  view is disabled and destroys the material with the view. `EnemyLookView` exposes its body renderer (`Body`).
- The hero is not given a silhouette: structures in front of him already fade over his body. Health bars are unchanged.
- Test: `FoundryRegionTests` asserts every wave enemy body shares the one two-pass x-ray material while the view runs
  and gets `Sprites/Default` back when it is disabled.

No gameplay, collision, balance, save or telemetry change; static scenery unchanged (17 renderers / 54,168 vertices).

## Evidence

Actual Unity captures in `after/`, matched against `../foundry-depth-mass-01/after/` (same code except this package):

- `depth-enemy-behind-tower.png` — the part of the enemy covered by the south boiler now shows as an ember silhouette;
  the boundary between the normally drawn part and the silhouette follows the dome's edge.
- `start.png` — the open court with eleven enemies: no silhouette appears anywhere.

Gate: EditMode 405/405, PlayMode 130/130, Android build exit 0; integrity PASS 520 project GUIDs (+1, the shader's
meta) / 1,099 / 568. APK 39,340,240 bytes, SHA-256 `BE991765AE0BB13AE6B72BF416A78379F1BD71EDE02353F6F633E5303AB6ED86`,
**installed** on the S23 (installed base.apk hash matches; profile and backup unchanged by the installation; backups in
`TestResults/foundry-xray-install-20260930`).

On device: a short live run with guarded drags to the south-west corner of the boiler compound (`device.png`: the new
boilers, balustrade and lantern post on the phone, enemies drawn normally in the open court). No enemy passed behind a
structure during the captures, so **the silhouette itself has not been seen on the phone yet**. The run then ended in a
defeat to the Forge Warden in Warden's Crucible, which banked 3 gold through the normal save path (profile revision
215 -> 216, gold 32,776 -> 32,779; the game rotated the previous profile into the backup). Not restored: it is newer
progress. Logcat: 0 matches for exceptions or shader errors.

## Cost and limits

- Enemy bodies now use a two-pass shader: one extra draw per enemy body. Not measured on device.
- Only enemy bodies get a silhouette; ground drops (gems, gold, chests) behind structures stay hidden.
- The fill is uniform; enemies overlapping behind the same structure merge into one shape.

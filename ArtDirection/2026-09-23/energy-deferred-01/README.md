# Movement energy deferred

Owner request: remove energy for now because large-map movement feels disruptive; leave a note to reconsider it later.

Current gameplay and default DescentSimulation now share disabled energy rules. Hero base speed stays2.5; continuous movement no longer consumes a budget or drops to40% speed. The MOVE track/fill/label hide on startup. StaminaBar cards are excluded by the same eligibility path for level offers and chest books, without reordering the authored pool. Permanent saves and other combat values are unchanged.

HeroStamina.CurrentRulesEnabled is the single current-rule switch. Explicit enabled instances, old parameters, card assets and historical experiment tests remain, so reintroduction is reversible. Do not re-enable automatically: review map traversal, kiting and alternative enemy pressure first. See docs19/26 and the deferred item in docs15.

Verification before Android gate:390 pure tests, clean compile, graphics traversal1 passed. New tests cover a full minute without drain/slowdown, simulation default parity, excluded energy offers/books, hidden scene HUD and sustained scene movement. The previous two active-energy HUD tests were replaced by the current-rule scene test. Historical pure energy tests remain.

Same five-point isolated Unity traversal:106.7 seconds with energy versus44.5 without. Combat disabled in this fixture, but a live encounter remains, exercising the movement-budget path. This is a deterministic traversal comparison, not an estimate of actual player session duration. unity-no-energy.png and walk.csv are current proof. Full suite/device results follow in docs22/23.


## 2026-09-23 - Energy deferred, verification complete

Owner requested temporary removal of movement energy. Current-rule switch disables spending and empty-speed penalty in scene and default simulation; MOVE HUD hidden; StaminaBar excluded from both offers and books. Historic explicit-budget tests remain. Gate: pure390, compile0 warnings/errors, graphics five-position walk1, EditMode405/405, PlayMode130/130, integrity505 GUIDs/568 scene objects. Actual same-route time106.7 ->44.5seconds; no art/camera/geometry edits. Android exit0, APK29,253,465bytes, SHA256 F0105B61C8454C4867D6CCA47D35E0CF592E4BCA85CEF7748271FAF2037EA67A.


## 2026-09-23 - Energy-free APK ready, device install pending

Built APK F0105B61C8454C4867D6CCA47D35E0CF592E4BCA85CEF7748271FAF2037EA67A after390/405/130 gate. NOT INSTALLED. Fresh read-only device screenshot showed owner progress beyond the assistant handback: paused boss room6/6,35runGold,38/100HP,level4,XP57/70. Asked whether to restart/install or let owner finish; no answer at completion. No tap/swipe/force-stop/install or profile change in this turn. Installed device remains7EE642D10E16A21588E40318D70CCC2F03C3ADFB61FE6CD27BA43851855E42EB, with energy still active. Screenshot TestResults/complete-install-20260923-223213/live/energy-before.png. Inspect fresh before installation; do not assume old profile199 is current. Decision deferred in docs15/19/26, proof in energy-deferred-01.

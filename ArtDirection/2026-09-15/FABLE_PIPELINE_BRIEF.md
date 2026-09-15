# Fable integration brief — dependency after visual contract agreement

This is a scoped follow-up proposal, not a request to start simultaneously with Opus edits to shared files. Read docs/25 and the approved art contract first. Astra supplies the visual assets and layout; implement their import/presentation without changing combat rules.

Propose the smallest LookDefinition adapter for one hero body with optional external frames, and existing generated art as fallback. Start with one character, not an all-content rewrite. Preserve 32 PPU, 32x44 body canvas, bottom-centre pivot, point filtering and separate weapon anchors. Validate dimensions, required poses and references; handle absent optional frames explicitly.

Keep shared sprite/texture ownership separate from per-character animation state. Imported assets must never be destroyed by generated-sprite cleanup. Preserve ILookSprites hit silhouettes, weapon placement, depth sorting and pause behavior. Discuss front/back and mirrored side directions with Astra before requiring new frames; do not silently mirror asymmetric equipment.

Coordinate HeroLookView, EnemyLookView, CombatantView and Gameplay.unity ownership. Cache work overlaps these views and should be performed by the same integration owner, in a separate measured slice. No simultaneous edits by Opus and Fable to these files.

Verify imported vs fallback paths, missing frame handling, anchor/pivot stability, silhouette updates, pause/death behavior and safe scene restart. Follow docs/25's full gate and device rules. Record evidence and limitations in 21/22/23. Complete one imported hero test before expanding to all enemies, weapons and animations.

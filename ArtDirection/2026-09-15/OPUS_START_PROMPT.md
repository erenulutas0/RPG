# Prompt to paste into Opus — one independent technical slice

**Historical prompt; completed by Claude/Ultracode in `127f4de`. Do not run this telemetry slice again.** The subsequent art integration is recorded in `UNITY_INTEGRATION.md` and the latest docs/21–23 entries.

Read docs/25_HANDOFF_2026-09-15.md in full, then docs/01_PRODUCT_VISION.md, 03_GAME_DESIGN_DOCUMENT.md, 06_TECH_ARCHITECTURE_UNITY.md, 10_ANALYTICS_EXPERIMENTATION.md and the current sections of 21, 22 and 23. Read ArtDirection/2026-09-15/REVIEW_AND_SHARED_ROADMAP.md as a proposal; it does not override approved project decisions.

Talk to me in Turkish; write code and docs in English. Astra owns visual direction and art deliverables. You own this technical slice. First report actual tool availability and inspect the current dirty worktree. Preserve other assistants' work.

Implement the handoff's local telemetry slice only. No backend, SDK, network transmission, ads or IAP. Write a bounded local event log beside, but separate from, the existing profile. Reuse current events and the project's composition style; do not rewrite gameplay. Avoid touching HUD, sprite drawing, camera, art assets or Gameplay.unity. If unavoidable coupling requires one of those files, report the smallest required interface before editing it so we can assign one owner.

Use stable content IDs, session/run IDs, event ordering and timestamps. Cover the existing run/room/floor/boss lifecycle, upgrade offered/selected, checkpoint choice, run end, and relevant earned/spent currency events from docs/10. Add only small justified diagnostic fields/events for first kill, chest pickup and ability use if existing signals expose them. Do not invent events for unimplemented onboarding or heroes. Report active combat time separately from choice/pause time; handle application interruption without fabricating a completed session/run. Do not log every frame or every ordinary hit, and do not collect personal/device identifiers.

Bound storage and avoid synchronous per-hit writes. Failed log writes must not stop combat or affect profile integrity. Unsubscribe correctly across restart; event subscriptions must not duplicate. Preserve the profile format and its recovery behavior.

Test ordering/IDs, exactly-once outcomes and selections, restart subscription lifetime, time accounting, bounded storage and recoverable write failures. Follow the gate in docs/25: .NET, UnityCompileCheck, full Unity suites, APK only after passing suites, static verification. Never replace simulation-parity assertions with weakened comparisons. If a required tool is unavailable, quote the failure and do not claim a pass.

Before any phone session announce it, and gate every touch/capture on package focus, awake display and no keyguard; check again after captures. Preserve the owner's profile. Pull and inspect one real session log to verify it agrees with observed outcomes. Record actual evidence in docs/21, 22 and 23; update 15 when complete. Follow repository commit/push rules only after the full gate; keep the unrelated Astra review file out of your commit.

Stop after this slice and report results. As a follow-up recommendation only, propose the smallest seeded upgrade-offer plus one behavioral-upgrade change, with deterministic simulation support. Do not implement extra upgrade content, mastery, chests, performance caching or art changes in this same slice.

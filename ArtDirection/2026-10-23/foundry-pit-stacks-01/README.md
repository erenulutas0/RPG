# Foundry pit vessels: tall banded stacks with fire

Verified 2026-10-23. Baseline `7769e74`.

On the concept each pit vessel carries a tall column that rises over the rail with a fire on its crown. The runtime
hoods ended in a short flue mouth at 1.26. `PitStack` (FoundryRegionMassing.cs) raises each flue as a blackened iron
stack to 3.1 (`PitStackTop`), with brass collars at its foot, midway and under its crown, and a fire basket
(`Brazier`) burning on top - in the existing iron (-10.5) and brazier batches, inside the hood's piece under its own
.8-square depth footprint (height 4.4). The hood itself is unchanged (its tests' 1.1 radius / 1.3 height envelope).

Region 38 renderers / 156,082 vertices (+728).

Images: `before/`, `after/` (39 matched views), `compare-concept.png` (pit close-up before / after / concept).

Verification: 457 .NET / 738 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,459,130 bytes, SHA256
F018FEA82414B73DE3CF4EB539640FD02B9A4D9E0F83F9D720A8BAB3A807FA0C, NOT INSTALLED (the owner is using the phone). Source
freeze `5a3597fcb19ebfc918f8e14c745b9122a2ff0cbc9ffc6973718c678f3bd3bfb6` (1,273 files) taken after the final After capture,
matched after the full suites. Gameplay: none.

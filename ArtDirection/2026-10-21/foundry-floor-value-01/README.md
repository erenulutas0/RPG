# Foundry floor value: the concept's court brightness

Verified 2026-10-21. Baseline `dbcbe70c244c33d7d7cf7f90a64173284537cffc`.

Measured at the full-map framing the court read ~15 % darker than the concept (value .30-.31 vs .35). Both floor
materials are lifted 14 % with their hue balance kept: MAT_FoundryFlagstone .5876 -> .67 (neutral), MAT_FoundryPaving
(.5717/.5894/.6252) -> (.6517/.6719/.7127). After: .33-.35. The mobile fixed4 colour bound (peak light 1.74 x tone 1.435 x
gain) stays at 1.78 < 2, and the flagstone test now asserts it (< 1.9). Hero and enemies stay readable on the lighter floor
(approach views). No geometry change.

Verification: 457 .NET / 732 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,412,761 bytes, SHA256
D2A071024BF8DB260E7E2B314024F7157319063309C8486B6970694BF421BDE3, NOT INSTALLED. Source freeze
`442a2096e6e0cba1d6abf4003dfdd651588cd98708ab7b8fe83f1725a19ee236` matched after the final capture and the full suites.

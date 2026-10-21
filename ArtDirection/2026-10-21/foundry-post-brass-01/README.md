# Foundry rail posts: brass collars and finials

Verified 2026-10-21. Baseline `3ab2d074ac8622920aadd867518f91ed6d4c20e5`.

The concept's rail posts carry brass bands and pointed brass finials. Every plain balustrade post and pit collar post now
has an octagonal brass collar under its cap (circumscribing the .52 post) and a two-stage brass finial on its pyramid's
point (to 1.56), built with the braziers' bronze course on the iron material in the existing brazier ironwork batch
(order -10.5, drawn after the post's stone). Post layout is unchanged (the 1.35 placement screen test is kept); the posts'
depth pieces rise to 1.56. Region 38 renderers (no new batch) / ~162,100 vertices.

At the portrait game camera the bands read clearly; at the 16 px/unit full-map scale they are a small glint, dimmer than
the concept's bright gold (the iron material's light response limits them; a 1.3x polish was the accepted step).
Candidates: `01-rubbed` (braziers' bronze), `02-polished` (identical runtime to After).

Verification: 457 .NET / 732 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,414,225 bytes, SHA256
F89C4F2522C2CCA5D21DF4D48E44BD2AE7DDA9233F7DB758C6E8FC945E4AFC0A, NOT INSTALLED. Source freeze
`af6bbd74f1ca21fbfe9499478003865dfef9e3254b4dc6141c9ba3b14c07ae16` matched after the final capture and the full suites.

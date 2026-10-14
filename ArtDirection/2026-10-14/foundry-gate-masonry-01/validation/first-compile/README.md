# Initial Unity compilation rejection

The isolated compilation succeeded because it compiles project sources and tests together. Unity's separate EditMode assembly correctly rejected a direct reference to the internal FoundrySurface class in the new cornice test (CS0122). No graphics test or suite ran, and no APK was built from this source. The test is repaired to use public FoundryMap projection with the documented raised-landing height; runtime art is unchanged. The failed log and source freeze are retained here. The test snapshot was taken after the repair and is explicitly named repaired-test.cs.txt.

The committed capture.log is a compiler diagnostic excerpt, with the original raw log SHA-256. Unrelated machine, network and licensing diagnostics are omitted. The original raw log is preserved locally in ignored TestResults/gate-masonry-first-compile-raw.log.

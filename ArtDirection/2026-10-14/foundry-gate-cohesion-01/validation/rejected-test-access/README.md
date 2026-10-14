# Rejected Unity test compilation

The initial isolated compile passed because it compiles runtime and test source together. Real Unity compilation correctly rejected a test's direct reference to internal `FoundrySurface.Project` across the assembly boundary. The preserved test source and Editor log show CS0122. The test now independently reconstructs the datum with public `FoundryMap.ToFloor`, half projected Y and the existing raised plateau of1.25. No runtime visibility was broadened and no runtime source changed for this correction.

The failed Editor left a stale lock marker with no running Unity process. A direct Unity test launch handled its own stale marker and then exited normally; subsequent standard launches resumed. The failure is not part of accepted test totals.

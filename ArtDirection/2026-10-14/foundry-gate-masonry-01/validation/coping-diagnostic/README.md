# Managed coping geometry diagnostic

This evidence invokes the repository's compiled geometry methods using .NET 9, without starting Unity. It reproduces a float boundary assertion and is not device or Unity verification.

To reproduce from `E:/MobileGame` after a fresh isolated compile:

1. Copy `Program.cs.txt` to `TestResults/coping-diagnostic/Program.cs`.
2. Copy `coping-diagnostic.csproj.txt` to `TestResults/coping-diagnostic/coping-diagnostic.csproj`.
3. Run `dotnet run --project TestResults/coping-diagnostic/coping-diagnostic.csproj`.

The project references the existing `Tools/UnityCompileCheck/bin/Debug/netstandard2.1/UnityCompileCheck.dll` and pinned Unity managed assemblies. It calls actual private production methods through reflection and writes `shell.json`, `actual.json` and `final-boundary.json` below TestResults. The final check invokes the revised test predicate on all three real profiles and tests an outward eight-ULP counterexample per profile.

`boundary-witnesses.json` additionally records the independent double segment / half-ULP rounding-cell analysis performed on the original shell and actual vertex arrays. Nineteen of the twenty-one vertices rejected by the original barycentric boundary oracle have exact rounding-cell witnesses; two duplicate cap points require the documented inverse/projection roundtrip. See the adjacent `coping-failure-analysis.md` for coordinates and diagnosis.

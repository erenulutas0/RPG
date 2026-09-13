using System.Runtime.CompilerServices;

// The Descent simulation stays internal to the tests: both test assemblies share it, the game never sees it.
[assembly: InternalsVisibleTo("Project.EditModeTests")]
[assembly: InternalsVisibleTo("Project.PlayModeTests")]

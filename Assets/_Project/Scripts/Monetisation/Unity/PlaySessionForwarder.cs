using UnityEngine;

namespace Cryptforge.Monetisation
{
    // The app leaving the foreground and coming back, for the launch's SessionClock (docs/58 7.4): one hidden
    // DontDestroyOnLoad object, made by MonetisationRuntime once a launch, so every scene's trips are counted - the
    // telemetry bridge's OnApplicationPause lives only with a gameplay scene. It only forwards; it writes nothing itself.
    [AddComponentMenu("")]
    public sealed class PlaySessionForwarder : MonoBehaviour
    {
        public const string ObjectName = "Play Session Forwarder";

        private void OnApplicationPause(bool paused) => MonetisationRuntime.ApplicationPaused(paused);
    }
}

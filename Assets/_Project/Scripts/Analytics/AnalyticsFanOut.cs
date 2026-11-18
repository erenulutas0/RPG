using System;
using System.Runtime.ExceptionServices;

namespace Cryptforge.Analytics
{
    // Several sinks as one: the local file log first, the GameAnalytics backend beside it once it exists (docs/59 3.3).
    // Each call reaches every sink in order; a sink that throws does not keep the event from the sinks after it, and the
    // first exception is rethrown (with its own stack) once all have had the call, so a backend's failure never costs the
    // local log a line.
    public sealed class AnalyticsFanOut : IAnalytics
    {
        private readonly IAnalytics[] _sinks;

        public int Count => _sinks.Length;

        public AnalyticsFanOut(params IAnalytics[] sinks)
        {
            if (sinks == null)
                throw new ArgumentNullException(nameof(sinks));
            for (int i = 0; i < sinks.Length; i++)
            {
                if (sinks[i] == null)
                    throw new ArgumentException("A sink is null.", nameof(sinks));
            }
            _sinks = (IAnalytics[])sinks.Clone();
        }

        public void Emit(string name, TelemetryFields fields = null)
        {
            Exception first = null;
            foreach (IAnalytics sink in _sinks)
            {
                try
                {
                    sink.Emit(name, fields);
                }
                catch (Exception exception)
                {
                    first = first ?? exception;
                }
            }
            if (first != null)
                ExceptionDispatchInfo.Capture(first).Throw();
        }

        public void EmitForRun(string runId, string name, TelemetryFields fields = null)
        {
            Exception first = null;
            foreach (IAnalytics sink in _sinks)
            {
                try
                {
                    sink.EmitForRun(runId, name, fields);
                }
                catch (Exception exception)
                {
                    first = first ?? exception;
                }
            }
            if (first != null)
                ExceptionDispatchInfo.Capture(first).Throw();
        }

        public bool Flush()
        {
            bool empty = true;
            Exception first = null;
            foreach (IAnalytics sink in _sinks)
            {
                try
                {
                    empty &= sink.Flush();
                }
                catch (Exception exception)
                {
                    first = first ?? exception;
                }
            }
            if (first != null)
                ExceptionDispatchInfo.Capture(first).Throw();
            return empty;
        }
    }
}

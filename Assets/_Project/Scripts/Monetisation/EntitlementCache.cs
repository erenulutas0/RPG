using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cryptforge.Monetisation
{
    // The products the store last said this account owns, as the cache holds them.
    public sealed class EntitlementCacheData
    {
        public const int CurrentVersion = 1;

        private readonly List<string> _owned = new List<string>();

        // Bumped by every save; the copy with the highest revision is the one loaded.
        public long Revision { get; internal set; }
        // When the store last confirmed the list (a successful query or a purchase), UTC to the second; null before it ever
        // has. A diagnostic: nothing expires by it (docs/58 3.3 keeps the skip offline for as long as the cache holds it).
        public DateTime? ConfirmedUtc { get; set; }
        // In the order the store gave them.
        public IReadOnlyList<string> OwnedProductIds => _owned;

        public bool Owns(string productId) => !string.IsNullOrEmpty(productId) && _owned.Contains(productId);

        // True when the product was not owned before.
        public bool Grant(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("A product id is required.", nameof(productId));
            if (_owned.Contains(productId))
                return false;
            _owned.Add(productId);
            return true;
        }

        // The store's whole list in place of the cached one (a successful query). True when the set changed.
        public bool ReplaceWith(IEnumerable<string> productIds)
        {
            var next = new List<string>();
            if (productIds != null)
            {
                foreach (string id in productIds)
                {
                    if (!string.IsNullOrEmpty(id) && !next.Contains(id))
                        next.Add(id);
                }
            }
            bool changed = next.Count != _owned.Count;
            for (int i = 0; !changed && i < next.Count; i++)
                changed = !_owned.Contains(next[i]);
            _owned.Clear();
            _owned.AddRange(next);
            return changed;
        }
    }

    // entitlements.json beside the profile and apart from its save (docs/58 3.3: "restoring a purchase is separate from
    // restoring a save"), written as SmallSaveFile writes (a temp file, a backup, the highest revision loaded), every
    // number and the date in the invariant culture. It is what the game believes offline: on start the store's query
    // replaces it when the store answers (EntitlementService), and stands in for it when the store cannot. A missing or
    // unreadable cache reads as owning nothing until the store answers; the purchase itself is never lost, it is the
    // store's.
    public sealed class EntitlementCache
    {
        public const string FileName = "entitlements.json";
        // UTC to the second, with the Z: "2026-11-19T03:04:05Z".
        public const string TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'";

        private readonly SmallSaveFile _file;
        private readonly List<string> _problems = new List<string>();

        public string Directory => _file.Directory;
        public IReadOnlyList<string> LastLoadProblems => _problems;
        public bool LastLoadWasNew { get; private set; }

        public EntitlementCache(string directory)
        {
            _file = new SmallSaveFile(directory, FileName);
        }

        public EntitlementCacheData Load()
        {
            _problems.Clear();
            EntitlementCacheData best = null;
            foreach (KeyValuePair<string, string> copy in _file.ReadCopies(_problems))
            {
                EntitlementCacheData data = Read(copy.Value);
                if (data == null)
                {
                    _problems.Add(copy.Key + " is not a readable entitlement cache of version " +
                                  EntitlementCacheData.CurrentVersion.ToString(CultureInfo.InvariantCulture) + " or older.");
                    continue;
                }
                if (best == null || data.Revision > best.Revision)
                    best = data;
            }
            LastLoadWasNew = best == null;
            return best ?? new EntitlementCacheData();
        }

        // Bumps the revision and writes. May throw IOException or UnauthorizedAccessException.
        public void Save(EntitlementCacheData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.Revision++;
            _file.Write(Write(data));
        }

        public static string Write(EntitlementCacheData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return new FlatJson.Writer()
                .Field("saveVersion", EntitlementCacheData.CurrentVersion)
                .Field("revision", data.Revision)
                .Field("confirmedUtc", data.ConfirmedUtc.HasValue ? FormatTime(data.ConfirmedUtc.Value) : null)
                .Field("ownedProductIds", data.OwnedProductIds)
                .Finish();
        }

        // The cache the text holds; null for text that is not a JSON object or a version this build does not know. A date
        // that does not parse reads as never confirmed; the products stand.
        public static EntitlementCacheData Read(string json)
        {
            Dictionary<string, object> o = FlatJson.Read(json);
            if (o == null)
                return null;
            int version = FlatJson.Int(o, "saveVersion", 0);
            if (version < 1 || version > EntitlementCacheData.CurrentVersion)
                return null;
            var data = new EntitlementCacheData { Revision = Math.Max(0, FlatJson.Long(o, "revision", 0)) };
            data.ReplaceWith(FlatJson.Strings(o, "ownedProductIds"));
            if (TryParseTime(FlatJson.String(o, "confirmedUtc"), out DateTime confirmed))
                data.ConfirmedUtc = confirmed;
            return data;
        }

        // A local time is converted; an unspecified one is taken as UTC (TelemetryJson.Timestamp's rule).
        public static string FormatTime(DateTime time)
        {
            DateTime utc = time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : time;
            return utc.ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        public static bool TryParseTime(string text, out DateTime utc)
        {
            if (!string.IsNullOrEmpty(text) && DateTime.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc))
                return true;
            utc = default(DateTime);
            return false;
        }
    }
}

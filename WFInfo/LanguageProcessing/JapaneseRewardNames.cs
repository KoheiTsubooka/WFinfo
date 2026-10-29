using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Reward-only aliases. Japanese Warframe keeps Prime proper names in Latin script.
    /// Only return keys supplied by the application's canonical item database.
    /// </summary>
    internal static class JapaneseRewardNames
    {
        // Confirmed in tests/japanese-rewards.json's real game screenshots.
        private static readonly KeyValuePair<string, string>[] Suffixes =
        {
            new KeyValuePair<string, string>(" Blueprint", "の設計図"),
            new KeyValuePair<string, string>(" Barrel", "バレル"),
            new KeyValuePair<string, string>(" Receiver", "レシーバー"),
            new KeyValuePair<string, string>(" Stock", "ストック"),
            new KeyValuePair<string, string>(" Handle", "ハンドル"),
            new KeyValuePair<string, string>(" Upper Limb", "トップリム"),
            new KeyValuePair<string, string>(" Lower Limb", "ボトムリム"),
            new KeyValuePair<string, string>(" Neuroptics", "ニューロティックの設計図"),
            new KeyValuePair<string, string>(" Chassis", "シャーシの設計図")
        };

        internal static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            // NFKC composes half-width kana/dakuten and full-width Latin characters.
            return new string(text.Normalize(NormalizationForm.FormKC)
                .Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }

        internal static string GetAlias(string canonicalName)
        {
            if (canonicalName == "Forma Blueprint") return "フォーマの設計図";
            if (string.IsNullOrEmpty(canonicalName) || !canonicalName.Contains(" Prime")) return null;
            // nameData/marketData use e.g. "Yareli Prime Chassis", without Blueprint.
            foreach (var suffix in Suffixes)
            {
                if (canonicalName.EndsWith(suffix.Key, StringComparison.Ordinal))
                    return canonicalName.Substring(0, canonicalName.Length - suffix.Key.Length) + suffix.Value;
            }
            return null; // Unknown component: fail closed, never invent a translation.
        }

        internal static string Match(string text, IEnumerable<string> canonicalNames,
            out int distance, out bool ambiguous)
        {
            distance = 9999;
            ambiguous = false;
            string input = Normalize(text);
            if (input.Length < 4 || canonicalNames == null) return null;
            string best = null;
            foreach (string key in canonicalNames.Where(k => k != null).Distinct(StringComparer.Ordinal))
            {
                string alias = Normalize(GetAlias(key));
                if (alias.Length == 0) continue;
                int value = Distance(input, alias);
                // At most one edit for long names; short names (Forma) must be exact.
                // Do not use the broad 50% threshold used by other locales.
                int limit = alias.Length >= 10 ? 1 : 0;
                if (value > limit) continue;
                if (value < distance)
                {
                    distance = value;
                    best = key;
                    ambiguous = false;
                }
                else if (value == distance) ambiguous = true;
            }
            // A tie between distinct canonical keys must not select an arbitrary item.
            if (ambiguous) return null;
            return best;
        }

        private static int Distance(string a, string b)
        {
            if (Math.Abs(a.Length - b.Length) > 1) return 9999;
            var row = Enumerable.Range(0, b.Length + 1).ToArray();
            for (int i = 1; i <= a.Length; i++)
            {
                int previous = row[0];
                row[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int old = row[j];
                    row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1),
                        previous + (a[i - 1] == b[j - 1] ? 0 : 1));
                    previous = old;
                }
            }
            return row[b.Length];
        }
    }
}

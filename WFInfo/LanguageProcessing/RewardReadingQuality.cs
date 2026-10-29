using System;
using System.Collections.Generic;
using System.Linq;

namespace WFInfo.LanguageProcessing
{
    internal static class RewardReadingQuality
    {
        // Negative entries mean unrecognized. More independently recognized slots wins;
        // equal coverage requires fewer OCR edits.
        internal static bool PreferAlternative(int[] current, int[] alternative)
        {
            int recognized = current.Count(d => d >= 0);
            int otherRecognized = alternative.Count(d => d >= 0);
            return otherRecognized > recognized ||
                (otherRecognized == recognized &&
                 alternative.Where(d => d >= 0).Sum() < current.Where(d => d >= 0).Sum());
        }

        // Only combine readings when two distinct items anchor the same slot positions,
        // the number of slots agrees, and no jointly recognized slot contradicts the other.
        internal static string[] FillMissing(string[] preferredText, string[] preferredKeys,
            string[] otherText, string[] otherKeys)
        {
            if (preferredText.Length != preferredKeys.Length || otherText.Length != otherKeys.Length)
                throw new ArgumentException("Each reading must have a key for every slot.");
            var result = (string[])preferredText.Clone();
            if (preferredKeys.Length != otherKeys.Length) return result;
            var anchors = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < preferredKeys.Length; i++)
            {
                if (preferredKeys[i] == null || otherKeys[i] == null) continue;
                if (preferredKeys[i] != otherKeys[i]) return result;
                anchors.Add(preferredKeys[i]);
            }
            if (anchors.Count < 2) return result;
            for (int i = 0; i < result.Length; i++)
                if (preferredKeys[i] == null && otherKeys[i] != null) result[i] = otherText[i];
            return result;
        }
    }
}

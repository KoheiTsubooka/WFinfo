using System.Linq;

namespace WFInfo.LanguageProcessing
{
    internal static class RewardReadingQuality
    {
        // Negative entries mean unrecognized. More independently recognized slots wins;
        // equal coverage requires fewer OCR edits. Never combine two layouts' slots.
        internal static bool PreferAlternative(int[] current, int[] alternative)
        {
            int recognized = current.Count(d => d >= 0);
            int otherRecognized = alternative.Count(d => d >= 0);
            return otherRecognized > recognized ||
                (otherRecognized == recognized &&
                 alternative.Where(d => d >= 0).Sum() < current.Where(d => d >= 0).Sum());
        }
    }
}

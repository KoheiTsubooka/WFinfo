using System;
using System.IO;
using System.Security.Cryptography;

namespace WFInfo
{
    /// <summary>Verified, pinned official LSTM model; independent of the market API locale.</summary>
    internal static class JapaneseOcrModel
    {
        internal const string Language = "jpn";
        internal const string Revision = "87416418657359cb625c412a48b6e1d6d41c29bd";
        internal const string Sha256 = "1f5de9236d2e85f5fdf4b3c500f2d4926f8d9449f28f5394472d9e8d83b91b4d";
        internal const string Url = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/" + Revision + "/jpn.traineddata";

        internal static bool IsValid(string path)
        {
            if (!File.Exists(path)) return false;
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "")
                    .Equals(Sha256, StringComparison.OrdinalIgnoreCase);
        }

        internal static void Ensure(string directory, Action<string, string> download)
        {
            Directory.CreateDirectory(directory);
            string target = Path.Combine(directory, Language + ".traineddata");
            if (IsValid(target)) return;
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                download(Url, temporary);
                if (!IsValid(temporary)) throw new InvalidDataException("Japanese OCR model SHA-256 verification failed.");
                // The old file remains intact if download or verification fails.
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}

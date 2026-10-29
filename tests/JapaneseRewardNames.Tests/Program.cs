using System.Text.Json;
using WFInfo;
using WFInfo.LanguageProcessing;

int checks = 0;
void Check(bool value, string label)
{
    if (!value) throw new Exception("FAIL: " + label);
    checks++;
}
string tests = Path.GetFullPath(args.Length > 0 ? args[0] : "tests");
var examples = new List<(string Text, string Key)>();
using (var map = JsonDocument.Parse(File.ReadAllText(Path.Combine(tests, "japanese-rewards.json"))))
foreach (var path in map.RootElement.GetProperty("scenarios").EnumerateArray())
{
    using var spec = JsonDocument.Parse(File.ReadAllText(Path.Combine(tests, path.GetString() + ".json")));
    foreach (var part in spec.RootElement.GetProperty("parts").EnumerateObject())
        examples.Add((spec.RootElement.GetProperty("displayed_text").GetProperty(part.Name).GetString(), part.Value.GetString()));
}
var keys = examples.Select(e => e.Key).Distinct().ToArray();
foreach (var example in examples)
{
    string actual = JapaneseRewardNames.Match(example.Text, keys, out int distance, out bool ambiguous);
    Check(actual == example.Key && distance == 0 && !ambiguous, example.Text);
}
Check(examples.Count == 23, "all 23 manually transcribed reward labels executed");
Check(JapaneseRewardNames.Normalize("Ｌｅｘ　Ｐｒｉｍｅ\r\nの設計図") == "lexprimeの設計図", "full width and wrapping");
Check(JapaneseRewardNames.Normalize("ﾊﾞﾚﾙ") == "バレル", "half-width dakuten composition");
Check(JapaneseRewardNames.Match("VeloxPrimeﾊﾞﾚﾙ", keys, out _, out _) == "Velox Prime Barrel", "half-width alias");
Check(JapaneseRewardNames.Match("BurstonPrimeの設計囚", keys, out int d, out _) == "Burston Prime Blueprint" && d == 1, "one OCR substitution");
foreach (string text in new[] { "", "設計図", "Prime", "知らないアイテム", "Unknown Primeの設計図", "Lex Prime", "フォーマ", "フォーマの設計囚", "Lex Prime システムの設計図", "Paris Prime リム" })
    Check(JapaneseRewardNames.Match(text, keys, out _, out _) == null, "reject: " + text);
Check(JapaneseRewardNames.Match("Paris Prime ボトムリム", new[] { "Paris Prime Upper Limb" }, out _, out _) == null, "do not confuse upper/lower limb");
Check(JapaneseRewardNames.Match("Yareli Primeの設計図", new[] { "Yareli Prime Chassis" }, out _, out _) == null, "do not confuse blueprint/component");
Check(JapaneseRewardNames.Match("Fang Primeの設計図", new[] { "Fang Prime Blueprint", "Fang Prime Blueprint" }, out _, out bool duplicate) == "Fang Prime Blueprint" && !duplicate, "duplicate database keys");
Check(JapaneseRewardNames.Match("Soma Primeの設計図", new[] { "Soma Prime Blueprint", "Soma Prime BlueprintX" }, out _, out _) == "Soma Prime Blueprint", "unsupported suffix ignored");
Check(JapaneseRewardNames.Match("Aba Primeの設計図", new[] { "Abb Prime Blueprint", "Abc Prime Blueprint" }, out _, out bool tie) == null && tie, "ambiguous distinct keys rejected");
Check(JapaneseRewardNames.Match("Aba Primeの設計図", new[] { "Abb Prime Blueprint", "Aba Prime Blueprint" }, out int exact, out bool exactTie) == "Aba Prime Blueprint" && exact == 0 && !exactTie, "exact beats approximate");
Check(JapaneseRewardNames.Match("Lex Primeの設計図", Array.Empty<string>(), out _, out _) == null, "no fabricated item without database");

Check(RewardReadingQuality.PreferAlternative(new[] { -1, -1, -1, -1 }, new[] { 0, 1, 0, 0 }), "empty wrong crop loses to recognized text");
Check(RewardReadingQuality.PreferAlternative(new[] { 1, 0, 0, -1 }, new[] { 0, 0, 0, 1 }), "recover fourth slot");
Check(!RewardReadingQuality.PreferAlternative(new[] { 0, 0, 0, 1 }, new[] { 0, 0, -1, -1 }), "do not replace with lower coverage");
Check(RewardReadingQuality.PreferAlternative(new[] { 1, 1, 0 }, new[] { 0, 0, 0 }), "prefer intact names at equal coverage");
Check(!RewardReadingQuality.PreferAlternative(new[] { 0, 0, 0 }, new[] { 0, 0, 0 }), "stable tie");
Check(!RewardReadingQuality.PreferAlternative(new[] { -1, -1 }, new[] { -1, -1, -1, -1 }), "blank extra slots do not improve score");

var preferredText = new[] { "Lavos Primeの設計図", "unreadable", "Fang Primeの設計図", "Akbronco Primeの設計図" };
var otherText = new[] { "vos Primeの設計図", "Sarofang Prime ハン\nドル", "Fang Primeの設計図", "Akbronco Primeの設計図" };
string[] MatchKeys(string[] readings) => readings.Select(t => JapaneseRewardNames.Match(t, keys, out _, out _)).ToArray();
var combined = RewardReadingQuality.FillMissing(preferredText, MatchKeys(preferredText), otherText, MatchKeys(otherText));
Check(MatchKeys(combined).SequenceEqual(new[] { "Lavos Prime Blueprint", "Sarofang Prime Handle", "Fang Prime Blueprint", "Akbronco Prime Blueprint" }), "complementary crops recover all four slots");
Check(preferredText[1] == "unreadable", "combining does not mutate original reading");
Check(RewardReadingQuality.FillMissing(preferredText, new[] { "Lavos", null, "Fang", "Akbronco" }, otherText, new[] { "Conflict", "Sarofang", "Fang", "Akbronco" }).SequenceEqual(preferredText), "conflicting recognized item forbids combining");
Check(RewardReadingQuality.FillMissing(preferredText, new[] { "Lavos", null, "Fang", "Akbronco" }, otherText, new[] { null, "Sarofang", "Fang", null }).SequenceEqual(preferredText), "one anchor is insufficient");
Check(RewardReadingQuality.FillMissing(preferredText, new[] { null, null, "Fang", "Fang" }, otherText, new[] { "Lavos", "Sarofang", "Fang", "Fang" }).SequenceEqual(preferredText), "repeated same item is not two distinct anchors");
Check(RewardReadingQuality.FillMissing(preferredText, MatchKeys(preferredText), otherText.Take(3).ToArray(), MatchKeys(otherText).Take(3).ToArray()).SequenceEqual(preferredText), "different player counts must not combine");
Check(RewardReadingQuality.FillMissing(new[] { "bad", "bad" }, new string[] { null, null }, new[] { "a", "b" }, new[] { "A", "B" }).SequenceEqual(new[] { "bad", "bad" }), "no shared anchors must not combine");

string temp = Path.Combine(Path.GetTempPath(), "wfinfo-ja-model-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    string target = Path.Combine(temp, "jpn.traineddata");
    File.WriteAllText(target, "previous invalid model");
    try
    {
        JapaneseOcrModel.Ensure(temp, (_, path) => File.WriteAllText(path, "bad download"));
        throw new Exception("bad model was accepted");
    }
    catch (InvalidDataException) { checks++; }
    Check(File.ReadAllText(target) == "previous invalid model", "failed verification preserves old file");
    Check(Directory.GetFiles(temp).Length == 1, "failed verification cleans temporary file");
    try
    {
        JapaneseOcrModel.Ensure(temp, (_, path) => { File.WriteAllText(path, "partial"); throw new IOException("network failed"); });
        throw new Exception("download failure was swallowed");
    }
    catch (IOException) { checks++; }
    Check(Directory.GetFiles(temp).Length == 1, "failed download cleans temporary file");
    // Optional locally downloaded official model verifies install, repair, and idempotency.
    if (args.Length > 1)
    {
        string source = Path.GetFullPath(args[1]);
        Check(JapaneseOcrModel.IsValid(source), "official model checksum");
        JapaneseOcrModel.Ensure(temp, (_, path) => File.Copy(source, path));
        Check(JapaneseOcrModel.IsValid(target), "atomic replacement with verified model");
        JapaneseOcrModel.Ensure(temp, (_, _) => throw new Exception("unnecessary download"));
        checks++;
    }
}
finally { Directory.Delete(temp, true); }
Console.WriteLine($"PASS: {checks} checks; 23 manually transcribed labels. No image OCR or Windows runtime exercised.");

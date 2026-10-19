using System;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Storage;
namespace VoiceTools.Settings
{
 public static class AppSettings
 {
  private static Windows.Foundation.Collections.IPropertySet Values => ApplicationData.Current.LocalSettings.Values;
  public static event Action LanguageChanged;
  public static string Language { get => Values["Language"] as string == "en-US" ? "en-US" : "zh-CN"; set { Values["Language"] = value == "en-US" ? "en-US" : "zh-CN"; Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = (string)Values["Language"]; LanguageChanged?.Invoke(); } }
  public static bool ShowGender { get => !(Values["ShowGender"] is bool) || (bool)Values["ShowGender"]; set => Values["ShowGender"] = value; }
  public static string Scale { get { var value = Values["Scale"] as string; return value == "Linear" || value == "Mel" ? value : "ERB"; } set => Values["Scale"] = value; }
  public static string InputDevice { get => Values["InputDevice"] as string ?? ""; set => Values["InputDevice"] = value; }
  public static string OutputDevice { get => Values["OutputDevice"] as string ?? ""; set => Values["OutputDevice"] = value; }
  public static string InputName { get => Values["InputName"] as string ?? ""; set => Values["InputName"] = value; }
  public static string OutputName { get => Values["OutputName"] as string ?? ""; set => Values["OutputName"] = value; }
  private static string SentencesPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "sentences-" + Language + ".txt");
  public static string[] Sentences {
   get {
    if (!File.Exists(SentencesPath)) return PracticeTexts.Sentences(Language);
    return File.ReadAllLines(SentencesPath).Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line))).ToArray();
   }
  }
  public static void SaveSentences(string[] sentences)
  {
   if (sentences == null || sentences.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(Locale.T("语句不能为空。"));
   File.WriteAllLines(SentencesPath, sentences.Select(text => Convert.ToBase64String(Encoding.UTF8.GetBytes(text.Trim()))));
  }
  public static string PassagePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "analysis-text-" + Language + ".txt");
  public static string Passage {
   get {
    if (!File.Exists(PassagePath)) return PracticeTexts.DefaultPassage(Language);
    var saved = File.ReadAllText(PassagePath);
    // Earlier versions saved the built-in passage when recording started.
    if (Language == "zh-CN" && NormalizePassage(saved) == NormalizePassage(PracticeTexts.LegacyChinesePassage))
     return PracticeTexts.DefaultPassage(Language);
    return saved;
   }
  }
  private static string NormalizePassage(string text) { return text.Replace("\r\n", "\n").Trim(); }
  public static void SavePassage(string text)
  {
   if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException(Locale.T("请填写朗读文本。"));
   // A separate file avoids LocalSettings' small per-value size limit.
   if (NormalizePassage(text) == NormalizePassage(PracticeTexts.DefaultPassage(Language))) {
    if (File.Exists(PassagePath)) File.Delete(PassagePath);
   } else File.WriteAllText(PassagePath, text);
  }
 }
}


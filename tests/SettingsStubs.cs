using System.Collections.Generic;
namespace Windows.Foundation.Collections {
 public interface IPropertySet { object this[string key] { get; set; } }
 public sealed class TestProperties : IPropertySet {
  private readonly Dictionary<string,object> data = new Dictionary<string,object>();
  public object this[string key] { get { object value; return data.TryGetValue(key,out value) ? value : null; } set { data[key]=value; } }
 }
}
namespace Windows.Storage {
 public sealed class ApplicationData {
  public static ApplicationData Current { get; } = new ApplicationData();
  public TestSettings LocalSettings { get; } = new TestSettings();
  public TestFolder LocalFolder { get; } = new TestFolder();
 }
 public sealed class TestSettings { public Windows.Foundation.Collections.IPropertySet Values { get; } = new Windows.Foundation.Collections.TestProperties(); }
 public sealed class TestFolder { public string Path { get; set; } }
}
namespace Windows.Globalization { public static class ApplicationLanguages { public static string PrimaryLanguageOverride { get; set; } } }
namespace VoiceTools.Settings { public static class Locale { public static string T(string value) { return value; } } }

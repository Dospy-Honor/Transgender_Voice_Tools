using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using VoiceTools.Settings;
namespace VoiceTools
{
 public sealed partial class MainPage : Page
 {
  public MainPage()
  {
   InitializeComponent(); OpenVoicePage();
   Loaded += (s, e) => { AppSettings.LanguageChanged += RefreshLanguage; RefreshLanguage(); };
   Unloaded += (s, e) => AppSettings.LanguageChanged -= RefreshLanguage;
  }
  private void RefreshLanguage() { Locale.Apply(this); }
  public void OpenVoicePage()
  {
   // Navigate explicitly: initial display must not depend on SelectionChanged firing.
   if (!(MainFrame.Content is Voice_Page)) MainFrame.Navigate(typeof(Voice_Page));
   ((Voice_Page)MainFrame.Content).ShowSection("Pitch");
   Listbox.SelectedItem = HomePage;
   TitileTextBlock.Text = "Voice Tools";
  }
  private void View_Click(object sender, RoutedEventArgs e) { View.IsPaneOpen = !View.IsPaneOpen; }
  private void ChooseChanged(object sender, SelectionChangedEventArgs e)
  {
   var selected = Listbox.SelectedItem as ListBoxItem;
   if (selected == null) return;
   string section = (string)selected.Tag;
   if (section == "Settings") { if (!(MainFrame.Content is Settings_Page)) MainFrame.Navigate(typeof(Settings_Page)); }
   else if (section == "Analysis") { if (!(MainFrame.Content is Analysis_Page)) MainFrame.Navigate(typeof(Analysis_Page)); }
   else {
    if (!(MainFrame.Content is Voice_Page)) MainFrame.Navigate(typeof(Voice_Page));
    ((Voice_Page)MainFrame.Content).ShowSection(section);
   }
   TitileTextBlock.Text = section == "Analysis" ? Locale.T("分析") : section == "Settings" ? Locale.T("设置") : section == "Tone" ? Locale.T("音调") : section == "Volume" ? Locale.T("音量") : section == "Stats" ? Locale.T("统计") : "Voice Tools";
  }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }
    }
}






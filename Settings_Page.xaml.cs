using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using VoiceTools.Settings;
namespace VoiceTools
{
 public sealed partial class Settings_Page : Page
 {
  private bool loading = true, devicesLoading, textDirty;
  public sealed class DeviceOption { public string Id { get; set; } public string Name { get; set; } }
  public Settings_Page()
  {
   InitializeComponent();
   LanguageChoice.SelectedIndex = AppSettings.Language == "en-US" ? 1 : 0;
   GenderChoice.IsOn = AppSettings.ShowGender;
   ScaleChoice.SelectedIndex = AppSettings.Scale == "ERB" ? 0 : AppSettings.Scale == "Mel" ? 1 : 2;
   var saved = ApplicationData.Current.LocalSettings.Values["ReplaySeconds"];
   int seconds = saved is int ? (int)saved : 10;
   ReplayDuration.SelectedIndex = seconds == 5 ? 0 : seconds == 20 ? 2 : seconds == 30 ? 3 : 1;
   AnalysisText.Text = AppSettings.Passage;
   loading = false;
  }
  private async void Page_Loaded(object sender, RoutedEventArgs e) { Locale.Apply(this); await LoadDevices(); }
  private async void Language_Changed(object sender, SelectionChangedEventArgs e)
  {
   if (loading) return;
   try {
    if (textDirty) AppSettings.SavePassage(AnalysisText.Text);
    AppSettings.Language = (string)((ComboBoxItem)LanguageChoice.SelectedItem).Tag;
    loading = true; AnalysisText.Text = AppSettings.Passage; textDirty = false; loading = false;
    TextStatus.Text = ""; Locale.Apply(this); await LoadDevices();
   } catch (Exception ex) {
    loading = true; LanguageChoice.SelectedIndex = AppSettings.Language == "en-US" ? 1 : 0; loading = false;
    TextStatus.Text = ex.Message;
   }
  }
  private void OpenSentences_Click(object sender, RoutedEventArgs e)
  {
   NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
   Frame.Navigate(typeof(Sentences_Page));
  }
  private void Gender_Changed(object sender, RoutedEventArgs e) { if (!loading) AppSettings.ShowGender = GenderChoice.IsOn; }
  private void Scale_Changed(object sender, SelectionChangedEventArgs e) { if (!loading) AppSettings.Scale = (string)((ComboBoxItem)ScaleChoice.SelectedItem).Tag; }
  private void Duration_Changed(object sender, SelectionChangedEventArgs e)
  {
   if (!loading && ReplayDuration.SelectedItem is ComboBoxItem item) ApplicationData.Current.LocalSettings.Values["ReplaySeconds"] = int.Parse((string)item.Tag);
  }
  private void Passage_Changed(object sender, TextChangedEventArgs e) { if (!loading) { textDirty = true; TextStatus.Text = Locale.T("文本尚未保存。"); } }
  private void SaveText_Click(object sender, RoutedEventArgs e)
  {
   try { AppSettings.SavePassage(AnalysisText.Text); textDirty = false; TextStatus.Text = Locale.T("文本已保存。"); }
   catch (Exception ex) { TextStatus.Text = ex.Message; }
  }
  private void ResetText_Click(object sender, RoutedEventArgs e) { AnalysisText.Text = PracticeTexts.DefaultPassage(AppSettings.Language); SaveText_Click(sender, e); }
  private async void Refresh_Click(object sender, RoutedEventArgs e) { await LoadDevices(); }
  private async Task LoadDevices()
  {
   if (devicesLoading) return;
   devicesLoading = true; RefreshButton.IsEnabled = InputChoice.IsEnabled = OutputChoice.IsEnabled = false;
   try {
    var inputs = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioCaptureSelector());
    var outputs = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector());
    BindDevices(InputChoice, inputs, AppSettings.InputDevice, AppSettings.InputName);
    BindDevices(OutputChoice, outputs, AppSettings.OutputDevice, AppSettings.OutputName);
    DeviceStatus.Text = "";
   } catch (Exception ex) { DeviceStatus.Text = Locale.T("无法读取设备：") + ex.Message; }
   finally { devicesLoading = false; RefreshButton.IsEnabled = InputChoice.IsEnabled = OutputChoice.IsEnabled = true; }
  }
  private void BindDevices(ComboBox box, DeviceInformationCollection devices, string selected, string savedName)
  {
   var options = new List<DeviceOption> { new DeviceOption { Id = "", Name = Locale.T("系统默认") } };
   options.AddRange(devices.Where(d => d.IsEnabled).Select(d => new DeviceOption { Id = d.Id, Name = d.Name }));
   if (!options.Any(d => d.Id == selected)) options.Add(new DeviceOption { Id = selected, Name = savedName + Locale.T("（设备不可用）") });
   box.ItemsSource = options; box.SelectedItem = options.First(d => d.Id == selected);
  }
  private void Input_Changed(object sender, SelectionChangedEventArgs e)
  {
   if (devicesLoading || !(InputChoice.SelectedItem is DeviceOption item)) return;
   AppSettings.InputDevice = item.Id; AppSettings.InputName = item.Name;
  }
  private void Output_Changed(object sender, SelectionChangedEventArgs e)
  {
   if (devicesLoading || !(OutputChoice.SelectedItem is DeviceOption item)) return;
   AppSettings.OutputDevice = item.Id; AppSettings.OutputName = item.Name;
  }
 }
}

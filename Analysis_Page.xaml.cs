using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using VoiceTools.Audio;
using VoiceTools.Settings;

namespace VoiceTools
{
 public sealed partial class Analysis_Page : Page
 {
  private readonly SemaphoreSlim operation = new SemaphoreSlim(1, 1);
  private readonly DispatcherTimer clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
  private readonly Stopwatch elapsed = new Stopwatch();
  private readonly RoutedAudioPlayer Player = new RoutedAudioPlayer();
  private MediaCapture capture;
  private StorageFile recording;
  private IRandomAccessStream replay;
  private CancellationTokenSource cancellation = new CancellationTokenSource();
  private bool active, suspended, isRecording, owned, working, listening;
  public Analysis_Page()
  {
   InitializeComponent(); Player.MediaEnded += Player_Ended; Player.MediaFailed += Player_Failed; ConfigureGauge();
   Passage.Text = AppSettings.Passage;
   clock.Tick += (s, e) => TimerText.Text = elapsed.Elapsed.ToString(@"mm\:ss");
  }
  private void Page_Loaded(object sender, RoutedEventArgs e)
  {
   Locale.Apply(this);
   active = true; Application.Current.Suspending += Suspending; Application.Current.Resuming += Resuming;
  }
  private async void Page_Unloaded(object sender, RoutedEventArgs e)
  {
   active = false; cancellation.Cancel();
   Application.Current.Suspending -= Suspending; Application.Current.Resuming -= Resuming;
   await Cleanup(true); Player.Dispose();
  }
  private async void Suspending(object sender, SuspendingEventArgs e)
  {
   suspended = true; cancellation.Cancel(); var deferral = e.SuspendingOperation.GetDeferral();
   try { await Cleanup(false); StatusText.Text = Locale.T("录音已中断并保留，恢复后可分析已有录音。"); }
   finally { deferral.Complete(); }
  }
  private async void Resuming(object sender, object e)
  {
   await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { suspended = false; UpdateControls(); });
  }
  private async Task Run(Func<Task> action)
  {
   if (!active || suspended || !await operation.WaitAsync(0)) return;
   working = true; UpdateControls();
   cancellation.Dispose(); cancellation = new CancellationTokenSource();
   try { await action(); }
   catch (OperationCanceledException) { }
   catch (Exception ex) { if (active) StatusText.Text = Locale.T("无法完成操作：") + Locale.T(ex.Message); }
   finally { working = false; AnalysisProgress.Visibility = Visibility.Collapsed; UpdateControls(); operation.Release(); }
  }
  private void UpdateControls()
  {
   bool available = !working && !suspended;
   RecordButton.IsEnabled = available;
   ImportButton.IsEnabled = available && !isRecording;
   AnalyzeButton.IsEnabled = available && !isRecording && recording != null;
   SaveButton.IsEnabled = available && recording != null;
   ListenButton.IsEnabled = available && recording != null;
   Passage.IsReadOnly = isRecording || working;
   RecordLabel.Text = isRecording ? Locale.T("停止并分析") : Locale.T("录制全文");
   RecordIcon.Symbol = isRecording ? Symbol.Stop : Symbol.Microphone;
  }
  private async void Record_Click(object sender, RoutedEventArgs e)
  {
   await Run(async () => {
    StopPlayback();
    if (isRecording) { await StopRecording(); await Analyze(); return; }
    AppSettings.SavePassage(Passage.Text);
    await RemoveOwnedFile(); recording = null;
    capture = new MediaCapture();
    try {
     var inputDevice = await AudioDevices.SelectedAsync(true);
     await capture.InitializeAsync(new MediaCaptureInitializationSettings { StreamingCaptureMode = StreamingCaptureMode.Audio, AudioDeviceId = inputDevice?.Id ?? "" });
     cancellation.Token.ThrowIfCancellationRequested();
     recording = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("VoiceTools-reading-" + Guid.NewGuid().ToString("N") + ".wav", CreationCollisionOption.FailIfExists);
     owned = true;
     var profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
     profile.Audio = AudioEncodingProperties.CreatePcm(16000, 1, 16);
     capture.Failed += Capture_Failed;
     await capture.StartRecordToStorageFileAsync(profile, recording);
     isRecording = true;
     if (!active || suspended) { await StopRecording(); return; }
     elapsed.Restart(); TimerText.Text = "00:00"; clock.Start(); StatusText.Text = Locale.T("正在录制全文 · 点击红色按钮结束并分析");
    } catch {
     if (isRecording) await StopRecording();
     else { capture?.Dispose(); capture = null; }
     await RemoveOwnedFile(); recording = null; throw;
    }
   });
  }
  private async void Capture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs e)
  {
   await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
    await Run(async () => { await StopRecording(); StatusText.Text = Locale.T("录音设备发生错误：") + e.Message + Locale.T("。可尝试分析已保存的录音。"); });
   });
  }
  private async Task StopRecording()
  {
   clock.Stop(); elapsed.Stop();
   try { if (isRecording && capture != null) await capture.StopRecordAsync(); }
   finally { isRecording = false; if (capture != null) capture.Failed -= Capture_Failed; capture?.Dispose(); capture = null; }
  }
  private async void Analyze_Click(object sender, RoutedEventArgs e) { await Run(Analyze); }
  private async Task Analyze()
  {
   if (recording == null) return;
   StopPlayback(); cancellation.Token.ThrowIfCancellationRequested();
   AnalysisProgress.Value = 0; AnalysisProgress.Visibility = Visibility.Visible; StatusText.Text = Locale.T("正在分析完整录音…");
   var token = cancellation.Token;
   var progress = new Progress<double>(value => { if (active && !token.IsCancellationRequested) AnalysisProgress.Value = value * 100; });
   RecordingAnalysis result;
   using (var stream = await recording.OpenStreamForReadAsync())
    result = await Task.Run(() => RecordingAnalysis.ReadWave(stream, token, progress), token);
   token.ThrowIfCancellationRequested(); if (!active) return;
   bool voiced = result.VoicedSeconds > 0;
   AverageBubble.Text = voiced ? result.Average.ToString("F1") + " Hz" : "— Hz";
   FemalePercent.Text = voiced ? result.Percent(2).ToString("F1") + "%" : "—";
   MalePercent.Text = voiced ? result.Percent(0).ToString("F1") + "%" : "—";
   OtherPercent.Text = voiced ? Locale.T("过渡区 ") + result.Percent(1).ToString("F1") + Locale.T("% · 区间外 ") + result.Percent(3).ToString("F1") + "%" : Locale.T("未检测到可靠音高，不能计算区间百分比。");
   PitchValues.Text = voiced ? string.Join("\n", new[] { Hz(result.Average), Hz(result.Median), Hz(result.High), Hz(result.Low) }) : "—\n—\n—\n—";
   VolumeValues.Text = result.Duration > 0 ? string.Join("\n", new[] { result.VolumeAverage.ToString("F1"), result.VolumeMedian.ToString("F1"), result.VolumeHigh.ToString("F1"), result.VolumeLow.ToString("F1") }) : "—\n—\n—\n—";
   CoverageText.Text = Locale.T("录音 ") + result.Duration.ToString("F1") + Locale.T(" 秒 · 有效音高 ") + result.VoicedSeconds.ToString("F1") + Locale.T(" 秒 · 静音/无法识别 ") + Math.Max(0, result.Duration - result.VoicedSeconds).ToString("F1") + Locale.T(" 秒");
   AverageMarker.Visibility = voiced ? Visibility.Visible : Visibility.Collapsed;
   Canvas.SetTop(AverageMarker, 12 + FrequencyScale.Position(result.Average, 50, 350, AppSettings.Scale) * 240);
   ReadingPanel.Visibility = Visibility.Collapsed; ResultsPanel.Visibility = Visibility.Visible;
   StatusText.Text = voiced ? Locale.T("整段文件分析完成 · ") + recording.Name : Locale.T("分析完成，但没有足够的可靠声音。请检查输入设备和录音内容。");
  }
  private static string Hz(double value) { return value.ToString("F1") + " Hz"; }
  private void ConfigureGauge()
  {
   SetGaugeBand(GaugeFemale, 180, 310); SetGaugeBand(GaugeNeutral, 165, 180); SetGaugeBand(GaugeMale, 85, 165);
   if (!AppSettings.ShowGender)
   {
    GaugeFemale.Visibility = GaugeNeutral.Visibility = GaugeMale.Visibility = Visibility.Collapsed;
    FemaleCircle.Fill = MaleCircle.Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 207, 216, 221));
    FemaleLabel.Text = "180–310 Hz"; MaleLabel.Text = "85–165 Hz";
   }
  }
  private void SetGaugeBand(Windows.UI.Xaml.Shapes.Rectangle rectangle, double low, double high)
  {
   double top = FrequencyScale.Position(high, 50, 350, AppSettings.Scale) * 240;
   double bottom = FrequencyScale.Position(low, 50, 350, AppSettings.Scale) * 240;
   Canvas.SetTop(rectangle, 12 + top); rectangle.Height = bottom - top;
  }
  private async void Import_Click(object sender, RoutedEventArgs e)
  {
   await Run(async () => {
    var picker = new FileOpenPicker(); picker.FileTypeFilter.Add(".wav");
    var selected = await picker.PickSingleFileAsync();
    if (selected == null || !active || suspended) return;
    StopPlayback(); await RemoveOwnedFile(); recording = selected; owned = false;
    await Analyze();
   });
  }
  private async void Save_Click(object sender, RoutedEventArgs e)
  {
   await Run(async () => {
    if (recording == null) return;
    var picker = new FileSavePicker { SuggestedFileName = "VoiceTools-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") };
    picker.FileTypeChoices.Add(Locale.T("WAV 录音"), new[] { ".wav" });
    var destination = await picker.PickSaveFileAsync();
    if (destination == null || !active || suspended) return;
    if (destination.Path != recording.Path) await recording.CopyAndReplaceAsync(destination);
    StatusText.Text = Locale.T("录音已保存：") + destination.Name;
   });
  }
  private async void Listen_Click(object sender, RoutedEventArgs e)
  {
   await Run(async () => {
    if (listening) { StopPlayback(); return; }
    if (recording == null) return;
    replay = await recording.OpenReadAsync();
    if (!active || suspended) { StopPlayback(); return; }
    await Player.ConfigureDeviceAsync();
    if (!active || suspended) { StopPlayback(); return; }
    Player.SetSource(replay, "audio/wav"); listening = true; Player.Play(); ListenButton.Content = Locale.T("■ 停止");
   });
  }
  private void StopPlayback() { listening = false; Player.Stop(); Player.Clear(); replay?.Dispose(); replay = null; ListenButton.Content = Locale.T("▶ 回听"); }
  private void Player_Ended(object sender, EventArgs e) { StopPlayback(); }
  private void Player_Failed(object sender, PlaybackError e) { if (!listening) return; StopPlayback(); StatusText.Text = Locale.T("无法回听：") + e.ErrorMessage; }
  private void Back_Click(object sender, RoutedEventArgs e) { if (working) return; StopPlayback(); ResultsPanel.Visibility = Visibility.Collapsed; ReadingPanel.Visibility = Visibility.Visible; }
  private async Task RemoveOwnedFile()
  {
   if (owned && recording != null) { await recording.DeleteAsync(StorageDeleteOption.PermanentDelete); recording = null; }
   owned = false;
  }
  private async Task Cleanup(bool remove)
  {
   await operation.WaitAsync();
   try { StopPlayback(); await StopRecording(); if (remove) await RemoveOwnedFile(); }
   catch (Exception ex) { if (active) StatusText.Text = Locale.T("清理录音时遇到问题：") + Locale.T(ex.Message); }
   finally { UpdateControls(); operation.Release(); }
  }
 }
}




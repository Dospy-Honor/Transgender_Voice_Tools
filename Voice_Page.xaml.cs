using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Input;
using Windows.System;
using VoiceTools.Audio;
using VoiceTools.Settings;

namespace VoiceTools
{
 public sealed partial class Voice_Page : Page
 {
  private VoiceEngine engine = new VoiceEngine();
  private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
  private readonly List<PitchPoint> history = new List<PitchPoint>();
  private readonly List<double> measured = new List<double>();
  private InMemoryRandomAccessStream playback;
  private bool busy, active, playing, suspended;
  private readonly PlaybackTimeline timeline = new PlaybackTimeline(ReplaySeconds());
  private static int ReplaySeconds() { var saved = ApplicationData.Current.LocalSettings.Values["ReplaySeconds"]; int seconds = saved is int ? (int)saved : 10; return seconds == 5 || seconds == 20 || seconds == 30 ? seconds : 10; }
  private string replayIconPath;
  private int sentence;
  private int lifecycle;
  private int toneRequest;
  private Button heldTone;
  private string currentSection = "Pitch";
  private readonly string[] sentences = AppSettings.Sentences;
  private readonly RoutedAudioPlayer Player = new RoutedAudioPlayer();
  public Voice_Page() { InitializeComponent(); Player.MediaEnded += Player_Ended; Player.MediaFailed += Player_Failed; BuildToneKeyboard(); timer.Tick += Tick; SentenceText.Text = sentences.Length > 0 ? sentences[0] : Locale.T("请在设置中添加建议语句"); }
  public void ShowSection(string section)
  {
   if (currentSection != section) StopReferenceTone();
   currentSection = section;
   PitchPanel.Visibility = section == "Pitch" ? Visibility.Visible : Visibility.Collapsed;
   TonePanel.Visibility = section == "Tone" ? Visibility.Visible : Visibility.Collapsed;
   VolumePanel.Visibility = section == "Volume" ? Visibility.Visible : Visibility.Collapsed;
   StatsPanel.Visibility = section == "Stats" ? Visibility.Visible : Visibility.Collapsed;
   ReadingBar.Visibility = section == "Tone" ? Visibility.Collapsed : Visibility.Visible;
  }
  private async void Page_Loaded(object sender, RoutedEventArgs e)
  {
   Locale.Apply(this);
   active = true; Application.Current.Suspending += Suspending; Application.Current.Resuming += Resuming;
   Window.Current.Activated += Window_Activated;
   var saved = ApplicationData.Current.LocalSettings.Values["ReplaySeconds"];
   ReplaySecondsText.Text = timeline.WindowSeconds.ToString();
   Draw(); await StartCapture();
  }
  private void Page_Unloaded(object sender, RoutedEventArgs e) { active = false; Application.Current.Suspending -= Suspending; Application.Current.Resuming -= Resuming; Window.Current.Activated -= Window_Activated; Shutdown(); Player.Dispose(); }
  private void Window_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e) { if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated) StopReferenceTone(); }
  private void Suspending(object sender, SuspendingEventArgs e) { suspended = true; Shutdown(); }
  private async void Resuming(object sender, object e) { await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => { suspended = false; if (active) await StartCapture(); }); }
  private void Shutdown() { lifecycle++; timer.Stop(); StopReferenceTone(); StopPlayback(); engine.Dispose(); history.Clear(); timeline.Reset(); }
  private async void Retry_Click(object sender, RoutedEventArgs e) { await StartCapture(); }
  private async System.Threading.Tasks.Task StartCapture()
  {
   if (busy || !active || suspended || playing || engine.Running) return;
   busy = true; RetryButton.Visibility = Visibility.Collapsed;
   int startingLifecycle = lifecycle;
   try {
    await engine.StartAsync();
    if (!active || startingLifecycle != lifecycle) { engine.Dispose(); return; }
    // Leave a gap in the trace across playback or suspension.
    history.Add(new PitchPoint { Time = engine.CapturedSeconds });
    timeline.UpdateRecording(engine.CapturedSeconds);
    timer.Start(); StatusBar.Visibility = Visibility.Collapsed;
   } catch (Exception ex) { StatusText.Text = Locale.T(ex.Message); StatusBar.Visibility = Visibility.Visible; RetryButton.Visibility = Visibility.Visible; }
   finally { busy = false; if (startingLifecycle != lifecycle && active && !suspended) await StartCapture(); }
  }
  private void Tick(object sender, object e)
  {
   if (playing) { Draw(); return; }
   if (!engine.Running) return;
   double db, time; double pitch = engine.Analyze(out db, out time);
   timeline.UpdateRecording(time);
   history.Add(new PitchPoint { Time = time, Pitch = pitch });
   history.RemoveAll(point => point.Time < time - 30);
   if (pitch > 0) { measured.Add(pitch); if (measured.Count > 6000) measured.RemoveAt(0); }
   PitchText.Text = pitch > 0 ? Math.Round(pitch) + " Hz" : "— Hz";
   TonePitchText.Text = pitch > 0 ? Math.Round(pitch) + "Hz" : "---";
   ToneNoteText.Text = pitch > 0 ? "(" + MusicalTone.NearestNote(pitch) + ")" : "";
   BandText.Text = pitch == 0 ? Locale.T("无可靠音高") : pitch < 85 || pitch > 310 ? Locale.T("参考区间之外") : pitch < 165 ? Locale.T("蓝色参考区") : pitch < 180 ? Locale.T("过渡参考区") : Locale.T("粉色参考区");
   if (!AppSettings.ShowGender) BandText.Text = pitch <= 0 ? Locale.T("无可靠音高") : pitch < 85 || pitch > 310 ? Locale.T("参考区间之外") : pitch < 165 ? "85–165 Hz" : pitch < 180 ? "165–180 Hz" : "180–310 Hz";
   if (measured.Count > 0) { var sorted = measured.OrderBy(v => v).ToArray(); int n = sorted.Length; MedianText.Text = Math.Round((sorted[(n - 1) / 2] + sorted[n / 2]) / 2) + " Hz"; }
   Level.Value = db; LevelText.Text = Locale.T("输入电平 ") + db.ToString("F1") + " dBFS" + (db > -3 ? Locale.T(" · 输入过强") : db < -50 ? Locale.T(" · 输入较弱") : ""); Draw();
  }
  private async void Replay_Click(object sender, RoutedEventArgs e)
  {
   if (busy) return;
   if (playing) {
    StopPlayback();
    await StartCapture();
    Draw(); return;
   }
   await Play(ReplaySeconds());
  }
  private void BuildToneKeyboard()
  {
   for (int row = 0; row < 4; row++) ToneKeyboard.RowDefinitions.Add(new RowDefinition());
   for (int col = 0; col < 6; col++) ToneKeyboard.ColumnDefinitions.Add(new ColumnDefinition());
   for (int row = 0; row < 4; row++) for (int col = 0; col < 6; col++)
   {
    int midi = 60 - row * 6 + col;
    Color color = row == 0 ? (col < 4 ? Color.FromArgb(255, 255, 181, 197) : Color.FromArgb(255, 207, 216, 221))
     : row == 1 ? Color.FromArgb(255, 255, 181, 197)
     : row == 2 && col >= 3 ? Color.FromArgb(255, 204, 204, 204) : Color.FromArgb(255, 168, 214, 229);
    if (!AppSettings.ShowGender) color = Color.FromArgb(255, 207, 216, 221);
    var button = new Button { Content = MusicalTone.Name(midi), Tag = midi, Background = new SolidColorBrush(color),
     Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 144, 255)), BorderBrush = new SolidColorBrush(Color.FromArgb(255, 190, 196, 199)),
     BorderThickness = new Thickness(.5), Padding = new Thickness(0), FontSize = 16,
     HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
    button.Resources["ButtonBackgroundPointerOver"] = button.Background;
    button.Resources["ButtonBackgroundPressed"] = button.Background;
    button.Resources["ButtonForegroundPointerOver"] = button.Foreground;
    button.Resources["ButtonForegroundPressed"] = button.Foreground;
    Windows.UI.Xaml.Automation.AutomationProperties.SetName(button, MusicalTone.Name(midi) + Locale.T("，按住试听 ") + MusicalTone.Frequency(midi).ToString("F1") + " Hz");
    button.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TonePressed), true);
    button.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(ToneReleased), true);
    button.PointerCanceled += ToneReleased;
    button.PointerCaptureLost += ToneReleased;
    button.KeyDown += ToneKeyDown; button.KeyUp += ToneKeyUp;
    button.LostFocus += (s, e) => { if (heldTone == s) StopReferenceTone(); };
    Grid.SetRow(button, row); Grid.SetColumn(button, col); ToneKeyboard.Children.Add(button);
   }
  }
  private async void TonePressed(object sender, PointerRoutedEventArgs e)
  {
   if (!e.GetCurrentPoint((UIElement)sender).Properties.IsLeftButtonPressed) return;
   var button = (Button)sender;
   e.Handled = true; button.Focus(FocusState.Pointer);
   if (heldTone == button) return;
   StopReferenceTone();
   if (!button.CapturePointer(e.Pointer)) return;
   await BeginReferenceTone(button);
  }
  private void ToneReleased(object sender, PointerRoutedEventArgs e) { if (heldTone == sender) StopReferenceTone(); }
  private async void ToneKeyDown(object sender, KeyRoutedEventArgs e)
  {
   if (e.Key != VirtualKey.Space && e.Key != VirtualKey.Enter) return;
   e.Handled = true;
   if (heldTone != sender) { StopReferenceTone(); await BeginReferenceTone((Button)sender); }
  }
  private void ToneKeyUp(object sender, KeyRoutedEventArgs e)
  {
   if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter) { e.Handled = true; if (heldTone == sender) StopReferenceTone(); }
  }
  private async System.Threading.Tasks.Task BeginReferenceTone(Button button)
  {
   if (!active || suspended || currentSection != "Tone" || busy) { button.ReleasePointerCaptures(); return; }
   heldTone = button; int request = ++toneRequest;
   button.BorderThickness = new Thickness(2);
   try
   {
    // A reference tone has its own output node. It never pauses microphone analysis.
    if (playing) StopPlayback();
    await StartCapture();
    if (request != toneRequest || !active || suspended || !engine.Running) return;
    await engine.EnsureToneOutputAsync();
    if (request != toneRequest || !active || suspended || currentSection != "Tone") return;
    engine.PlayTone(MusicalTone.Frequency((int)button.Tag));
   }
   catch (Exception ex) { StopReferenceTone(); StatusText.Text = Locale.T(ex.Message); StatusBar.Visibility = Visibility.Visible; }
  }
  private void StopReferenceTone()
  {
   toneRequest++; engine.StopTone();
   var button = heldTone; heldTone = null;
   if (button != null) { button.BorderThickness = new Thickness(.5); button.ReleasePointerCaptures(); }
  }
  private async System.Threading.Tasks.Task Play(int seconds)
  {
   busy = true; ReplayButton.IsEnabled = false;
   try {
    StopReferenceTone(); engine.Pause(); timer.Stop(); StopPlayback();
    var snapshot = engine.CaptureReplay(seconds);
    if (snapshot.Wave == null) { StatusText.Text = Locale.T("还没有可回听的录音，请稍等片刻。"); StatusBar.Visibility = Visibility.Visible; return; }
    timeline.BeginReplay(snapshot.EndSeconds, snapshot.DurationSeconds);
    var stream = new InMemoryRandomAccessStream(); playback = stream;
    await stream.WriteAsync(snapshot.Wave.AsBuffer());
    if (!active || playback != stream) return;
    await Player.ConfigureDeviceAsync();
    if (!active || playback != stream) return;
    stream.Seek(0); Player.SetSource(stream, "audio/wav"); playing = true;
    Player.Play(); timer.Start(); StatusBar.Visibility = Visibility.Collapsed; Draw();
   } catch (Exception ex) { StopPlayback(); StatusText.Text = Locale.T("播放失败：") + Locale.T(ex.Message); StatusBar.Visibility = Visibility.Visible; }
   finally { busy = false; ReplayButton.IsEnabled = true; if (!playing) { timeline.EndReplay(); await StartCapture(); } }
  }
  private void StopPlayback()
  {
   bool finishedReplay = playing;
   playing = false; timeline.EndReplay();
   Player.Stop(); Player.Clear(); playback?.Dispose(); playback = null;
   // Any exit from replay discards its recording and starts a fresh timeline.
   if (finishedReplay) { engine.Clear(); history.Clear(); timeline.Reset(); Draw(); }
  }
  private async void ResumeRecording_Click(object sender, RoutedEventArgs e) { if (busy) return; StopPlayback(); await StartCapture(); Draw(); }
  private async void Player_Ended(object sender, EventArgs e) { StopPlayback(); await StartCapture(); }
  private async void Player_Failed(object sender, PlaybackError e) { if (!playing) return; StopPlayback(); await StartCapture(); StatusText.Text = Locale.T("无法播放音频：") + e.ErrorMessage; StatusBar.Visibility = Visibility.Visible; }
  private async void Clear_Click(object sender, RoutedEventArgs e) { if (busy) return; StopPlayback(); engine.Clear(); timeline.Reset(); history.Clear(); measured.Clear(); PitchText.Text = MedianText.Text = "— Hz"; BandText.Text = Locale.T("已清除"); Level.Value = -90; LevelText.Text = Locale.T("输入电平 — dBFS"); StatusText.Text = Locale.T("音频和统计已清除。"); Draw(); await StartCapture(); }
  private void Next_Click(object sender, RoutedEventArgs e) { if (sentences.Length == 0) return; sentence = (sentence + 1) % sentences.Length; SentenceText.Text = sentences[sentence]; }
  private void Chart_SizeChanged(object sender, SizeChangedEventArgs e) { Draw(); }
  private double Y(double hz) { return FrequencyScale.Position(hz, 50, 500, AppSettings.Scale) * Chart.ActualHeight; }
  private void Draw()
  {
   if (Chart == null || TransportText == null || ReplayIcon == null) return;
   double width = Chart.ActualWidth; if (width < 60 || Chart.ActualHeight < 1) return;
   Chart.Children.Clear();
   Band(50, 500, Color.FromArgb(255, 207, 216, 221), width);
   if (AppSettings.ShowGender) {
   Band(85, 165, Color.FromArgb(255, 168, 214, 229), width);
   Band(165, 180, Color.FromArgb(255, 204, 204, 204), width);
   Band(180, 310, Color.FromArgb(255, 255, 181, 197), width);
   }
   for (int hz = 50; hz <= 450; hz += 50) {
    var line = new Line { X1 = 0, X2 = width, Y1 = Y(hz), Y2 = Y(hz), Stroke = new SolidColorBrush(Color.FromArgb(70, 70, 90, 110)), StrokeThickness = 1 }; Chart.Children.Add(line);
    var label = new TextBlock { Text = hz + " Hz", FontSize = 10, Foreground = new SolidColorBrush(Colors.Black) }; Canvas.SetTop(label, Math.Max(0, Y(hz) - 15)); Canvas.SetLeft(label, 3); Chart.Children.Add(label);
   }
   if (AppSettings.ShowGender) foreach (var band in new[] { new { Hz = 310.0, Text = Locale.T("女性音高参考区") }, new { Hz = 180.0, Text = Locale.T("中性音高参考区") }, new { Hz = 165.0, Text = Locale.T("男性音高参考区") } }) {
    var label = new TextBlock { Text = band.Text, FontSize = 10, Width = 110, TextAlignment = TextAlignment.Right, Foreground = new SolidColorBrush(Colors.Black) };
    Canvas.SetLeft(label, Math.Max(45, width - 114)); Canvas.SetTop(label, Y(band.Hz)); Chart.Children.Add(label);
   }
   for (int i = 1; i < history.Count; i++) {
    var previous = history[i - 1]; var point = history[i];
    if (previous.Pitch <= 0 || point.Pitch <= 0 || point.Time - previous.Time > .35 || previous.Time < timeline.StartSeconds || point.Time > timeline.EndSeconds) continue;
    Chart.Children.Add(new Line { X1 = (width - 1) * timeline.Fraction(previous.Time), X2 = (width - 1) * timeline.Fraction(point.Time), Y1 = Y(previous.Pitch), Y2 = Y(point.Pitch), Stroke = new SolidColorBrush(Color.FromArgb(255, 238, 48, 63)), StrokeThickness = 1.3 });
   }
   Marker((width - 1) * timeline.StartFraction, Color.FromArgb(255, 90, 102, 108), 1);
   if (playing) Marker((width - 1) * timeline.PlaybackFraction(Player.PositionSeconds), Color.FromArgb(255, 220, 35, 48), 2);
   TransportText.Text = AppSettings.Scale + " · " + (playing ? Locale.T("正在回放") + " " + Math.Min(timeline.DurationSeconds, Player.PositionSeconds).ToString("F1") + " / " + timeline.DurationSeconds.ToString("F1") : Locale.T("正在录音") + " " + timeline.DurationSeconds.ToString("F1")) + " s";
   ResumeRecordingButton.Visibility = playing ? Visibility.Visible : Visibility.Collapsed;
   var iconPath = playing ? "ic_stop_white_24.png" : "ic_play_arrow_white_24.png";
   if (replayIconPath != iconPath) { replayIconPath = iconPath; ReplayIcon.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/" + iconPath)); }
   var action = playing ? Locale.T("停止回放并清空录音") : Locale.T("回听最近录音");
   Windows.UI.Xaml.Automation.AutomationProperties.SetName(ReplayButton, action);
   ToolTipService.SetToolTip(ReplayButton, action);
  }
  private void Marker(double x, Color color, double thickness) { Chart.Children.Add(new Line { X1 = x, X2 = x, Y1 = 0, Y2 = Chart.ActualHeight, Stroke = new SolidColorBrush(color), StrokeThickness = thickness }); }

  private void Band(double low, double high, Color color, double width) { var rectangle = new Rectangle { Width = width, Height = Y(low) - Y(high), Fill = new SolidColorBrush(color) }; Canvas.SetTop(rectangle, Y(high)); Chart.Children.Add(rectangle); }
 }
}





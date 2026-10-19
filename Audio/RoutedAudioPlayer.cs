using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using VoiceTools.Settings;
namespace VoiceTools.Audio
{
 public sealed class PlaybackError : EventArgs { public string ErrorMessage { get; set; } }
 public sealed class RoutedAudioPlayer : IDisposable
 {
  private readonly MediaPlayer player = new MediaPlayer { AutoPlay = false, Volume = .5 };
  private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;
  private MediaSource source;
  private int generation;
  private bool disposed;
  public event EventHandler<EventArgs> MediaEnded;
  public event EventHandler<PlaybackError> MediaFailed;
  public RoutedAudioPlayer()
  {
   player.MediaEnded += async (s, e) => {
    int version = generation;
    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { if (!disposed && version == generation) MediaEnded?.Invoke(this, EventArgs.Empty); });
   };
   player.MediaFailed += async (s, e) => {
    int version = generation;
    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { if (!disposed && version == generation) MediaFailed?.Invoke(this, new PlaybackError { ErrorMessage = e.ErrorMessage }); });
   };
  }
  public async Task ConfigureDeviceAsync()
  {
   var device = await AudioDevices.SelectedAsync(false);
   if (!disposed) player.AudioDevice = device;
  }
  public void SetSource(IRandomAccessStream stream, string mime) { Clear(); source = MediaSource.CreateFromStream(stream, mime); player.Source = source; }
  public void Play() { player.Play(); }
  public void Pause() { if (!disposed) player.Pause(); }
  public double PositionSeconds => disposed ? 0 : player.PlaybackSession.Position.TotalSeconds;
  public void Stop() { generation++; if (!disposed) player.Pause(); }
  public void Clear() { generation++; if (!disposed) player.Source = null; source?.Dispose(); source = null; }
  public void Dispose() { if (disposed) return; Stop(); Clear(); disposed = true; player.Dispose(); }
 }
}

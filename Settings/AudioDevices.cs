using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
namespace VoiceTools.Settings
{
 public static class AudioDevices
 {
  public static async Task<DeviceInformation> SelectedAsync(bool input)
  {
   string id = input ? AppSettings.InputDevice : AppSettings.OutputDevice;
   if (string.IsNullOrEmpty(id)) return null;
   var devices = await DeviceInformation.FindAllAsync(input ? MediaDevice.GetAudioCaptureSelector() : MediaDevice.GetAudioRenderSelector());
   var selected = devices.FirstOrDefault(d => d.Id == id && d.IsEnabled);
   if (selected == null) throw new InvalidOperationException(Locale.T("所选设备不可用，请在设置中重新选择：") + (input ? AppSettings.InputName : AppSettings.OutputName));
   return selected;
  }
 }
}

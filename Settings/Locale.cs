using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Automation;
namespace VoiceTools.Settings
{
 public static class Locale
 {
  // Exact UI strings only: practice passages and user text are never translated by this helper.
  private static readonly Dictionary<string, string> English = new Dictionary<string, string>();
  private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>();
  static Locale()
  {
   foreach (var line in Translations.Split('\n')) {
    var clean = line.TrimEnd('\r'); int divider = clean.IndexOf('|'); if (divider < 0) continue;
    string zh = clean.Substring(0, divider).Replace("\\n", "\n"), en = clean.Substring(divider + 1).Replace("\\n", "\n");
    English[zh] = en; if (!Chinese.ContainsKey(en)) Chinese[en] = zh;
   }
  }
  public static string T(string value)
  {
   if (string.IsNullOrEmpty(value)) return value;
   string translated;
   return (AppSettings.Language == "en-US" ? English : Chinese).TryGetValue(value, out translated) ? translated : value;
  }
  public static void Apply(DependencyObject root)
  {
   Apply(root, new HashSet<DependencyObject>());
  }
  private static void Apply(DependencyObject root, HashSet<DependencyObject> visited)
  {
   if (root == null || !visited.Add(root)) return;
   if (root is TextBlock text) text.Text = T(text.Text);
   if (root is ContentControl content && content.Content is string label) content.Content = T(label);
   else if (root is ContentControl container && container.Content is DependencyObject nested) Apply(nested, visited);
   if (root is ComboBox combo && combo.Header is string comboHeader) combo.Header = T(comboHeader);
   if (root is TextBox box) { if (box.Header is string header) box.Header = T(header); box.PlaceholderText = T(box.PlaceholderText); return; }
   if (root is ToggleSwitch toggle) {
    if (toggle.Header is string header) toggle.Header = T(header);
    if (toggle.OnContent is string on) toggle.OnContent = T(on);
    if (toggle.OffContent is string off) toggle.OffContent = T(off);
   }
   string name = AutomationProperties.GetName(root); if (!string.IsNullOrEmpty(name)) AutomationProperties.SetName(root, T(name));
   if (root is FrameworkElement element && ToolTipService.GetToolTip(element) is string tip) ToolTipService.SetToolTip(element, T(tip));
   if (root is ItemsControl items) foreach (var item in items.Items) if (item is DependencyObject child) Apply(child, visited);
   for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) Apply(VisualTreeHelper.GetChild(root, i), visited);
  }
  private const string Translations = @"返回设置|Back to Settings
编辑完成后点击保存，未保存的修改不会生效。|Select Save when finished. Unsaved changes will not take effect.
保存|Save
取消|Cancel
建议语句库|Suggested sentences
仅显示和编辑当前语言的语句库，修改后自动保存。|View and edit sentences for the current language only. Changes are saved automatically.
语句库为空，请添加语句。|Your library is empty. Add a sentence.
新增语句|Add sentence
修改语句|Edit sentence
删除语句|Delete sentence
语句内容|Sentence
保存语句|Save sentence
语句不能为空。|Sentences cannot be empty.
请在设置中添加建议语句|Add suggested sentences in Settings
停止回放并清空录音|Stop playback and clear recording
已暂停|Paused
正在回放|Playing
正在录音|Recording
继续录音|Resume recording
暂停回放|Pause playback
继续回放|Resume playback

设置|Settings
语言|Language
性别（色块）标记|Gender (color) markers
显示|Show
隐藏|Hide
比例尺|Frequency scale
回听时长|Replay duration
5 秒|5 seconds
10 秒|10 seconds
20 秒|20 seconds
30 秒|30 seconds
分析待读文本|Analysis reading passage
保存文本|Save passage
恢复默认文本|Restore default passage
接收设备（麦克风）|Input device (microphone)
播放设备|Output device
刷新设备|Refresh devices
练习语句和默认长文只使用所选语言；自定义文本按语言分别保存。|Practice sentences and default passages use only the selected language. Custom passages are saved separately for each language.
隐藏后使用中性色与数值区间，不改变统计结果。|Use neutral colors and numeric ranges when hidden. Analysis results stay the same.
只改变频率坐标的间距，读数和区间阈值仍以 Hz 表示。|Only changes frequency spacing. Readings and range boundaries remain in Hz.
设备选择应用于实时分析、音调试听、长文录音和所有回听。设备断开时会提示重新选择。|Device choices apply to live analysis, reference tones, passage recording and all playback. Disconnected devices require reselection.
设置自动保存；长文编辑后请点击保存文本。|Settings save automatically. Click Save passage after editing your text.
文本尚未保存。|Passage has not been saved.
文本已保存。|Passage saved.
请填写朗读文本。|Please enter a reading passage.
系统默认|System default
（设备不可用）| (device unavailable)
无法读取设备：|Unable to list devices: 
所选设备不可用，请在设置中重新选择：|The selected device is unavailable. Choose another in Settings: 
分析|Analysis
音调|Tone
音量|Volume
统计|Statistics
展开或收起菜单|Expand or collapse navigation
按住下方音符试听，同时观察自己的音调|Press and hold a note below while watching your live pitch
建议使用耳机，避免麦克风拾取参考音|Use headphones to keep reference tones out of the microphone
输入音量|Input volume
输入电平 — dBFS|Input level — dBFS
这是麦克风数字输入电平，不是环境声压级。|This is the digital microphone level, not environmental sound pressure.
当前音高|Current pitch
等待声音|Waiting for voice
最近有效音高中位数|Recent voiced pitch median
清除录音和统计|Clear recording and statistics
正在连接麦克风…|Connecting to microphone…
重试|Retry
点击切换练习句|Click for the next practice sentence
回听最近录音|Replay recent recording
无可靠音高|No reliable pitch
参考区间之外|Outside reference ranges
蓝色参考区|Blue reference range
过渡参考区|Transition range
粉色参考区|Pink reference range
输入电平 |Input level 
 · 输入过强| · Input too loud
 · 输入较弱| · Input too quiet
正在回听最近 |Replaying up to the last 
 秒内的可用录音| seconds of available audio
，按住试听 |, hold to play 
还没有可回听的录音，请稍等片刻。|No recording is available yet. Please wait a moment.
播放失败：|Playback failed: 
无法播放音频：|Cannot play audio: 
已清除|Cleared
音频和统计已清除。|Audio and statistics cleared.
女性音高参考区|Female pitch range
男性音高参考区|Male pitch range
中性音高参考区|Neutral pitch range
录音并朗读以下长文：|Record and read the following passage aloud:
可直接编辑或粘贴自己的文章。录完后点击停止，分析整段录音。|Edit or paste your own passage. Stop recording to analyze the entire file.
朗读长文|Reading passage
打开 WAV 文件|Open WAV file
分析已有录音|Analyze existing recording
录制全文|Record passage
停止并分析|Stop and analyze
‹ 返回|‹ Back
结果|Results
▶ 回听|▶ Replay
■ 停止|■ Stop
女性音高区|Female pitch
男性音高区|Male pitch
过渡区 — · 区间外 —|Transition — · Outside ranges —
音高|Pitch
平均值\n中位数\n高位（95%分位）\n低位（5%分位）|Average\nMedian\nHigh (95th percentile)\nLow (5th percentile)
平均电平\n中位数\n高位（95%分位）\n低位（5%分位）|Average level\nMedian\nHigh (95th percentile)\nLow (5th percentile)
音量（dBFS）|Volume (dBFS)
百分比按有效音高时长计算；静音与无法识别部分不参与区间占比。范围固定，与实时音高页一致。|Percentages use reliable voiced duration only. Silence and unrecognized audio are excluded. Fixed ranges match the live pitch page.
这是音高分布，不是整体声音的性别判断。|This is a pitch distribution, not a gender judgment of the overall voice.
保存录音 WAV|Save recording as WAV
全文录音会暂存为本地 WAV；离开分析页后删除临时录音，可在结果页另存。|Passage recordings are temporary local WAV files, removed when you leave Analysis. Save a copy from Results to keep one.
录音已中断并保留，恢复后可分析已有录音。|Recording was interrupted and retained. Analyze the existing recording after resuming.
无法完成操作：|Unable to complete operation: 
正在录制全文 · 点击红色按钮结束并分析|Recording passage · Click the red button to stop and analyze
录音设备发生错误：|Recording device error: 
。可尝试分析已保存的录音。|. You can try analyzing the saved recording.
正在分析完整录音…|Analyzing the complete recording…
过渡区 |Transition 
% · 区间外 |% · Outside ranges 
未检测到可靠音高，不能计算区间百分比。|No reliable pitch detected. Range percentages are unavailable.
录音 |Recording 
 秒 · 有效音高 | s · Reliable pitch 
 秒 · 静音/无法识别 | s · Silence/unrecognized 
 秒| s
整段文件分析完成 · |Complete file analyzed · 
分析完成，但没有足够的可靠声音。请检查输入设备和录音内容。|Analysis complete, but no reliable voice was detected. Check the input device and recording.
WAV 录音|WAV recording
录音已保存：|Recording saved: 
无法回听：|Cannot replay: 
清理录音时遇到问题：|Recording cleanup failed: 
无法初始化音频设备：|Cannot initialize audio device: 
无法访问麦克风，请检查 Windows 麦克风权限和输入设备：|Cannot access microphone. Check Windows microphone permissions and input device: 
无法播放参考音：|Cannot play reference tone: 
请选择标准 PCM 16 位 WAV 录音。|Please select a standard 16-bit PCM WAV recording.
文件不是 WAV 音频。|The file is not WAV audio.
录音文件不完整。|The recording file is incomplete.
WAV 格式信息不完整。|The WAV format header is incomplete.
支持 8–96 kHz、单声道或双声道的 PCM 16 位 WAV 文件。|Supported: 8–96 kHz mono or stereo 16-bit PCM WAV files.
录音数据不完整。|The recording data is incomplete.
";
 }
}

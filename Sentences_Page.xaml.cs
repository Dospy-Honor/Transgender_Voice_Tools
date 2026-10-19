using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation;
using VoiceTools.Settings;

namespace VoiceTools
{
 public sealed partial class Sentences_Page : Page
 {
  private readonly string language = AppSettings.Language;
  public Sentences_Page()
  {
   InitializeComponent();
   LanguageText.Text = language == "en-US" ? "English" : "简体中文";
   foreach (var sentence in AppSettings.Sentences) AddRow(sentence);
   StatusText.Text = "";
  }
  private void Page_Loaded(object sender, RoutedEventArgs e) { Locale.Apply(this); }
  private TextBox AddRow(string text)
  {
   var row = new Grid { ColumnSpacing = 8 };
   row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
   row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   var number = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
   var input = new TextBox { Text = text, AcceptsReturn = false, TextWrapping = TextWrapping.NoWrap, HorizontalAlignment = HorizontalAlignment.Stretch };
   ScrollViewer.SetHorizontalScrollMode(input, ScrollMode.Enabled);
   ScrollViewer.SetHorizontalScrollBarVisibility(input, ScrollBarVisibility.Hidden);
   input.TextChanged += (s, e) => StatusText.Text = Locale.T("文本尚未保存。");
   var delete = new Button { Content = new SymbolIcon(Symbol.Delete), VerticalAlignment = VerticalAlignment.Center };
   ToolTipService.SetToolTip(delete, Locale.T("删除语句"));
   delete.Click += (s, e) => { Rows.Children.Remove(row); Renumber(); StatusText.Text = Locale.T("文本尚未保存。"); };
   Grid.SetColumn(input, 1); Grid.SetColumn(delete, 2);
   row.Children.Add(number); row.Children.Add(input); row.Children.Add(delete);
   Rows.Children.Add(row); Renumber(); return input;
  }
  private void Renumber()
  {
   for (int i = 0; i < Rows.Children.Count; i++) {
    var row = (Grid)Rows.Children[i];
    ((TextBlock)row.Children[0]).Text = (i + 1).ToString();
    AutomationProperties.SetName(row.Children[1], Locale.T("语句内容") + " " + (i + 1));
    AutomationProperties.SetName(row.Children[2], Locale.T("删除语句") + " " + (i + 1));
   }
  }
  private void Add_Click(object sender, RoutedEventArgs e)
  {
   var input = AddRow("");
   StatusText.Text = Locale.T("文本尚未保存。");
   input.Loaded += (s, args) => { input.Focus(FocusState.Programmatic); input.StartBringIntoView(); };
  }
  private void Save_Click(object sender, RoutedEventArgs e)
  {
   try {
    if (AppSettings.Language != language) { Frame.Navigate(typeof(Settings_Page)); return; }
    var inputs = Rows.Children.Cast<Grid>().Select(row => (TextBox)row.Children[1]).ToArray();
    var empty = inputs.FirstOrDefault(input => string.IsNullOrWhiteSpace(input.Text));
    if (empty != null) { StatusText.Text = Locale.T("语句不能为空。"); empty.Focus(FocusState.Programmatic); empty.StartBringIntoView(); return; }
    AppSettings.SaveSentences(inputs.Select(input => input.Text).ToArray());
    StatusText.Text = Locale.T("文本已保存。");
   } catch (Exception ex) { StatusText.Text = ex.Message; }
  }
  private void Back_Click(object sender, RoutedEventArgs e)
  {
   if (Frame.CanGoBack) Frame.GoBack(); else Frame.Navigate(typeof(Settings_Page));
  }
 }
}

using System;
namespace VoiceTools.Settings
{
 public static class FrequencyScale
 {
  public static double Transform(double hz, string scale)
  {
   hz = Math.Max(0, hz);
   switch (scale) {
    case "ERB": return 21.4 * Math.Log10(1 + .00437 * hz);
    case "Mel": return 2595 * Math.Log10(1 + hz / 700);
    default: return hz;
   }
  }
  public static double Position(double hz, double low, double high, string scale)
  {
   double min = Transform(low, scale), max = Transform(high, scale);
   return (max - Transform(Math.Max(low, Math.Min(high, hz)), scale)) / (max - min);
  }
 }
 public static class PracticeTexts
 {
  public static string[] Sentences(string language)
  {
   return language == "en-US" ? new[] {
    "The morning sunlight falls softly across the room.", "We walked beside the river and listened to the birds.",
    "Would you like a cup of tea before we begin?", "Tomorrow is another chance to try something new."
   } : new[] {
    "清晨的阳光穿过窗户，轻轻落在桌上的书页间。", "沿着河边慢慢走，听风吹过树叶的声音。",
    "今天我想和朋友分享一个有趣的小故事。", "雨停以后，远处的天空出现了一道彩虹。"
   };
  }
  public const string LegacyChinesePassage = "清晨，我推开窗户，看见阳光落在对面的屋顶上。街道还很安静，只有几辆自行车慢慢经过。楼下的店主正在整理门前的花盆，一只小猫蹲在台阶旁，好奇地望着来往的人。\n\n我带上水杯和一本书，沿着熟悉的小路走向公园。昨夜刚下过雨，树叶上还留着细小的水珠。微风吹来，枝叶轻轻摇动，阳光也跟着变换位置。远处有人在慢跑，有人坐在长椅上聊天，还有一个孩子正在认真地观察地上的蚂蚁。\n\n走到湖边时，我停下脚步。水面映着天空的颜色，偶尔有一阵风吹过，倒影就变成了一圈圈细碎的波纹。我想起朋友曾经问过我的一个问题：如果有一个完全不用赶时间的下午，你最想做什么？当时我没有立刻回答，现在却觉得答案很简单。也许只是找一个舒服的地方，安静地读几页书，再和喜欢的人分享今天看到的事情。\n\n中午，我来到一家小店，点了一碗热汤。老板笑着问我味道怎么样，我说，温度刚刚好，闻起来也很香。窗外忽然传来一阵笑声，几位路人正为一件小事讨论得很热闹。我听不清他们的全部对话，却能感受到那份轻松。\n\n回家的路上，天空渐渐亮了起来。我决定晚上给朋友打个电话，告诉他今天的经历。有时候，一天里值得记住的事情并不特别，只要慢一点，就能发现许多原本被忽略的细节。";
  public static string DefaultPassage(string language)
  {
   return language == "en-US"
    ? "One morning, I opened the window and watched the sunlight move across the roofs. The street was quiet. A shopkeeper was arranging flowers outside the door, and a small cat was watching people pass.\n\nI took a book and a bottle of water and walked to the park. Rain had left tiny drops on the leaves. A gentle breeze moved the branches, and the light changed with them. Some people were running, others were talking on a bench, and a child was carefully watching an ant cross the path.\n\nAt the lake, I stopped for a while. The water reflected the sky until the wind broke the reflection into small waves. I remembered a question from a friend: if you had an afternoon with nothing to hurry for, what would you do? I had not known what to say then. Now the answer seemed simple. I would find a comfortable place, read a few pages, and share the little things I had noticed with someone I liked.\n\nAt noon, I ordered a bowl of soup in a small cafe. The owner asked how it tasted. I told her it was warm and comforting. Outside, a group of people began to laugh. I could not hear their conversation, but their voices made the room feel brighter.\n\nOn the way home, I decided to call my friend that evening. A day does not need a great adventure to be worth remembering. Sometimes we only need to slow down and notice what is already around us."
    : "从前，一位大法官非常仰慕皇后的美貌，皇后那性感的嘴唇令他神魂颠倒，他做梦都想亲一亲那嘴唇。但他只是止于妄想，因为他知道，如果胆敢冒犯皇后，必定会被国王送上断头台。\n\n这天，大法官忍不住把他的秘密告诉了御医。御医笑了笑，神秘地说道：“我可以帮你实现这个愿望，如果你……”大法官马上答应，如果他能得偿所愿，他会付给御医一万金币。\n\n御医秘密配制了一种令人发痒的药水，并买通了皇后的贴身侍女。这天晚上，那名贴身侍女趁皇后洗澡时把那种令人发痒的药水涂在了皇后的口红上。皇后准备去参加一个宴会，她洗完澡后就开始化妆。当她的嘴唇碰到口红时，马上就感到奇痒无比。\n\n国王急忙召来御医。御医装模作样给皇后检查一番后，说：“陛下，皇后得了一种怪病，要想治好这种病……”说到这里，御医假装面露难色。\n\n“快说。只要能免除皇后的痛苦，用什么方法朕都答应。”国王催促道。\n\n御医犹豫了一会儿，说道：“陛下，要想给皇后止痒，只有让一个人来亲皇后的嘴唇。也就是说，这个人的唾液就是止痒的良药。”\n\n国王听了御医的话，皱起了眉头。但皇后痛苦的呻吟声与哀求的眼神令他不能再犹豫了。他叹了一口气，说道：“说吧，这个人是谁？”\n\n“大法官。”御医忍住笑答道。\n\n大法官马上被召进皇宫。事先，御医已经把解药给了大法官。大法官把药水含在嘴里，遵照国王的旨意，开始执行他那做梦都想的美差。果然，大法官的嘴唇印在皇后的嘴唇上后，皇后马上就感到不痒了。\n\n次日，御医就来大法官家索要报酬。“你开玩笑吧，我怎么会答应你这种事？”大法官笑道。他肯定御医不敢把他们之间的秘密说出去，就耍赖了。御医却没有生气。他摇摇头就离开了。\n\n不久之后的某个晚上，国王洗完澡穿上内裤，他的屁股感到奇痒难耐，仿佛有上万只蚂蚁在上面爬。于是，大法官立即被召进了皇宫……\n\n（摘自《喜剧世界》第7期 庞启帆/编译）";
  }
 }
}

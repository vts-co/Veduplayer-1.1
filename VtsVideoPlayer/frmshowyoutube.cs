using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VtsVideoPlayer
{
    public partial class frmshowyoutube : Form
    {
        public frmshowyoutube()
        {
            InitializeComponent();
        }
        private Timer recordingCheckTimer = new Timer();
        private string content;
        private string bucket;
        private string foldername;
        private int servertype;
        private double NumOfMinutes;
        private string Execution;
        private string Student;
        private bool IsVisitor;
        private string lessonId;
        private string baseUrl;
         private Timer watchTimer;
        private int watchedSeconds = 0;
        private bool lessonMarked = false;
        private bool isPlaying = false;
        public frmshowyoutube(string content,string Execution, string Student, string NumOfMinutes,bool isVisitorBool,string lessonId,string baseUrl)
        {
            this.content = content;

            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.NumOfMinutes = Convert.ToDouble(NumOfMinutes);
            this.Student = Student;
            this.Execution = Execution;
            this.IsVisitor = isVisitorBool;
            this.lessonId = lessonId;
            this.baseUrl = baseUrl;
            InitializeStaticImage();
            StartRecordingCheckTimer();
            SetupUI();
            this.Shown += Form1_Shown;
        }

        private PictureBox staticImageBox;

        // كود منع Alt+Tab وغيره
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private void BlockKeys()
        {
            _hookID = SetHook(_proc);
        }
        private Button hiddenExitButton;
        private void StartClipboardCleaner()
        {
            Timer timer = new Timer();
            timer.Interval = 500; // كل ثانية
            timer.Tick += (s, e) =>
            {
                if (Clipboard.ContainsImage())
                {
                    Clipboard.Clear(); // يمسح الصورة من الحافظة
                }
            };
            timer.Start();
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        // القيم المتاحة لـ Display Affinity
        private const uint WDA_NONE = 0;
        private const uint WDA_MONITOR = 1;
        private Dictionary<string, string> videoQualities = new Dictionary<string, string>();

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // تفعيل الحماية
            bool success = SetWindowDisplayAffinity(this.Handle, WDA_MONITOR);
            if (!success)
            {
                //  MessageBox.Show("فشل في تفعيل حماية الشاشة.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                //  MessageBox.Show("تم تفعيل حماية الشاشة بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void CheckForRecordingApps(object sender, EventArgs e)
        {
            string[] blockedApps = { "obs", "bandicam", "camstudio", "camtasia", "snagit", "screenrec" };

            foreach (var proc in Process.GetProcesses())
            {
                foreach (var name in blockedApps)
                {
                    if (proc.ProcessName.ToLower().Contains(name))
                    {
                        // إما الخروج أو إظهار شاشة سوداء
                        MessageBox.Show("لا يمكن تشغيل البرنامج أثناء تسجيل الشاشة.", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Application.Exit();
                    }
                }
            }
        }
        //#region Tool Bar
        private Panel controlPanel;
        private Button btnPlay, btnPause, btnForward, btnBackward;
        private TrackBar volumeTrackBar, timelineTrackBar;
        private ComboBox speedComboBox;
        private Timer timelineTimer;
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern bool BlockInput(bool fBlockIt);



        private void SetupUI()
        {
            this.Text = "Vedu Player";
            this.Size = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = true;



        }

        private WebView2 webView;
        private async void btnPlay_Click(object sender, EventArgs e)
        {
            await webView.ExecuteScriptAsync("playVideo();");
        }

        private async void btnPause_Click(object sender, EventArgs e)
        {
            await webView.ExecuteScriptAsync("pauseVideo();");
        }

        private async void btnSeek_Click(object sender, EventArgs e)
        {
            int seconds = 60; // مثال: انتقال إلى الدقيقة 1
            await webView.ExecuteScriptAsync($"seekTo({seconds});");
        }

        private async Task InitializeWebViewAsync(string iframeContent)
        {
            if (webView != null)
            {
                this.Controls.Remove(webView);
                webView.Dispose();
            }

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            var srcStart = iframeContent.IndexOf("src=\"") + 5;
            var srcEnd = iframeContent.IndexOf("\"", srcStart);
            var src = iframeContent.Substring(srcStart, srcEnd - srcStart);

            var embedPrefix = "https://www.youtube.com/embed/";
            var videoPart = src.StartsWith(embedPrefix) ? src.Substring(embedPrefix.Length) : "";
            var questionMarkIndex = videoPart.IndexOf('?');
            string videoId = questionMarkIndex > 0 ? videoPart.Substring(0, questionMarkIndex) : videoPart;
            MessageBox.Show(videoId);

            string url = $"https://www.youtube.com/embed/{videoId}";

            try
            {
                var env = await CoreWebView2Environment.CreateAsync();
                await webView.EnsureCoreWebView2Async(env);
                MessageBox.Show(url);
                if (webView.CoreWebView2 != null)
                {
                    webView.Source = new Uri(url);
                }
                else
                {
                    MessageBox.Show("لم يتم تحميل WebView2 بنجاح.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل الفيديو: " + ex.Message);
            }
        }
        private static string ExtractYouTubeId(string url)
        {
            try
            {
                if (url.Contains("watch?v="))
                {
                    var uri = new Uri(url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    return query["v"];
                }
                else if (url.Contains("youtu.be/"))
                {
                    return url.Substring(url.LastIndexOf("/") + 1);
                }
                else if (url.Contains("embed/"))
                {
                    var idx = url.LastIndexOf("embed/");
                    return url.Substring(idx + "embed/".Length).Split('?')[0];
                }
                else if (url.Contains("/v/"))
                {
                    var idx = url.LastIndexOf("/v/");
                    return url.Substring(idx + "/v/".Length).Split('?')[0];
                }
                else if (url.Length == 11) // المعرف نفسه
                {
                    return url;
                }
            }
            catch { }
            return "";
        }
        #region MyRegion
        //        private async void Form1_Shown(object sender, EventArgs e)
        //        {
        //            string finalUrl = content;
        //            string videoId = ExtractYouTubeId(finalUrl);

        //            if (string.IsNullOrEmpty(videoId))
        //            {
        //                MessageBox.Show("❌ لم أستطع استخراج videoId من اللينك: " + finalUrl);
        //                return;
        //            }

        //            webView = new WebView2
        //            {
        //                Dock = DockStyle.Fill
        //            };
        //            this.Controls.Add(webView);
        //            await webView.EnsureCoreWebView2Async();

        //            string html = $@"
        //<html>
        //  <head>
        //    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
        //    <script src='https://www.youtube.com/iframe_api'></script>
        //    <script>
        //      var player;
        //      function onYouTubeIframeAPIReady() {{
        //        player = new YT.Player('ytplayer', {{
        //          height: '100%',
        //          width: '100%',
        //          videoId: '{videoId}',
        //          playerVars: {{
        //            'autoplay': 1,
        //            'controls': 0,
        //            'modestbranding': 1,
        //            'rel': 0,
        //            'showinfo': 0
        //          }},
        //          events: {{
        //            'onReady': function(event) {{ event.target.playVideo(); }}
        //          }}
        //        }});
        //      }}
        //    </script>
        //    <style>
        //      html, body {{ margin:0; height:100%; overflow:hidden; background-color:#000; }}
        //      #ytplayer {{ width:100%; height:100%; }}
        //    </style>
        //  </head>
        //  <body>
        //    <div id='ytplayer'></div>
        //  </body>
        //</html>";

        //            webView.CoreWebView2.NavigateToString(html);
        //        } 
        #endregion

        #region Form1_Shown 
        //private async void Form1_Shown(object sender, EventArgs e)
        //{
        #region MyRegion


        //string iframeContent = content;
        //var srcStart = iframeContent.IndexOf("src=\"") + 5;
        //var srcEnd = iframeContent.IndexOf("\"", srcStart);
        //var src = iframeContent.Substring(srcStart, srcEnd - srcStart);

        //var embedPrefix = "https://www.youtube.com/embed/";
        //var videoPart = src.StartsWith(embedPrefix) ? src.Substring(embedPrefix.Length) : "";
        //var questionMarkIndex = videoPart.IndexOf('?');
        ////string videoId = questionMarkIndex > 0 ? videoPart.Substring(0, questionMarkIndex) : videoPart;
        //string videoId = content;

        //string finalUrl = content;

        //// ✅ استخرج videoId من لينك يوتيوب
        //string videoId = "";
        //if (finalUrl.Contains("watch?v="))
        //{
        //    var uri = new Uri(finalUrl);
        //    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        //    videoId = query["v"];  // هنا هيجيب jZLHSyjBrxE
        //}


        //else if (finalUrl.Contains("youtu.be/"))
        //{
        //    videoId = finalUrl.Substring(finalUrl.LastIndexOf("/") + 1);
        //}

        //// لو مش لاقي videoId
        //if (string.IsNullOrEmpty(videoId))
        //{
        //    MessageBox.Show("❌ لم أستطع استخراج videoId من اللينك: " + finalUrl);
        //    return;
        //} 
        #endregion


        //            string finalUrl = content;
        //            string videoId = ExtractYouTubeId(finalUrl);

        //            if (string.IsNullOrEmpty(videoId))
        //            {
        //                MessageBox.Show("❌ لم أستطع استخراج videoId من اللينك: " + finalUrl);
        //                return;
        //            }

        //            webView = new WebView2();
        //            webView.Dock = DockStyle.Fill;
        //            this.Controls.Add(webView);

        //            await webView.EnsureCoreWebView2Async();
        //            string html = $@"
        //<html>
        //  <head>
        //    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
        //    <script src='https://www.youtube.com/iframe_api'></script>
        //    <script>
        //      var player;
        //      var volumeTimeout;

        //      function onYouTubeIframeAPIReady() {{
        //        player = new YT.Player('ytplayer', {{
        //          height: '100%',
        //          width: '100%',
        //          videoId: '{videoId}',
        //          playerVars: {{
        //            'autoplay': 1,
        //            'controls': 0,
        //            'modestbranding': 1,
        //            'rel': 0,
        //            'showinfo': 0,
        //            'disablekb': 1, // تعطيل لوحة المفاتيح
        //            'fs': 0 // تعطيل وضع ملء الشاشة
        //          }},
        //          events: {{
        //            'onReady': onPlayerReady,
        //            'onStateChange': onPlayerStateChange
        //          }}
        //        }});
        //      }}

        //      function onPlayerReady(event) {{
        //        updateTime();
        //        player.setVolume(100);
        //        // تعطيل التفاعل مع الفيديو
        //        disableVideoInteractions();
        //      }}

        //      function onPlayerStateChange(event) {{
        //        // منع أي تغييرات غير مرغوب فيها
        //      }}

        //      function disableVideoInteractions() {{
        //        // تعطيل جميع التفاعلات مع الفيديو
        //        var iframe = document.querySelector('#ytplayer iframe');
        //        if (iframe) {{
        //          iframe.style.pointerEvents = 'none';
        //          iframe.setAttribute('allow', 'autoplay'); // السماح فقط بالأوتوبلاي
        //        }}
        //      }}

        //      function playVideo() {{
        //        if (player && player.playVideo) {{
        //          player.playVideo();
        //        }}
        //      }}

        //      function pauseVideo() {{
        //        if (player && player.pauseVideo) {{
        //          player.pauseVideo();
        //        }}
        //      }}

        //      function seekTo(seconds) {{
        //        if (player && player.seekTo) {{
        //          player.seekTo(parseInt(seconds), true);
        //        }}
        //      }}

        //      function updateTime() {{
        //        setInterval(() => {{
        //          if (player && player.getCurrentTime && player.getDuration) {{
        //            let current = player.getCurrentTime();
        //            let duration = player.getDuration();
        //            document.getElementById('progress').max = duration;
        //            document.getElementById('progress').value = current;
        //            document.getElementById('timeLabel').innerText = formatTime(current) + ' / ' + formatTime(duration);
        //          }}
        //        }}, 1000);
        //      }}

        //      function formatTime(time) {{
        //        let minutes = Math.floor(time / 60);
        //        let seconds = Math.floor(time % 60);
        //        return minutes + ':' + (seconds < 10 ? '0' + seconds : seconds);
        //      }}

        //      function toggleVolumeBar() {{
        //        var bar = document.getElementById('volumeBar');
        //        if (bar.style.display === 'flex') {{
        //          bar.style.display = 'none';
        //        }} else {{
        //          bar.style.display = 'flex';
        //          clearTimeout(volumeTimeout);
        //          volumeTimeout = setTimeout(() => {{
        //            bar.style.display = 'none';
        //          }}, 3000);
        //        }}
        //      }}

        //      function changeVolume(val) {{
        //        if (player && player.setVolume) {{
        //          player.setVolume(val);
        //        }}
        //      }}
        //    </script>
        //    <style>
        //      html, body {{
        //        margin: 0;
        //        padding: 0;
        //        height: 100%;
        //        overflow: hidden;
        //        direction: rtl;
        //        font-family: 'Tahoma', sans-serif;
        //        background-color: #000;
        //        user-select: none; /* منع التحديد */
        //      }}
        //      #playerContainer {{
        //        position: relative;
        //        width: 100%;
        //        height: 85%;
        //      }}
        //      #ytplayer iframe {{
        //        pointer-events: none !important; /* تعطيل جميع الضغطات على الفيديو */
        //      }}
        //      #blockLayer {{
        //        position: absolute;
        //        top: 0;
        //        left: 0;
        //        width: 100%;
        //        height: 100%;
        //        background-color: transparent;
        //        z-index: 100;
        //        pointer-events: auto; /* السماح للطبقة بمنع الضغطات */
        //        cursor: not-allowed; /* مؤشر ممنوع */
        //      }}
        //      #controls {{
        //        height: 15%;
        //        background-color: #1c1c1c;
        //        color: white;
        //        display: flex;
        //        flex-direction: column;
        //        justify-content: center;
        //        padding: 5px 20px;
        //        box-sizing: border-box;
        //        position: relative;
        //        z-index: 200; /* التأكد أن الأزرار فوق كل شيء */
        //      }}
        //      .control-row {{
        //        display: flex;
        //        align-items: center;
        //        justify-content: center;
        //        margin: 4px 0;
        //        position: relative;
        //      }}
        //      .progress-row {{
        //        display: flex;
        //        align-items: center;
        //        justify-content: space-between;
        //        width: 100%;
        //      }}
        //      button {{
        //        padding: 8px 14px;
        //        background-color: #333;
        //        border: none;
        //        color: white;
        //        cursor: pointer;
        //        border-radius: 5px;
        //        font-size: 16px;
        //        margin: 0 5px;
        //        transition: background-color 0.3s;
        //        z-index: 300;
        //        position: relative;
        //      }}
        //      button:hover {{
        //        background-color: #555;
        //      }}
        //      #timeLabel {{
        //        min-width: 100px;
        //        text-align: center;
        //        font-size: 13px;
        //      }}
        //      input[type='range'] {{
        //        -webkit-appearance: none;
        //        width: 100%;
        //        height: 5px;
        //        background: #555;
        //        border-radius: 5px;
        //        outline: none;
        //        cursor: pointer;
        //        z-index: 300;
        //        position: relative;
        //      }}
        //      input[type='range']::-webkit-slider-thumb {{
        //        -webkit-appearance: none;
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        cursor: pointer;
        //        border: none;
        //      }}
        //      input[type='range']::-moz-range-thumb {{
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        border: none;
        //        cursor: pointer;
        //      }}
        //      #volumeIcon {{
        //        font-size: 20px;
        //        cursor: pointer;
        //        margin-right: 10px;
        //        position: relative;
        //        z-index: 300;
        //      }}
        //      #volumeBar {{
        //        display: none;
        //        position: absolute;
        //        bottom: 35px;
        //        right: 0;
        //        height: 100px;
        //        width: 35px;
        //        background-color: #1c1c1c;
        //        padding: 5px;
        //        border-radius: 5px;
        //        box-shadow: 0 2px 5px rgba(0,0,0,0.4);
        //        z-index: 400;
        //        display: flex;
        //        align-items: center;
        //        justify-content: center;
        //      }}
        //      #volumeBar input[type='range'] {{
        //        writing-mode: bt-lr;
        //        -webkit-appearance: slider-vertical;
        //        width: 5px;
        //        height: 100px;
        //        background: #555;
        //        border-radius: 5px;
        //        outline: none;
        //      }}
        //    </style>
        //  </head>
        //  <body>
        //    <div id='playerContainer'>
        //      <div id='ytplayer'></div>
        //      <div id='blockLayer' onclick='event.stopPropagation();'></div>
        //    </div>
        //    <div id='controls'>
        //      <div class='control-row'>
        //        <button onclick='playVideo(); event.stopPropagation();'>▶ تشغيل</button>
        //        <button onclick='pauseVideo(); event.stopPropagation();'>⏸ إيقاف مؤقت</button>
        //      </div>
        //      <div class='control-row progress-row'>
        //        <input type='range' id='progress' min='0' max='100' value='0' oninput='seekTo(this.value); event.stopPropagation();' />
        //        <div id='volumeIcon' onclick='toggleVolumeBar(); event.stopPropagation();'>🔊
        //          <div id='volumeBar' onclick='event.stopPropagation();'>
        //            <input type='range' min='0' max='100' value='100' oninput='changeVolume(this.value); event.stopPropagation();' />
        //          </div>
        //        </div>
        //      </div>
        //      <div class='control-row'>
        //        <div id='timeLabel'>0:00 / 0:00</div>
        //      </div>
        //    </div>
        //  </body>
        //</html>";
        //            webView.CoreWebView2.NavigateToString(html);

        #region work
        //string finalUrl = content;
        //string videoId = ExtractYouTubeId(finalUrl);

        //if (string.IsNullOrEmpty(videoId))
        //{
        //    MessageBox.Show("❌ لم أستطع استخراج videoId من اللينك: " + finalUrl);
        //    return;
        //}
        //webView = new WebView2();
        //webView.Dock = DockStyle.Fill;
        //this.Controls.Add(webView);

        //await webView.EnsureCoreWebView2Async();
        //string html = $@"
        //<html>
        //  <head>
        //    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
        //    <script src='https://www.youtube.com/iframe_api'></script>
        //    <script>
        //      var player;
        //      var volumeTimeout;

        //      function onYouTubeIframeAPIReady() {{
        //        player = new YT.Player('ytplayer', {{
        //          height: '100%',
        //          width: '100%',
        //          videoId: '{videoId}',
        //          playerVars: {{
        //            'autoplay': 1,
        //            'controls': 0,
        //            'modestbranding': 1,
        //            'rel': 0,
        //            'showinfo': 0
        //          }},
        //          events: {{
        //            'onReady': onPlayerReady
        //          }}
        //        }});
        //      }}

        //      function onPlayerReady(event) {{
        //        updateTime();
        //        player.setVolume(100);
        //      }}

        //      function playVideo() {{
        //        player.playVideo();
        //      }}

        //      function pauseVideo() {{
        //        player.pauseVideo();
        //      }}

        //      function seekTo(seconds) {{
        //        player.seekTo(parseInt(seconds), true);
        //      }}

        //      function updateTime() {{
        //        setInterval(() => {{
        //          if (player && player.getCurrentTime && player.getDuration) {{
        //            let current = player.getCurrentTime();
        //            let duration = player.getDuration();
        //            document.getElementById('progress').max = duration;
        //            document.getElementById('progress').value = current;
        //            document.getElementById('timeLabel').innerText = formatTime(current) + ' / ' + formatTime(duration);
        //          }}
        //        }}, 1000);
        //      }}

        //      function formatTime(time) {{
        //        let minutes = Math.floor(time / 60);
        //        let seconds = Math.floor(time % 60);
        //        return minutes + ':' + (seconds < 10 ? '0' + seconds : seconds);
        //      }}

        //      function toggleVolumeBar() {{
        //        var bar = document.getElementById('volumeBar');
        //        bar.style.display = 'flex';
        //        clearTimeout(volumeTimeout);
        //        volumeTimeout = setTimeout(() => {{
        //          bar.style.display = 'none';
        //        }}, 3000);
        //      }}

        //      function changeVolume(val) {{
        //        if (player && player.setVolume) {{
        //          player.setVolume(val);
        //          toggleVolumeBar();
        //        }}
        //      }}

        //      function changeQuality(level) {{
        //        if (player && player.setPlaybackQuality) {{
        //          player.setPlaybackQuality(level);
        //        }}
        //      }}
        //    </script>
        //    <style>
        //      html, body {{
        //        margin: 0;
        //        padding: 0;
        //        height: 100%;
        //        overflow: hidden;
        //        direction: rtl;
        //        font-family: 'Tahoma', sans-serif;
        //        background-color: #000;
        //      }}
        //      #playerContainer {{
        //        position: relative;
        //        width: 100%;
        //        height: 85%;
        //      }}
        //      #ytplayer iframe {{
        //        pointer-events: none;
        //      }}
        //      #blockLayer {{
        //        position: absolute;
        //        top: 0;
        //        left: 0;
        //        width: 100%;
        //        height: 100%;
        //        background-color: transparent;
        //        z-index: 10;
        //      }}
        //      #controls {{
        //        height: 15%;
        //        background-color: #1c1c1c;
        //        color: white;
        //        display: flex;
        //        flex-direction: column;
        //        justify-content: center;
        //        padding: 5px 20px;
        //        box-sizing: border-box;
        //        position: relative;
        //      }}
        //      .control-row {{
        //        display: flex;
        //        align-items: center;
        //        justify-content: center;
        //        margin: 4px 0;
        //        position: relative;
        //      }}
        //      .progress-row {{
        //        display: flex;
        //        align-items: center;
        //        justify-content: space-between;
        //      }}
        //      button {{
        //        padding: 8px 14px;
        //        background-color: #333;
        //        border: none;
        //        color: white;
        //        cursor: pointer;
        //        border-radius: 5px;
        //        font-size: 16px;
        //        margin: 0 5px;
        //        transition: background-color 0.3s;
        //      }}
        //      button:hover {{
        //        background-color: #555;
        //      }}
        //      select {{
        //        padding: 6px 10px;
        //        background-color: #333;
        //        color: white;
        //        border: none;
        //        border-radius: 5px;
        //        font-size: 14px;
        //        margin: 0 5px;
        //        cursor: pointer;
        //      }}
        //      select:hover {{
        //        background-color: #555;
        //      }}
        //      #timeLabel {{
        //        min-width: 100px;
        //        text-align: center;
        //        font-size: 13px;
        //      }}
        //      input[type='range'] {{
        //        -webkit-appearance: none;
        //        width: 100%;
        //        height: 5px;
        //        background: #555;
        //        border-radius: 5px;
        //        outline: none;
        //      }}
        //      input[type='range']::-webkit-slider-thumb {{
        //        -webkit-appearance: none;
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        cursor: pointer;
        //        border: none;
        //      }}
        //      input[type='range']::-moz-range-thumb {{
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        border: none;
        //        cursor: pointer;
        //      }}
        //      #volumeIcon {{
        //        font-size: 20px;
        //        cursor: pointer;
        //        margin-right: 10px;
        //        position: relative;
        //      }}
        //      #volumeBar {{
        //        display: none;
        //        position: absolute;
        //        bottom: 35px;
        //        right: 0;
        //        height: 100px;
        //        width: 35px;
        //        background-color: #1c1c1c;
        //        padding: 5px;
        //        border-radius: 5px;
        //        box-shadow: 0 2px 5px rgba(0,0,0,0.4);
        //        z-index: 999;
        //        display: flex;
        //        align-items: center;
        //        justify-content: center;
        //      }}
        //      #volumeBar input[type='range'] {{
        //        writing-mode: bt-lr;
        //        -webkit-appearance: slider-vertical;
        //        width: 5px;
        //        height: 100px;
        //        background: #555;
        //        border-radius: 5px;
        //        outline: none;
        //      }}
        //      #volumeBar input[type='range']::-webkit-slider-thumb {{
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        cursor: pointer;
        //        border: none;
        //      }}
        //      #volumeBar input[type='range']::-moz-range-thumb {{
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        border: none;
        //        cursor: pointer;
        //      }}
        //    </style>
        //  </head>
        //  <body>
        //    <div id='playerContainer'>
        //      <div id='ytplayer'></div>
        //      <div id='blockLayer'></div>
        //    </div>
        //    <div id='controls'>
        //      <div class='control-row'>
        //        <button onclick='playVideo()'>▶ تشغيل</button>
        //        <button onclick='pauseVideo()'>⏸ إيقاف مؤقت</button>

        //      </div>
        //      <div class='control-row progress-row'>
        //        <input type='range' id='progress' min='0' max='100' value='0' oninput='seekTo(this.value)' />
        //        <div id='volumeIcon' onclick='toggleVolumeBar()'>🔊
        //          <div id='volumeBar'>
        //            <input type='range' min='0' max='100' value='100' oninput='changeVolume(this.value)' />
        //          </div>
        //        </div>
        //      </div>
        //      <div class='control-row'>
        //        <div id='timeLabel'>0:00 / 0:00</div>
        //      </div>
        //    </div>
        //  </body>
        //</html>";
        //webView.CoreWebView2.NavigateToString(html);

        #endregion


        #region MyRegion
        //    string html = $@"
        //<html>
        //  <head>
        //    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
        //    <script src='https://www.youtube.com/iframe_api'></script>
        //    <script>
        //      var player;
        //      var volumeTimeout;

        //      function onYouTubeIframeAPIReady() {{
        //        player = new YT.Player('ytplayer', {{
        //          height: '100%',
        //          width: '100%',
        //          videoId: '{videoId}',
        //          playerVars: {{
        //            'autoplay': 1,
        //            'controls': 0,
        //            'modestbranding': 1,
        //            'rel': 0,
        //            'showinfo': 0
        //          }},
        //          events: {{
        //            'onReady': onPlayerReady
        //          }}
        //        }});
        //      }}

        //      function onPlayerReady(event) {{
        //        updateTime();
        //        player.setVolume(100);
        //      }}

        //      function playVideo() {{
        //        player.playVideo();
        //      }}

        //      function pauseVideo() {{
        //        player.pauseVideo();
        //      }}

        //      function seekTo(seconds) {{
        //        player.seekTo(parseInt(seconds), true);
        //      }}

        //      function updateTime() {{
        //        setInterval(() => {{
        //          if (player && player.getCurrentTime && player.getDuration) {{
        //            let current = player.getCurrentTime();
        //            let duration = player.getDuration();
        //            document.getElementById('progress').max = duration;
        //            document.getElementById('progress').value = current;
        //            document.getElementById('timeLabel').innerText = formatTime(current) + ' / ' + formatTime(duration);
        //          }}
        //        }}, 1000);
        //      }}

        //      function formatTime(time) {{
        //        let minutes = Math.floor(time / 60);
        //        let seconds = Math.floor(time % 60);
        //        return minutes + ':' + (seconds < 10 ? '0' + seconds : seconds);
        //      }}

        //      function toggleVolumeBar() {{
        //        var bar = document.getElementById('volumeBar');
        //        bar.style.display = 'flex';
        //        clearTimeout(volumeTimeout);
        //        volumeTimeout = setTimeout(() => {{
        //          bar.style.display = 'none';
        //        }}, 3000);
        //      }}

        //      function changeVolume(val) {{
        //        if (player && player.setVolume) {{
        //          player.setVolume(val);
        //          toggleVolumeBar(); // إعادة ضبط المؤقت
        //        }}
        //      }}
        //    </script>
        //    <style>
        //      html, body {{
        //        margin: 0;
        //        padding: 0;
        //        height: 100%;
        //        overflow: hidden;
        //        direction: rtl;
        //        font-family: 'Tahoma', sans-serif;
        //        background-color: #000;
        //      }}
        //      #playerContainer {{
        //        position: relative;
        //        width: 100%;
        //        height: 85%;
        //      }}
        //      iframe {{
        //        pointer-events: none;
        //      }}
        //      #blockLayer {{
        //        position: absolute;
        //        top: 0;
        //        left: 0;
        //        width: 100%;
        //        height: 100%;
        //        background-color: transparent;
        //        z-index: 10;
        //      }}
        //      #controls {{
        //        height: 15%;
        //        background-color: #1c1c1c;
        //        color: white;
        //        display: flex;
        //        flex-direction: column;
        //        justify-content: center;
        //        padding: 5px 20px;
        //        box-sizing: border-box;
        //        position: relative;
        //      }}
        //      .control-row {{
        //        display: flex;
        //        align-items: center;
        //        justify-content: center;
        //        margin: 4px 0;
        //        position: relative;
        //      }}
        //      .progress-row {{
        //        display: flex;
        //        align-items: center;
        //        justify-content: space-between;
        //      }}
        //      button {{
        //        padding: 8px 14px;
        //        background-color: #333;
        //        border: none;
        //        color: white;
        //        cursor: pointer;
        //        border-radius: 5px;
        //        font-size: 16px;
        //        margin: 0 5px;
        //        transition: background-color 0.3s;
        //      }}
        //      button:hover {{
        //        background-color: #555;
        //      }}
        //      #timeLabel {{
        //        min-width: 100px;
        //        text-align: center;
        //        font-size: 13px;
        //      }}
        //      input[type='range'] {{
        //        -webkit-appearance: none;
        //        width: 100%;
        //        height: 5px;
        //        background: #555;
        //        border-radius: 5px;
        //        outline: none;
        //      }}
        //      input[type='range']::-webkit-slider-thumb {{
        //        -webkit-appearance: none;
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        cursor: pointer;
        //        border: none;
        //      }}
        //      input[type='range']::-moz-range-thumb {{
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        border: none;
        //        cursor: pointer;
        //      }}
        //      #volumeIcon {{
        //        font-size: 20px;
        //        cursor: pointer;
        //        margin-right: 10px;
        //        position: relative;
        //      }}
        //      #volumeBar {{
        //        display: none;
        //        position: absolute;
        //        bottom: 35px;
        //        right: 0;
        //        height: 100px;
        //        width: 35px;
        //        background-color: #1c1c1c;
        //        padding: 5px;
        //        border-radius: 5px;
        //        box-shadow: 0 2px 5px rgba(0,0,0,0.4);
        //        z-index: 999;
        //        display: flex;
        //        align-items: center;
        //        justify-content: center;
        //      }}
        //      #volumeBar input[type='range'] {{
        //        writing-mode: bt-lr;
        //        -webkit-appearance: slider-vertical;
        //        width: 5px;
        //        height: 100px;
        //        background: #555;
        //        border-radius: 5px;
        //        outline: none;
        //      }}
        //      #volumeBar input[type='range']::-webkit-slider-thumb {{
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        cursor: pointer;
        //        border: none;
        //      }}
        //      #volumeBar input[type='range']::-moz-range-thumb {{
        //        width: 12px;
        //        height: 12px;
        //        background: #ff6600;
        //        border-radius: 50%;
        //        border: none;
        //        cursor: pointer;
        //      }}
        //    </style>
        //  </head>
        //  <body>
        //    <div id='playerContainer'>
        //      <div id='ytplayer'></div>
        //      <div id='blockLayer'></div>
        //    </div>
        //    <div id='controls'>
        //      <div class='control-row'>
        //        <button onclick='playVideo()'>▶ تشغيل</button>
        //        <button onclick='pauseVideo()'>⏸ إيقاف مؤقت</button>
        //      </div>
        //      <div class='control-row progress-row'>
        //        <input type='range' id='progress' min='0' max='100' value='0' oninput='seekTo(this.value)' />
        //        <div id='volumeIcon' onclick='toggleVolumeBar()'>🔊
        //          <div id='volumeBar'>
        //            <input type='range' min='0' max='100' value='100' oninput='changeVolume(this.value)' />
        //          </div>
        //        </div>
        //      </div>
        //      <div class='control-row'>
        //        <div id='timeLabel'>0:00 / 0:00</div>
        //      </div>
        //    </div>
        //  </body>
        //</html>";

        #endregion


        //  }
        // إضافة هذا الكود أيضاً







        #endregion


        #region Form1_Shown  WorkNow
        private async void Form1_Shown(object sender, EventArgs e)
        {
            string finalUrl = content;
            string videoId = ExtractYouTubeId(finalUrl);

            if (string.IsNullOrEmpty(videoId))
            {
                MessageBox.Show("❌ لم أستطع استخراج videoId من اللينك: " + finalUrl);
                return;
            }

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            //            string html = $@"
            //<!doctype html>
            //<html>
            //  <head>
            //    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
            //    <script src='https://www.youtube.com/iframe_api'></script>
            //    <script>
            //      var player;
            //      var volumeTimeout;

            //      window.onYouTubeIframeAPIReady = function() {{
            //        player = new YT.Player('ytplayer', {{
            //          height: '100%',
            //          width: '100%',
            //          videoId: '{videoId}',
            //          playerVars: {{
            //            'autoplay': 1,
            //            'controls': 0,
            //            'modestbranding': 1,
            //            'rel': 0,
            //            'showinfo': 0,
            //            'disablekb': 1,
            //            'fs': 0
            //          }},
            //          events: {{
            //            'onReady': onPlayerReady,
            //            'onStateChange': onPlayerStateChange
            //          }}
            //        }});
            //      }};

            //      window.onPlayerReady = function(event) {{
            //        updateTime();
            //        if (player && player.setVolume) player.setVolume(100);
            //        disableVideoInteractions();
            //      }};

            //      window.onPlayerStateChange = function(event) {{
            //        // event.data values: -1 (unstarted), 0 (ended), 1 (playing), 2 (paused), 3 (buffering), 5 (video cued)
            //        try {{
            //          if (window.chrome && window.chrome.webview) {{
            //            if (event.data == YT.PlayerState.PLAYING) {{
            //              window.chrome.webview.postMessage('playing');
            //            }} else if (event.data == YT.PlayerState.PAUSED) {{
            //              window.chrome.webview.postMessage('paused');
            //            }} else if (event.data == YT.PlayerState.ENDED) {{
            //              window.chrome.webview.postMessage('ended');
            //            }} else {{
            //              window.chrome.webview.postMessage('state:' + event.data);
            //            }}
            //          }}
            //        }} catch (e) {{
            //          // ignore
            //        }}
            //      }};

            //      function disableVideoInteractions() {{
            //        var iframe = document.querySelector('#ytplayer iframe');
            //        if (iframe) {{
            //          iframe.style.pointerEvents = 'none';
            //          iframe.setAttribute('allow', 'autoplay'); 
            //        }}
            //      }}

            //      window.playVideo = function() {{
            //        if (player && player.playVideo) player.playVideo();
            //      }};

            //      window.pauseVideo = function() {{
            //        if (player && player.pauseVideo) player.pauseVideo();
            //      }};

            //      window.seekTo = function(seconds) {{
            //        if (player && player.seekTo) {{
            //          var s = parseInt(seconds) || 0;
            //          player.seekTo(s, true);
            //        }}
            //      }};

            //      window.updateTime = function() {{
            //        setInterval(function() {{
            //          if (player && player.getCurrentTime && player.getDuration) {{
            //            var current = player.getCurrentTime();
            //            var duration = player.getDuration();
            //            var prog = document.getElementById('progress');
            //            if (prog) {{
            //              prog.max = duration;
            //              prog.value = current;
            //            }}
            //            var lbl = document.getElementById('timeLabel');
            //            if (lbl) {{
            //              lbl.innerText = formatTime(current) + ' / ' + formatTime(duration);
            //            }}
            //          }}
            //        }}, 1000);
            //      }};

            //      function formatTime(time) {{
            //        var minutes = Math.floor(time / 60);
            //        var seconds = Math.floor(time % 60);
            //        return minutes + ':' + (seconds < 10 ? '0' + seconds : seconds);
            //      }}

            //      function toggleVolumeBar() {{
            //        var bar = document.getElementById('volumeBar');
            //        if (!bar) return;
            //        if (bar.style.display === 'flex') {{
            //          bar.style.display = 'none';
            //        }} else {{
            //          bar.style.display = 'flex';
            //          clearTimeout(volumeTimeout);
            //          volumeTimeout = setTimeout(function() {{
            //            bar.style.display = 'none';
            //          }}, 3000);
            //        }}
            //      }}

            //      function changeVolume(val) {{
            //        if (player && player.setVolume) {{
            //          player.setVolume(parseInt(val) || 0);
            //        }}
            //      }}
            //    </script>

            //    <style>
            //      html, body {{
            //        margin: 0; padding: 0; height: 100%; overflow: hidden; direction: rtl; font-family: Tahoma, sans-serif; background: #000; user-select: none;
            //      }}
            //      #playerContainer {{ position: relative; width: 100%; height: 85%; }}
            //      #ytplayer {{ width: 100%; height: 100%; }}
            //      #ytplayer iframe {{ pointer-events: none !important; }}
            //      #blockLayer {{ position: absolute; top:0; left:0; width:100%; height:100%; background:transparent; z-index:100; cursor:not-allowed; pointer-events:auto; }}
            //      #controls {{ height:15%; background:#1c1c1c; color:#fff; display:flex; flex-direction:column; justify-content:center; padding:6px 14px; box-sizing:border-box; z-index:200; }}
            //      .control-row {{ display:flex; align-items:center; justify-content:center; margin:4px 0; }}
            //      .progress-row {{ display:flex; align-items:center; justify-content:space-between; width:100%; }}
            //      button {{ padding:8px 14px; background:#333; border:none; color:#fff; border-radius:5px; margin:0 5px; cursor:pointer; }}
            //      #timeLabel {{ min-width:100px; text-align:center; font-size:13px; }}
            //      input[type='range'] {{ width:100%; height:6px; -webkit-appearance:none; background:#555; border-radius:6px; outline:none; }}
            //      input[type='range']::-webkit-slider-thumb {{ -webkit-appearance:none; width:14px; height:14px; background:#ff6600; border-radius:50%; }}
            //      #volumeIcon {{ font-size:20px; cursor:pointer; margin-right:10px; position:relative; }}
            //      #volumeBar {{ display:none; position:absolute; bottom:40px; right:0; height:110px; width:36px; background:#1c1c1c; padding:6px; border-radius:6px; box-shadow:0 2px 6px rgba(0,0,0,0.5); z-index:400; align-items:center; justify-content:center; }}
            //      #volumeBar input[type='range'] {{ writing-mode:bt-lr; -webkit-appearance:slider-vertical; width:6px; height:100px; }}
            //    </style>
            //  </head>
            //  <body>
            //    <div id='playerContainer'>
            //      <div id='ytplayer'></div>
            //      <div id='blockLayer' onclick='event.stopPropagation();'></div>
            //    </div>

            //    <div id='controls'>
            //      <div class='control-row'>
            //        <button onclick='playVideo(); event.stopPropagation();'>▶ تشغيل</button>
            //        <button onclick='pauseVideo(); event.stopPropagation();'>⏸ إيقاف مؤقت</button>
            //      </div>

            //      <div class='control-row progress-row'>
            //        <input type='range' id='progress' min='0' max='100' value='0' oninput='seekTo(this.value); event.stopPropagation();' />
            //        <div id='volumeIcon' onclick='toggleVolumeBar(); event.stopPropagation();'>🔊
            //          <div id='volumeBar' onclick='event.stopPropagation();'>
            //            <input type='range' min='0' max='100' value='100' oninput='changeVolume(this.value); event.stopPropagation();' />
            //          </div>
            //        </div>
            //      </div>

            //      <div class='control-row'>
            //        <div id='timeLabel'>0:00 / 0:00</div>
            //      </div>
            //    </div>

            //  </body>
            //</html>
            //";
            // مثال: مسار الملف داخل نفس مجلد التطبيق
           // string htmlFilePath = Path.Combine("https://vtsitco.com/player.html");
            string url = new Uri("https://vtsitco.com/player.html") + "?id=" + Uri.EscapeDataString(videoId);

            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Navigate(url);

            //webView.CoreWebView2.NavigateToString(html);

            StartWatchTimer();
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(msg)) return;

                // اعمل تحديث للمتغير isPlaying ضمن UI thread
                if (this.InvokeRequired)
                {
                    this.BeginInvoke((Action)(() => HandleWebMessage(msg)));
                }
                else
                {
                    HandleWebMessage(msg);
                }
            }
            catch { }
        }

        #region MyRegion
        private void HandleWebMessage(string msg)
        {
            if (msg == "playing")
            {
                isPlaying = true;
            }
            else if (msg == "paused" || msg == "ended")
            {
                isPlaying = false;
            }
        }

        private void StartWatchTimer()
        {
            int requiredSeconds = (NumOfMinutes <= 0) ? 0 : (int)Math.Round(NumOfMinutes);

            if (requiredSeconds == 0 && !lessonMarked)
            {
                lessonMarked = true;
                _ = MarkLessonAsWatchedWithRetry();
                return;
            }

            watchTimer = new Timer();
            watchTimer.Interval = 1000;
            watchTimer.Tick += async (s, e) =>
            {
                try
                {
                    if (isPlaying)
                    {
                        watchedSeconds++;

                        if (!lessonMarked && watchedSeconds >= requiredSeconds)
                        {
                            lessonMarked = true;
                            bool ok = await MarkLessonAsWatchedWithRetry();
                            if (ok)
                            {
                                watchTimer?.Stop();
                            }
                            else
                            {
                                lessonMarked = false;
                            }
                        }
                    }
                }
                catch { }
            };
            watchTimer.Start();
        }

        private async Task<bool> MarkLessonAsWatchedWithRetry(int maxRetries = 5)
        {
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var url = $"{baseUrl}/LessonURL/MarkTheLessonAsWatched";

                        var body = new
                        {
                            ExecutionId = Execution,
                            LessonId = lessonId,
                            UserId = Student,
                            IsVisitor = IsVisitor
                        };

                        string json = JsonConvert.SerializeObject(body);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadAsStringAsync();

                            if (result.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                            else
                            {
                                await Task.Delay(2000);
                            }
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            MessageBox.Show($"⚠️ خطأ في الاتصال: {response.StatusCode}\nالرد: {error}");
                            await Task.Delay(2000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ خطأ أثناء الإرسال: " + ex.Message);
                    await Task.Delay(2000);
                }
            }

            MessageBox.Show("⛔ فشل تسجيل المشاهدة بعد عدة محاولات.");
            return false;
        }

        private string ExtractYouTubeIdFromAny(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var srcMatch = Regex.Match(input, "src\\s*=\\s*['\"](?<src>[^'\"]+)['\"]", RegexOptions.IgnoreCase);
            string url = input;
            if (srcMatch.Success) url = srcMatch.Groups["src"].Value;


            var m = Regex.Match(url, @"embed/([^?&""]+)");
            if (m.Success) return m.Groups[1].Value;

            m = Regex.Match(url, @"[?&]v=([^?&""]+)");
            if (m.Success) return m.Groups[1].Value;

            m = Regex.Match(url, @"youtu\.be/([^?&""]+)");
            if (m.Success) return m.Groups[1].Value;

            m = Regex.Match(url, @"^[a-zA-Z0-9_-]{6,}$");
            if (m.Success) return m.Value;

            return null;
        }
        #endregion 
        #endregion
        private void StartRecordingCheckTimer()
        {
            recordingCheckTimer.Interval = 3000; // كل 3 ثواني
            recordingCheckTimer.Tick += CheckForRecordingApps;
            recordingCheckTimer.Start();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F12) // F12 مفتاح سري
            {
                Application.Exit(); // أو استخدم: hiddenExitButton.PerformClick();
                return true;
            }
            if (keyData == Keys.PrintScreen)
            {
                // ShowStaticImage();
                ReplaceClipboardWithFakeImage();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void InitializeStaticImage()
        {
            staticImageBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                //  Image = Properties.Resources.Screenshot__5_, // تأكد من إضافة الصورة إلى Resources
                SizeMode = PictureBoxSizeMode.StretchImage,
                Visible = false
            };

            this.Controls.Add(staticImageBox);
        }

        private void ReplaceClipboardWithFakeImage()
        {
            // إنشاء صورة سوداء بحجم صغير
            Bitmap fakeImage = new Bitmap(10, 10);
            using (Graphics g = Graphics.FromImage(fakeImage))
            {
                g.Clear(Color.Black);
            }

            // نسخ الصورة المزيفة إلى Clipboard
            Clipboard.SetImage(fakeImage);
            //  Clipboard.SetImage(Properties.Resources.Screenshot__5_);

        }
        // إظهار الصورة الثابتة
        private void ShowStaticImage()
        {
            staticImageBox.Visible = true;
        }

        // إخفاء الصورة عند عودة الشاشة الطبيعية
        private void HideStaticImage()
        {
            staticImageBox.Visible = false;
        }
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // منع Alt+Tab, Ctrl+Esc, Windows Key
                if ((vkCode == 0x09 && (GetAsyncKeyState(0x12) & 0x8000) != 0) || // Alt+Tab
                    (vkCode == 0x1B && (GetAsyncKeyState(0x11) & 0x8000) != 0) || // Ctrl+Esc
                    (vkCode == 0x5B) || (vkCode == 0x5C))                         // Windows Key
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        // استيراد الدوال المطلوبة من Windows API
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        private void frmshowyoutube_Load(object sender, EventArgs e)
        {
            BlockKeys();
            StartClipboardCleaner();

            string[] blockedApps = { "obs", "bandicam", "camstudio", "camtasia", "snagit", "screenrec" };
            foreach (var proc in Process.GetProcesses())
            {
                foreach (var name in blockedApps)
                {
                    if (proc.ProcessName.ToLower().Contains(name))
                    {
                        MessageBox.Show("لا يمكن تشغيل البرنامج أثناء تسجيل الشاشة.", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Application.Exit();
                    }
                }
            }
        }
    }
}

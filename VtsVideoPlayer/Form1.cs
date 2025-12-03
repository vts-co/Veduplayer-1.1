using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AxWMPLib;
using TLMS.ObjectStorageS3;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace VtsVideoPlayer
{
    public partial class frmvideoplayer : Form
    {
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
        public frmvideoplayer()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            InitializeStaticImage();
            StartRecordingCheckTimer();
            SetupUI();

            this.Shown += Form1_Shown;
        }

        public frmvideoplayer(string content, string bucket, string Execution, string Student,
            string NumOfMinutes, bool IsVisitor,string lessonId,string baseUrl, string foldername = null, int servertype = 0)
        {
            this.content = content;
            this.bucket = bucket;
            this.foldername = foldername;
            this.servertype = servertype;
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.NumOfMinutes = Convert.ToDouble(NumOfMinutes);
            this.Student = Student;
            this.Execution = Execution;
            this.IsVisitor = IsVisitor;
            this.lessonId = lessonId;
            this.baseUrl = baseUrl;
            InitializeStaticImage();
            StartRecordingCheckTimer();
            SetupUI();

            this.Shown += Form1_Shown;

           


        }

        public frmvideoplayer(string content, int servertype = 0)
        {
            this.content = content;

            this.servertype = servertype;
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
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


        private AxWMPLib.AxWindowsMediaPlayer axWindowsMediaPlayer1;
        private Timer recorderCheckTimer;

        //private void SetupUI()
        //{
        //    this.Text = "Vedu Player";
        //    this.Size = new Size(900, 600);
        //    this.FormBorderStyle = FormBorderStyle.FixedDialog;
        //    this.MaximizeBox = true;
        //    recorderCheckTimer = new Timer();
        //    recorderCheckTimer.Interval = 100;
        //    recorderCheckTimer.Tick += Timer_Tick;
        //    recorderCheckTimer.Start();

        //    axWindowsMediaPlayer1 = new AxWMPLib.AxWindowsMediaPlayer();
        //    axWindowsMediaPlayer1.BeginInit();
        //    axWindowsMediaPlayer1.Dock = DockStyle.Fill;
        //    axWindowsMediaPlayer1.Enabled = true;
        //    this.Controls.Add(axWindowsMediaPlayer1);
        //    axWindowsMediaPlayer1.EndInit();

        //}

        #region MyRegion

        private void InitializeStaticImage()
        {
            staticImageBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = Properties.Resources.black, // ضع صورة هنا في Resources
                SizeMode = PictureBoxSizeMode.StretchImage,
                Visible = false
            };

            this.Controls.Add(staticImageBox);
            staticImageBox.BringToFront(); // تأكد أنها فوق الفيديو
        }

        private void SetupUI()
        {
            this.Text = "Vedu Player";
            this.Size = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = true;

            axWindowsMediaPlayer1 = new AxWMPLib.AxWindowsMediaPlayer();
            axWindowsMediaPlayer1.BeginInit();
            axWindowsMediaPlayer1.Dock = DockStyle.Fill;
            axWindowsMediaPlayer1.Enabled = true;
            this.Controls.Add(axWindowsMediaPlayer1);
            axWindowsMediaPlayer1.EndInit();

            InitializeStaticImage();

            axWindowsMediaPlayer1.PlayStateChange += AxWindowsMediaPlayer1_PlayStateChange;
        }
        private async void AxWindowsMediaPlayer1_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            await Task.Delay(100);

            if (axWindowsMediaPlayer1.fullScreen && IsScreenRecorderRunning())
            {
                staticImageBox.Visible = true;
                staticImageBox.BringToFront();
            }
            else
            {
                staticImageBox.Visible = false;
            }
        }
        private bool IsScreenRecorderRunning()
        {
            string[] knownRecorders = { "obs", "bandicam", "camtasia", "flashback", "action", "screencast", "CupCut" };

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string name = process.ProcessName.ToLower();
                    if (knownRecorders.Any(r => name.Contains(r)))
                        return true;
                }
                catch { continue; }
            }

            return false;
        }

        //private void AxWindowsMediaPlayer1_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        //{
        //    // إذا دخل وضع ملء الشاشة، أخفِ الفيديو وأظهر الصورة
        //    if (axWindowsMediaPlayer1.fullScreen)
        //    {
        //        axWindowsMediaPlayer1.fullScreen = false;
        //        axWindowsMediaPlayer1.Visible = false;
        //        staticImageBox.Visible = true;
        //    }
        //}

        #endregion
        private WebView2 webView;


        private async void Form1_Shown(object sender, EventArgs e)
        {
            // this.Show();

            // S3Service s3Service = new S3Service(bucket, foldername, servertype);
            //var client = s3Service.GetClient();
            // //  string videoUrl = @"C:\Users\VTS Developer 1\Documents\Bandicam\اضافة مستخدم.mp4";
            // var videoUrl = s3Service.GeneratePresignedURLR(content, 2, client);
            //// string videoUrl = @"C:\Users\VTS Developer 1\Documents\Bandicam\اضافة مستخدم.mp4";
            // //string videoUrl = @"C:\Users\VTS Developer 1\Documents\Bandicam\123.mp4";
            // axWindowsMediaPlayer1.URL = videoUrl;
            // axWindowsMediaPlayer1.Ctlcontrols.play();

            try
            {
                //S3Service s3Service = new S3Service(bucket, foldername, servertype);
                //var client = s3Service.GetClient();

                //var videoUrl = s3Service.GeneratePresignedURLR(content, 2, client);
                axWindowsMediaPlayer1.URL = content;
                axWindowsMediaPlayer1.Ctlcontrols.play();
                // منع النقر الأيمن على عنصر التحكم
                axWindowsMediaPlayer1.ContextMenuStrip = new ContextMenuStrip();

                // التعامل مع أحداث الفأرة
                axWindowsMediaPlayer1.MouseDownEvent += AxWindowsMediaPlayer1_MouseDown;
                StartWatchTimer();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الفيديو: " + ex.Message);
            }



        }
        // إضافة هذا الكود أيضاً

        #region MyRegion

        private void StartWatchTimer()
        {
            watchTimer = new Timer();
            watchTimer.Interval = 1000; // كل ثانية
            watchTimer.Tick += async (s, e) =>
            {
                if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
                {
                    watchedSeconds++;

                    if (!lessonMarked && watchedSeconds >= NumOfMinutes || NumOfMinutes == 0) // حولها لثواني
                    {
                        lessonMarked = true;
                        await MarkLessonAsWatchedWithRetry();
                    }
                }
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
                            IsVisitor = false   
                        };
                        var json = JsonConvert.SerializeObject(body); 
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



        #endregion

        private void AxWindowsMediaPlayer1_MouseDown(object sender, AxWMPLib._WMPOCXEvents_MouseDownEvent e)
        {
            // منع النقر الأيمن (2 = right button)
            if (e.nButton == 2)
            {
                // إلغاء الحدث ومنع ظهور القائمة
                // يمكنك أيضاً إضافة رسالة للمستخدم إذا أردت
                MessageBox.Show("غير مسموح باستخدام القائمة اليمنى");
            }
        }
        private void StartRecordingCheckTimer()
        {
            recordingCheckTimer.Interval = 3000; // كل 3 ثواني
            recordingCheckTimer.Tick += CheckForRecordingApps;
            recordingCheckTimer.Start();
        }
        private void frmvideoplayer_Load(object sender, EventArgs e)
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
        //private void InitializeStaticImage()
        //{
        //    staticImageBox = new PictureBox
        //    {
        //        Dock = DockStyle.Fill,
        //        //  Image = Properties.Resources.Screenshot__5_, // تأكد من إضافة الصورة إلى Resources
        //        SizeMode = PictureBoxSizeMode.StretchImage,
        //        Visible = false
        //    };

        //    this.Controls.Add(staticImageBox);
        //}

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
    }
}

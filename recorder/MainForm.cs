
using NAudio.Wave;
using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace LoopbackRecorder
{
    public partial class MainForm : Form




    {

        //文件夹图标
        private Rectangle folderIconRect = new Rectangle(15, 5, 28, 22);

        //时间框变量
        private Label timeLabel;
        //start开启事件注册
        private System.Windows.Forms.Timer timeTimer;
        private DateTime startTime;
        // 按钮动画
        private System.Windows.Forms.Timer breatheTimer;

        private float scale = 1.0f;
        private bool growing = true;
        private Button recordButton;

        private WasapiLoopbackCapture capture;
        private WaveFileWriter writer;

        private bool isRecording = false;
        private Label infoLabel;


        private TrackBar volumeSlider;

        private Label volumeLabel;
        //光晕参数
        //透明度
        private int glowAlpha = 150;
        private int dir = 5;

        // 当前增益
        private float gain = 1.5f;
        public MainForm()
        {
            InitializeComponent();
            //开启双缓冲防止闪烁
            this.DoubleBuffered = true;

            SetupUI();
            InitText();

            //固定窗体大小
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

        }
        private void InitText()



        {
            //光晕
            //绑定光晕,重绘即持续开启
            //此外绑定文件夹
            this.Paint += Form1_Paint;
            //绑定文件夹点击事件
            this.MouseClick += Form1_MouseClick;
            this.MouseMove += Form1_MouseMove;

            // 创建文本控件
            infoLabel = new Label();

            //时间框初始化 
            timeTimer = new System.Windows.Forms.Timer();
            timeTimer.Interval = 1000;

            timeTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - startTime;

                timeLabel.Text = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            };
            // 初始化时间框
            timeLabel = new Label();
            timeLabel.Location = new Point(70, 40);
            timeLabel.Text = "00:00";
            timeLabel.AutoSize = true;
            timeLabel.Font = new Font("Microsoft YaHei", 12);

            this.Controls.Add(timeLabel);



            // 设置文本内容
            infoLabel.Text = "amplitude";

            // 设置位置（核心）
            infoLabel.Location = new Point(155, 15); // X=50, Y=50

            // 设置大小（可选）
            infoLabel.AutoSize = true;

            // 字体设置（可选）
            infoLabel.Font = new Font("Microsoft YaHei", 9);

            // 加入窗体
            this.Controls.Add(infoLabel);
        }

        private void SetupUI()
        {
            // 窗口设置
            this.Text = "Loopback Recorder";

            this.ClientSize = new Size(220, 220);

            this.BackColor = Color.White;

            this.StartPosition = FormStartPosition.CenterScreen;


            volumeSlider = new TrackBar();

            volumeSlider.Minimum = 10;
            volumeSlider.Maximum = 300;
            volumeSlider.Orientation = Orientation.Vertical;
            // 默认 150%
            volumeSlider.Value = 150;

            volumeSlider.TickFrequency = 25;

            volumeSlider.Height = 130;


            volumeSlider.Location = new Point(185, 40);

            volumeSlider.Scroll += VolumeSlider_Scroll;

            this.Controls.Add(volumeSlider);


            // 数值文字
            volumeLabel = new Label();

            volumeLabel.Text = "150%";

            volumeLabel.AutoSize = true;

            volumeLabel.Location = new Point(180, 170);

            this.Controls.Add(volumeLabel);


            // 创建按钮
            int size = 70;
            recordButton = new Button();

            recordButton.Size = new Size(size, size);

            recordButton.Location = new Point(
                60,
                100
            );
            // 按钮动画

            breatheTimer = new System.Windows.Forms.Timer();
            breatheTimer.Interval = 30; // 约33fps
            breatheTimer.Tick += BreatheTick;


            // 按钮样式
            recordButton.FlatStyle = FlatStyle.Flat;

            recordButton.FlatAppearance.BorderSize = 0;

            recordButton.BackColor = Color.LimeGreen;

            // 圆形按钮绘制


            System.Drawing.Drawing2D.GraphicsPath path =
          new System.Drawing.Drawing2D.GraphicsPath();

            path.AddEllipse(0, 0, size, size);

            recordButton.Region = new Region(path);


            // 点击事件
            recordButton.Click += ToggleRecording;

            this.Controls.Add(recordButton);

        }
        //
        private void BreatheTick(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                recordButton.Width = 40;
                recordButton.Height = 40;
                recordButton.Region = new System.Drawing.Region(
                    new System.Drawing.Drawing2D.GraphicsPath()
                );
                return;
            }



            // 控制缩放速度
            if (growing)
                scale += 0.02f;
            else
                scale -= 0.02f;
            // 控制缩放范围
            if (scale >= 1.02f) growing = false;
            if (scale <= 0.98f) growing = true;

            int size = (int)(70 * scale);

            recordButton.Width = size;
            recordButton.Height = size;

            // 重新设置圆形区域
            System.Drawing.Drawing2D.GraphicsPath path =
                new System.Drawing.Drawing2D.GraphicsPath();

            path.AddEllipse(0, 0, size, size);

            recordButton.Region = new Region(path);

            recordButton.Invalidate();




            //光晕参数
            /*    glowAlpha += dir;

                if (glowAlpha > 100 || glowAlpha < 200)
                    dir = -dir;
    */  // 不透明度改为固定不再闪烁

            //注意这个重绘可以对paint生效，根 跟button的不一样
            Invalidate();






        }
        //绘制光晕
        //临时重绘，关闭了不会残留
        //文件夹
        private void Form1_Paint(object sender, PaintEventArgs e)
        {



            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 文件夹主体
            using (SolidBrush folderBrush =
                new SolidBrush(Color.FromArgb(230, 255, 210, 70)))
            {
                g.FillRectangle(
                    folderBrush,
                    folderIconRect.X,
                    folderIconRect.Y + 6,
                    folderIconRect.Width,
                    folderIconRect.Height - 6);
            }

            // 文件夹上方凸起
            using (SolidBrush topBrush =
                new SolidBrush(Color.FromArgb(255, 255, 225, 120)))
            {
                g.FillRectangle(
                    topBrush,
                    folderIconRect.X + 3,
                    folderIconRect.Y,
                    12,
                    8);



                if (!isRecording)
                {
                    return;
                }

                //光晕
                using (SolidBrush brush =
                    new SolidBrush(Color.FromArgb(100, 0, 120, 255)))
                {
                    g.FillEllipse(
                        brush,
                        recordButton.Left - 30,
                        recordButton.Top - 30,
                        recordButton.Width + 60,
                        recordButton.Height + 60);
                }



            }
        }

        //文件夹鼠标点击事件
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (folderIconRect.Contains(e.Location))
            {
                string saveFolder = Path.Combine(
                  AppDomain.CurrentDomain.BaseDirectory,
                    "Recordings");

                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }

                Process.Start("explorer.exe", saveFolder);
            }
        }
        //鼠标改变手型
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (folderIconRect.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        private void ToggleRecording(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }





        private void StartRecording()
        {
            try
            {


                //初始化时间框参数
                startTime = DateTime.Now;
                timeLabel.Text = "00:00";
                timeTimer.Start();

                //调用api
                capture = new WasapiLoopbackCapture();
                //开启时间事件
                breatheTimer.Start();

                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/Recordings"))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Recordings");
                }
                string outputPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory + "/Recordings",
                    $"record_{DateTime.Now:yyyyMMdd_HHmmss}.wav"
                );


                writer = new WaveFileWriter(
                    outputPath,
                    capture.WaveFormat
                );
                
                //注册监听事件
                capture.DataAvailable += (s, e) =>
                {
                    int bytesPerSample = 4;

                    for (int i = 0; i < e.BytesRecorded; i += bytesPerSample)
                    {
                        float sample = BitConverter.ToSingle(e.Buffer, i);

                        sample *= gain;

                        // 防止削波
                        sample = Math.Max(-1.0f, Math.Min(1.0f, sample));

                        byte[] bytes = BitConverter.GetBytes(sample);

                        Array.Copy(bytes, 0, e.Buffer, i, 4);
                    }

                    writer.Write(e.Buffer, 0, e.BytesRecorded);
                };
                capture.RecordingStopped += (s, e) =>
                {
                    writer?.Dispose();
                    writer = null;

                    capture?.Dispose();
                    capture = null;
                };

                capture.StartRecording();


                isRecording = true;

                recordButton.BackColor = Color.Red;

                recordButton.Invalidate();


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StopRecording()
        {
            capture?.StopRecording();

            isRecording = false;

            recordButton.BackColor = Color.LimeGreen;

            recordButton.Invalidate();
            breatheTimer.Stop();

            recordButton.Width = 80;
            recordButton.Height = 80;
            //清空时间框
            timeTimer.Stop();
            timeLabel.Text = "00:00";
            //重绘paint
            Invalidate();
        }

        private void VolumeSlider_Scroll(object sender, EventArgs e)
        {
            gain = volumeSlider.Value / 100f;

            volumeLabel.Text = $"{volumeSlider.Value}%";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
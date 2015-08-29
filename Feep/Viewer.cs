using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace Feep
{
    sealed internal partial class Viewer : Form
    {

        //用于调整窗体大小
        internal enum Direction
        {
            Left,
            Right,
            Top,
            Bottom,
            LeftTop,
            LeftBottom,
            RightTop,
            RightBottom
        }

        //屏幕模式
        internal enum ScreenState
        {
            None,
            Full,
            Left,
            Right,
            Top,
            Bottom
        }

        //文件排序
        sealed internal class StringLogicalComparer : IComparer<string>
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern int StrCmpLogicalW(string x, string y);

            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }

        }

        //获取文件路径
        sealed internal class FilePath
        {
            List<string> paths = new List<string>();

            internal List<string> Paths
            {
                get
                {
                    return paths;
                }
                set
                {
                    paths = value;
                }
            }

            string[] extensions;

            internal string[] Extensions
            {
                get
                {
                    return extensions;
                }
                set
                {
                    extensions = value;
                }
            }

            internal void GetPaths(object path)
            {
                DirectoryInfo dir = new DirectoryInfo((string)path);

                foreach (string extension in extensions)
                {
                    foreach (FileInfo f in dir.GetFiles("*." + extension + ""))
                    {
                        string temp = dir.FullName + f.ToString();
                        if (!paths.Contains(temp))
                        {
                            paths.Add(temp);
                        }
                    }
                }

            }

        }


        #region 屏蔽原窗体行为

        const int WM_SYSCOMMAND = 0x112;
        const int SC_CLOSE = 0xF060;
        const int SC_MINIMIZE = 0xF020;
        const int SC_MAXIMIZE = 0xF030;
        const int SC_NOMAL = 0xF120;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND)
            {
                switch (m.WParam.ToInt32())
                {
                    case SC_CLOSE:
                    case SC_MINIMIZE:
                    case SC_MAXIMIZE:
                    case SC_NOMAL:
                        {
                            Exit();
                            return;
                        }
                }
            }
            base.WndProc(ref m);

        }

        #endregion


        #region 缤纷背景色

        bool SaturationDirection = true;
        float Hue, Saturation, Brightness;
        Timer timerBackColor = new Timer();
        Random randomBackColor = new Random(DateTime.Now.Millisecond);


        public static void HSI_RGB(float H, float S, float I, out byte R, out byte G, out byte B)
        {
            float r = 0, g = 0, b = 0;
            int i = (int)((H / 60) % 6);
            float f = (H / 60) - i;
            float p = I * (1 - S);
            float q = I * (1 - f * S);
            float t = I * (1 - (1 - f) * S);
            switch (i)
            {
                case 0:
                    r = I;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = I;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = I;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = I;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = I;
                    break;
                case 5:
                    r = I;
                    g = p;
                    b = q;
                    break;
                default:
                    break;
            }
            R = Convert.ToByte(r * 255.0f);
            G = Convert.ToByte(g * 255.0f);
            B = Convert.ToByte(b * 255.0f);

        }


        private void timerBackColor_Tick(object sender, EventArgs e)
        {
            Hue = (Hue + 0.17f);
            Hue = Hue > 360.0f ? Hue - 360.0f : Hue;

            if (SaturationDirection)
            {
                Saturation += 0.005f;
                if (Saturation > 0.79)
                {
                    SaturationDirection = false;
                }
            }
            else
            {
                Saturation -= 0.005f;
                if (Saturation < 0.53)
                {
                    SaturationDirection = true;
                }
            }

            byte R, G, B;
            HSI_RGB(Hue, Saturation, Brightness, out R, out G, out B);

            this.BackColor = Color.FromArgb(R, G, B);
            this.Picture.BackColor = this.BackColor;

        }


        #endregion


        //文件路径
        List<string> filePaths;
        //当前图片
        Bitmap image;
        //调整窗体大小时的方向
        Direction direction;
        //当前屏幕模式
        internal ScreenState screen;

        //光标处于自然状态
        bool flag = true;
        //是否可以移动窗体
        bool MoveWindow = false;
        //窗体是否移动过了 (用于判断操作是移动窗体还是切换全屏和自然)
        bool MovedWindow = false;
        //是否可以调整窗体大小 (翻转也需要用到，原因是翻转的同时也在调整窗体大小)
        bool ResizeForm = false;
        //右键是否按下了，用于旋转图像，删除图片
        bool RightButtonsPress = false;
        //左键是否按下了
        bool LeftButtonsPress = false;
        //是否旋转了图像，如果没有旋转的情况下松开了右键时是退出
        bool IsRotation = false;
        //是否是删除图片的操作
        bool IsCustomer = false;
        //是否是缩放图片
        bool IsZoom = false;
        //是否需要关闭
        bool IsClose = false;
        //是否处于半屏模式状态
        internal bool AtBorderline = false;
        //记录当前图片所在文件夹的索引
        int index;
        //记录移动时光标相对应窗体的位置
        int MoveX;
        int MoveY;
        //切换到全屏和半屏模式之前窗体的位置和尺寸
        internal int ScreenBeforeX;
        internal int ScreenBeforeY;
        internal int ScreenBeforeWidth;
        internal int ScreenBeforeHeight;
        //原始大小时，图片的坐标位置
        static int DistanceX, DistanceY;
        //缩放时屏幕中央的位置
        Point Center;
        //锁定缩放
        bool IsLockZoom = false;
        //释放控制
        bool IsLoseControl = false;
        //记录了右边和下边的位置，用于图像翻转归位
        int? OptimizeLeftPosition;
        int? OptimizeTopPosition;
        //查看原始比例时候隐藏光标之前的位置，记录它是因为回到适合窗体比例的时候光标要回到隐藏前的位置
        Point HideBeforePosition;
        //阴影的容器，总是全屏而透明的
        static Shadow Shadows;
        //启动时的窗体状态
        internal int StartState;

        //写配置文件
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        //窗体动画，防止闪烁
        [DllImportAttribute("user32.dll")]
        private static extern bool AnimateWindow(IntPtr hwnd, int dwTime, int dwFlags);


        internal Viewer(string PicturePath)
        {
            InitializeComponent();

            string folder = PicturePath.Remove(PicturePath.LastIndexOf('\\')) + '\\';
            FilePath filePath = new FilePath();
            filePath.Extensions = new string[] { "jpg", "jpeg", "tif", "tiff", "png", "pns", "bmp", "rle", "dib", "gif" };
            filePath.GetPaths(folder);
            filePath.Paths.Sort(new StringLogicalComparer());
            filePaths = filePath.Paths;
            index = filePaths.IndexOf(PicturePath);

            Picture.MouseDown += new MouseEventHandler(Viewer_MouseDown);
            Picture.MouseUp += new MouseEventHandler(Viewer_MouseUp);
            Picture.MouseMove += new MouseEventHandler(Viewer_MouseMove);
            this.MouseWheel += Viewer_MouseWheel;

            timerBackColor.Interval = 32;
            timerBackColor.Tick += timerBackColor_Tick;

            Shadows = new Shadow();

        }


        internal bool Show(string PicturePath)
        {
            try
            {
                this.Text = PicturePath;
                image = new Bitmap(PicturePath);
                Picture.Image = image;
                Picture.Width = image.Width;
                Picture.Height = image.Height;
                ChangeSize();
                Picture.Show();
                return true;
            }
            catch
            {
                this.Text = "";
                MessageBox.Show("Can Not Open the Image File：\r" + filePaths[index] + "\r\r\tPlease Check the File's Format !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                image = new Bitmap(1, 1);
                filePaths.RemoveAt(index);
                if (filePaths.Count == 0)
                {
                    this.Dispose();
                    Application.Exit();
                }
                return false;
            }

        }


        private void Next()
        {
            if (image != null)
            {
                image.Dispose();
            }

            if (Show(index > 0 ? filePaths[--index] : filePaths[index = (filePaths.Count - 1)]) && (index == filePaths.Count - 1))
            {
                System.Media.SystemSounds.Asterisk.Play();
            }

        }


        private void Previous()
        {
            if (image != null)
            {
                image.Dispose();
            }

            if (Show(index < filePaths.Count - 1 ? filePaths[++index] : filePaths[index = 0]))
            {
                if (index == 0)
                {
                    System.Media.SystemSounds.Beep.Play();
                }
            }
            else
            {
                index--;
            }

        }


        private void ChangeSize()
        {
            if ((image.Width >= (this.Width)) || (image.Height >= (this.Height)))
            {
                this.Picture.SizeMode = PictureBoxSizeMode.Zoom;
                this.Picture.Dock = DockStyle.Fill;
            }
            else if ((image.Width < (this.Width)) && (image.Height < (this.Height)))
            {
                this.Picture.Dock = DockStyle.None;
                this.Picture.SizeMode = PictureBoxSizeMode.Normal;
                Picture.Location = new Point((this.Width - Picture.Width) / 2, (this.Height - Picture.Height) / 2);
            }

        }


        private void ChangeScreenState(bool isKey)
        {
            Screen currentScreen = Screen.FromPoint(Control.MousePosition);

            switch (screen)
            {
                case ScreenState.None:
                    if (MovedWindow == false)
                    {
                        screen = ScreenState.Full;
                        this.TopMost = true;
                        ScreenBeforeX = this.Location.X;
                        ScreenBeforeY = this.Location.Y;
                        ScreenBeforeWidth = this.Width;
                        ScreenBeforeHeight = this.Height;
                        AnimateWindow(this.Handle, 40, 0x00000010 + 0x00080000 + 0x00010000);
                        this.Location = currentScreen.Bounds.Location;
                        this.Width = currentScreen.Bounds.Width;
                        this.Height = currentScreen.Bounds.Height;
                        AnimateWindow(this.Handle, 80, 0x00000010 + 0x00080000 + 0x00020000);
                    }
                    break;
                case ScreenState.Full:
                    if (MovedWindow == false)
                    {
                        screen = ScreenState.None;
                        this.TopMost = false;
                        AnimateWindow(this.Handle, 40, 0x00000010 + 0x00080000 + 0x00010000);
                        this.Location = new Point(ScreenBeforeX, ScreenBeforeY);
                        this.Width = ScreenBeforeWidth;
                        this.Height = ScreenBeforeHeight;
                        AnimateWindow(this.Handle, 80, 0x00000010 + 0x00080000 + 0x00020000);
                    }
                    break;
                case ScreenState.Left:
                    if (!isKey)
                    {
                        Shadows.ClearShadow();
                        ScreenBeforeX = this.Location.X;
                        ScreenBeforeY = this.Location.Y;
                        ScreenBeforeWidth = this.Width;
                        ScreenBeforeHeight = this.Height;
                        AnimateWindow(this.Handle, 10, 0x00000010 + 0x00080000 + 0x00010000);
                        this.Location = currentScreen.WorkingArea.Location;
                        this.Width = currentScreen.WorkingArea.Width / 2;
                        this.Height = currentScreen.WorkingArea.Height;
                        AnimateWindow(this.Handle, 30, 0x00000010 + 0x00080000 + 0x00020000);
                        AtBorderline = true;
                    }
                    break;
                case ScreenState.Right:
                    if (!isKey)
                    {
                        Shadows.ClearShadow();
                        ScreenBeforeX = this.Location.X;
                        ScreenBeforeY = this.Location.Y;
                        ScreenBeforeWidth = this.Width;
                        ScreenBeforeHeight = this.Height;
                        AnimateWindow(this.Handle, 10, 0x00000010 + 0x00080000 + 0x00010000);
                        this.Location = new Point(currentScreen.WorkingArea.Width / 2 + currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y);
                        this.Width = currentScreen.WorkingArea.Width / 2;
                        this.Height = currentScreen.WorkingArea.Height;
                        AnimateWindow(this.Handle, 30, 0x00000010 + 0x00080000 + 0x00020000);
                        AtBorderline = true;
                    }
                    break;
                case ScreenState.Top:
                    if (!isKey)
                    {
                        Shadows.ClearShadow();
                        ScreenBeforeX = this.Location.X;
                        ScreenBeforeY = this.Location.Y;
                        ScreenBeforeWidth = this.Width;
                        ScreenBeforeHeight = this.Height;
                        AnimateWindow(this.Handle, 10, 0x00000010 + 0x00080000 + 0x00010000);
                        this.Location = currentScreen.WorkingArea.Location;
                        this.Width = currentScreen.WorkingArea.Width;
                        this.Height = currentScreen.WorkingArea.Height / 2;
                        AnimateWindow(this.Handle, 30, 0x00000010 + 0x00080000 + 0x00020000);
                        AtBorderline = true;
                    }
                    break;
                case ScreenState.Bottom:
                    if (!isKey)
                    {
                        Shadows.ClearShadow();
                        ScreenBeforeX = this.Location.X;
                        ScreenBeforeY = this.Location.Y;
                        ScreenBeforeWidth = this.Width;
                        ScreenBeforeHeight = this.Height;
                        AnimateWindow(this.Handle, 10, 0x00000010 + 0x00080000 + 0x00010000);
                        this.Location = new Point(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Height / 2 + currentScreen.WorkingArea.Y);
                        this.Width = currentScreen.WorkingArea.Width;
                        this.Height = currentScreen.WorkingArea.Height / 2;
                        AnimateWindow(this.Handle, 30, 0x00000010 + 0x00080000 + 0x00020000);
                        AtBorderline = true;
                    }
                    break;
            }

        }


        private void Exit()
        {
            if (StartState != 2)
            {
                if (screen != ScreenState.None)
                {

                    WritePrivateProfileString("Location", "X", ScreenBeforeX.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Location", "Y", ScreenBeforeY.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Size", "Width", ScreenBeforeWidth.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Size", "Height", ScreenBeforeHeight.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Form", "Screen", screen.ToString(), Application.StartupPath + @".\configure.ini");
                }
                else
                {

                    WritePrivateProfileString("Location", "X", this.Location.X.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Location", "Y", this.Location.Y.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Size", "Width", this.Width.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Size", "Height", this.Height.ToString(), Application.StartupPath + @".\configure.ini");
                    WritePrivateProfileString("Form", "Screen", "None", Application.StartupPath + @".\configure.ini");
                }
            }
            WritePrivateProfileString("Form", "BackColor", ColorTranslator.ToHtml(this.BackColor), Application.StartupPath + @".\configure.ini");
            WritePrivateProfileString("Form", "StartState", StartState.ToString(), Application.StartupPath + @".\configure.ini");
            WritePrivateProfileString("Form", "ShowInTaskbar", ShowInTaskbar.ToString(), Application.StartupPath + @".\configure.ini");

            Shadows.Dispose();
            this.Close();

        }


        private void Viewer_Load(object sender, EventArgs e)
        {
            if (index != -1)
            {
                Show(filePaths[index]);
            }
            else
            {
                this.Dispose();
                Application.Exit();
            }

        }


        private void Viewer_SizeChanged(object sender, EventArgs e)
        {
            if (IsLoseControl)
            {
                IsLoseControl = false;
                this.Cursor = Cursors.Cross;
                Cursor.Hide();
            }

            if (IsLockZoom)
            {
                IsLockZoom = false;

                LeftButtonsPress = false;

                IsZoom = false;
                ChangeSize();
                Cursor.Show();
                flag = true;
            }

            if (LeftButtonsPress)
            {
                LeftButtonsPress = false;

                if (IsZoom == true)
                {
                    IsZoom = false;
                    ChangeSize();
                    Cursor.Show();
                    flag = true;
                }
            }

            if (image != null)
            {
                ChangeSize();
            }

        }


        private void Viewer_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsClose == true)
            {
                return;
            }

            if (IsLockZoom)
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (IsLoseControl)
                    {
                        IsLoseControl = false;
                        this.Cursor = Cursors.Cross;
                        Cursor.Position = Center;
                        Cursor.Hide();
                    }
                    else
                    {
                        IsLoseControl = true;
                        this.Cursor = Cursors.Default;
                        Cursor.Position = HideBeforePosition;
                        Cursor.Show();
                    }
                }

                return;
            }

            int mouseX = Control.MousePosition.X - this.Location.X;
            int mouseY = Control.MousePosition.Y - this.Location.Y;

            if (e.Button == MouseButtons.Left)
            {
                LeftButtonsPress = true;

                if (((mouseX < 5) || ((this.Location.X + this.Width) - Control.MousePosition.X < 5) || (mouseY < 5) || ((this.Location.Y + this.Height) - Control.MousePosition.Y < 5)) && screen != ScreenState.Full)
                {
                    ResizeForm = true;
                    return;
                }

                IsZoom = true;
                HideBeforePosition = Cursor.Position;
                Cursor.Hide();

                if ((image.Width > (this.Width)) || (image.Height > (this.Height)))
                {
                    flag = false;
                    int PointX, PointY;//图片相对于窗体容器的坐标
                    double w = Convert.ToDouble(image.Width) / Convert.ToDouble(this.Width);
                    double h = Convert.ToDouble(image.Height) / Convert.ToDouble(this.Height);

                    if (w > h)//横向缩放比例比较大，横向缩放比例为图片缩放比例
                    {
                        PointX = Convert.ToInt32(mouseX * w);
                        PointY = Convert.ToInt32((mouseY - ((this.Height - (image.Height / w)) / 2)) * w);
                    }
                    else if (w < h)//纵向缩放比例比较大，纵向缩放比例为图片缩放比例
                    {
                        PointX = Convert.ToInt32((mouseX - ((this.Width - (image.Width / h)) / 2)) * h);
                        PointY = Convert.ToInt32(mouseY * h);
                    }
                    else
                    {
                        PointX = Convert.ToInt32(mouseX * w);
                        PointY = Convert.ToInt32(mouseY * h);
                    }

                    //先将在原始比例下光标所指的位置移到(0,0)点，再移到一个合适的位置
                    PointX = (5 * mouseX + this.Width) / 7 - PointX;
                    PointY = (5 * mouseY + this.Height) / 7 - PointY;

                    if (PointX > 0)
                    {
                        PointX = 0;
                    }
                    else if (PointX < this.Width - image.Width)
                    {
                        PointX = this.Width - image.Width;
                    }

                    if (PointY > 0)
                    {
                        PointY = 0;
                    }
                    else if (PointY < this.Height - image.Height)
                    {
                        PointY = this.Height - image.Height;
                    }

                    if (image.Width < this.Width)
                    {
                        PointX = (this.Width - image.Width) / 2;
                    }

                    if (image.Height < this.Height)
                    {
                        PointY = (this.Height - image.Height) / 2;
                    }

                    this.Picture.SizeMode = PictureBoxSizeMode.Normal;
                    this.Picture.Dock = DockStyle.None;
                    Picture.Width = image.Width;
                    Picture.Height = image.Height;
                    Picture.Location = new Point(PointX, PointY);

                    Rectangle visualRect = Rectangle.Intersect(Cursor.Clip, this.DesktopBounds);


                    Center = new Point(visualRect.X + visualRect.Width / 2, visualRect.Y + visualRect.Height / 2);
                    Cursor.Position = new Point(Center.X, Center.Y);

                    DistanceX = PointX;
                    DistanceY = PointY;
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                if (LeftButtonsPress)
                {
                    return;
                }

                if (RightButtonsPress)
                {
                    IsCustomer = true;
                    return;
                }

                MoveWindow = true;
                MoveX = mouseX;
                MoveY = mouseY;
            }
            else if (e.Button == MouseButtons.Right)
            {
                RightButtonsPress = true;
            }

        }


        private void Viewer_MouseUp(object sender, MouseEventArgs e)
        {
            if (IsLockZoom)
            {
                if (e.Button == MouseButtons.Middle && !IsLoseControl)
                {
                    IsLockZoom = false;

                    LeftButtonsPress = false;

                    IsZoom = false;
                    ChangeSize();
                    Cursor.Position = HideBeforePosition;
                    Cursor.Show();
                    flag = true;
                }

                return;
            }

            OptimizeLeftPosition = null;
            OptimizeTopPosition = null;

            if (ResizeForm == true)
            {
                ResizeForm = false;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                LeftButtonsPress = false;


                if (IsZoom == true)
                {
                    IsZoom = false;
                    ChangeSize();
                    Cursor.Position = HideBeforePosition;
                    Cursor.Show();
                    flag = true;
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                if (LeftButtonsPress)
                {
                    IsLockZoom = true;
                    return;
                }

                if (IsCustomer == true)
                {
                    Hue = randomBackColor.Next(0, 360);
                    Saturation = randomBackColor.Next(53, 79) / 100f;
                    Brightness = randomBackColor.Next(43, 53) / 100f;
                    byte R, G, B;
                    HSI_RGB(Hue, Saturation, Brightness, out R, out G, out B);
                    this.BackColor = Color.FromArgb(R, G, B);
                    this.Picture.BackColor = this.BackColor;
                    return;
                }

                ChangeScreenState(false);

                MoveWindow = false;
                MovedWindow = false;
            }
            else if (e.Button == MouseButtons.Right)
            {
                RightButtonsPress = false;

                if (IsRotation == true)
                {
                    IsRotation = false;
                }
                else if (IsCustomer == true)
                {
                    IsCustomer = false;
                }
                else
                {
                    Exit();
                }
            }

        }


        private void Viewer_MouseMove(object sender, MouseEventArgs e)
        {
            Screen currentScreen = Screen.FromPoint(Control.MousePosition);

            #region 更改光标样式&图像翻转

            if (screen != ScreenState.Full && (Control.MousePosition.X - this.Location.X < 5))
            {
                if (ResizeForm == true && direction == Direction.Right)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }
                Cursor.Current = Cursors.SizeWE;
                direction = Direction.Left;
            }

            if (screen != ScreenState.Full && ((this.Location.X + this.Width) - Control.MousePosition.X < 5))
            {
                if (ResizeForm == true && direction == Direction.Left)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }
                Cursor.Current = Cursors.SizeWE;
                direction = Direction.Right;
            }

            if (screen != ScreenState.Full && (Control.MousePosition.Y - this.Location.Y < 5))
            {
                if (ResizeForm == true && direction == Direction.Bottom)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                Cursor.Current = Cursors.SizeNS;
                direction = Direction.Top;
            }

            if (screen != ScreenState.Full && ((this.Location.Y + this.Height) - Control.MousePosition.Y < 5))
            {
                if (ResizeForm == true && direction == Direction.Top)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                Cursor.Current = Cursors.SizeNS;
                direction = Direction.Bottom;
            }

            if (screen != ScreenState.Full && ((Control.MousePosition.X - this.Location.X < 10) && (Control.MousePosition.Y - this.Location.Y < 10)))
            {
                Cursor.Current = Cursors.SizeNWSE;
                direction = Direction.LeftTop;
            }

            if (screen != ScreenState.Full && ((Control.MousePosition.X - this.Location.X < 10) && ((this.Location.Y + this.Height) - Control.MousePosition.Y < 10)))
            {
                Cursor.Current = Cursors.SizeNESW;
                direction = Direction.LeftBottom;
            }

            if (screen != ScreenState.Full && (((this.Location.X + this.Width) - Control.MousePosition.X < 10) && (Control.MousePosition.Y - this.Location.Y < 10)))
            {
                Cursor.Current = Cursors.SizeNESW;
                direction = Direction.RightTop;
            }

            if (screen != ScreenState.Full && (((this.Location.X + this.Width) - Control.MousePosition.X < 10) && ((this.Location.Y + this.Height) - Control.MousePosition.Y < 10)))
            {
                Cursor.Current = Cursors.SizeNWSE;
                direction = Direction.RightBottom;
            }

            #endregion


            #region 调整窗体大小

            if (ResizeForm == true)
            {
                if (OptimizeLeftPosition == null)
                {
                    OptimizeLeftPosition = this.Right;
                }

                if (OptimizeTopPosition == null)
                {
                    OptimizeTopPosition = this.Bottom;
                }

                switch (direction)
                {
                    case Direction.Left:
                        this.Width = (int)OptimizeLeftPosition - MousePosition.X;
                        this.Location = new Point(MousePosition.X, this.Location.Y);
                        break;
                    case Direction.Right:
                        this.Width = MousePosition.X - this.Left;
                        break;
                    case Direction.Top:
                        this.Height = (int)OptimizeTopPosition - MousePosition.Y;
                        this.Location = new Point(this.Location.X, MousePosition.Y);
                        break;
                    case Direction.Bottom:
                        this.Height = MousePosition.Y - this.Top;
                        break;
                    case Direction.LeftTop:
                        this.Width = (int)OptimizeLeftPosition - MousePosition.X;
                        this.Height = (int)OptimizeTopPosition - MousePosition.Y;
                        this.Location = new Point(MousePosition.X, MousePosition.Y);
                        break;
                    case Direction.LeftBottom:
                        this.Width = (int)OptimizeLeftPosition - MousePosition.X;
                        this.Height = MousePosition.Y - this.Top;
                        this.Location = new Point(MousePosition.X, this.Location.Y);
                        break;
                    case Direction.RightTop:
                        this.Width = MousePosition.X - this.Left;
                        this.Height = (int)OptimizeTopPosition - MousePosition.Y;
                        this.Location = new Point(this.Location.X, MousePosition.Y);
                        break;
                    case Direction.RightBottom:
                        this.Width = MousePosition.X - this.Left;
                        this.Height = MousePosition.Y - this.Top;
                        break;
                }

                return;
            }

            #endregion


            if (MoveWindow == true)
            {

                #region 半屏阴影

                if (Control.MousePosition.X < currentScreen.Bounds.Left + 2)
                {
                    screen = ScreenState.Left;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width / 2, currentScreen.WorkingArea.Height));
                }
                else if (Control.MousePosition.X > currentScreen.Bounds.Right - 2)
                {
                    screen = ScreenState.Right;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.Width / 2 + currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width / 2, currentScreen.WorkingArea.Height));
                }
                else if (Control.MousePosition.Y < currentScreen.Bounds.Top + 2)
                {
                    screen = ScreenState.Top;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width, currentScreen.WorkingArea.Height / 2));
                }
                else if (Control.MousePosition.Y > currentScreen.Bounds.Bottom - 2)
                {
                    screen = ScreenState.Bottom;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Height / 2 + currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width, currentScreen.WorkingArea.Height / 2));
                }
                else
                {
                    if (screen != ScreenState.Full)
                    {
                        screen = ScreenState.None;
                    }
                    Shadows.ClearShadow();
                }

                #endregion


                if (screen != ScreenState.Full)
                {
                    if (AtBorderline == true)
                    {
                        screen = ScreenState.None;
                        this.TopMost = false;
                        this.Width = ScreenBeforeWidth;
                        this.Height = ScreenBeforeHeight;
                        MoveX = MoveX > Width ? Width : MoveX;
                        MoveY = MoveY > Height ? Height : MoveY;
                        AtBorderline = false;
                    }
                    else
                    {
                        Cursor.Current = Cursors.SizeAll;
                        this.Location = new Point(Control.MousePosition.X - MoveX, Control.MousePosition.Y - MoveY);
                    }
                }

                MovedWindow = true;
            }
            else if ((bool)flag == false && !IsLoseControl)
            {
                if (Cursor.Position.X == Center.X && Cursor.Position.Y == Center.Y)
                    return;

                DistanceX += Cursor.Position.X - Center.X;
                DistanceY += Cursor.Position.Y - Center.Y;
                Cursor.Position = Center;

                bool ImmovableX = false;
                bool ImmovableY = false;

                if (image.Width < this.Width)
                {
                    ImmovableX = true;
                }

                if (image.Height < this.Height)
                {
                    ImmovableY = true;
                }

                if (DistanceX > 0)
                {
                    DistanceX = 0;
                }
                if (DistanceX < (this.Width - image.Width))
                {
                    DistanceX = this.Width - image.Width;
                }
                if (DistanceY > 0)
                {
                    DistanceY = 0;
                }
                if (DistanceY < (this.Height - image.Height))
                {
                    DistanceY = this.Height - image.Height;
                }

                if (ImmovableX == true && ImmovableY == true)
                {
                    Picture.Location = new Point((this.Width - image.Width) / 2, (this.Height - image.Height) / 2);
                }
                else if (ImmovableX == true)
                {
                    Picture.Location = new Point((this.Width - image.Width) / 2, DistanceY);
                }
                else if (ImmovableY == true)
                {
                    Picture.Location = new Point(DistanceX, (this.Height - image.Height) / 2);
                }
                else
                {
                    Picture.Location = new Point(DistanceX, DistanceY);
                }

                Picture.Refresh();

            }
            else
            {
                Shadows.ClearShadow();
            }

        }


        private void Viewer_MouseWheel(object sender, MouseEventArgs e)
        {
            if (IsLockZoom)
            {
                return;
            }

            if (e.Delta > 0)
            {
                if (RightButtonsPress == true && LeftButtonsPress == false)
                {
                    IsRotation = true;
                    image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    Picture.Width = image.Width;
                    Picture.Height = image.Height;
                    ChangeSize();
                    Picture.Refresh();
                    return;
                }

                if (LeftButtonsPress == true)
                {
                    return;
                }

                Next();

            }
            else if (e.Delta < 0)
            {
                if (RightButtonsPress == true && LeftButtonsPress == false)
                {
                    IsRotation = true;
                    image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    Picture.Width = image.Width;
                    Picture.Height = image.Height;
                    ChangeSize();
                    Picture.Refresh();
                    return;
                }

                if (LeftButtonsPress == true)
                {
                    return;
                }

                Previous();

            }
        }


        private void Viewer_KeyUp(object sender, KeyEventArgs e)
        {

            if (!e.Control && !e.Shift && !e.Alt)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Exit();
                }

                if (IsLockZoom | LeftButtonsPress)
                {
                    return;
                }

                switch (e.KeyCode)
                {
                    case Keys.Space:
                        {
                            ChangeScreenState(true);
                            break;
                        }
                    case Keys.Up:
                        {
                            Next();
                            break;
                        }
                    case Keys.Down:
                        {
                            Previous();
                            break;
                        }
                    case Keys.Left:
                        {
                            image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            Picture.Width = image.Width;
                            Picture.Height = image.Height;
                            ChangeSize();
                            Picture.Refresh();
                            break;
                        }
                    case Keys.Right:
                        {
                            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            Picture.Width = image.Width;
                            Picture.Height = image.Height;
                            ChangeSize();
                            Picture.Refresh();
                            break;
                        }
                }

                return;
            }

            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A:
                        {
                            Clipboard.SetText(filePaths[index]);
                            break;
                        }
                    case Keys.B:
                        {
                            if (!timerBackColor.Enabled)
                            {
                                Hue = randomBackColor.Next(0, 360);
                                Saturation = randomBackColor.Next(53, 79) / 100f;
                                Brightness = randomBackColor.Next(43, 53) / 100f;
                                timerBackColor.Start();
                            }
                            else
                            {
                                timerBackColor.Stop();
                            }

                            break;
                        }
                    case Keys.C:
                        {
                            Clipboard.SetImage(Picture.Image);
                            break;
                        }
                }
            }

            if (e.Shift)
            {
                switch (e.KeyCode)
                {
                    case Keys.Delete:
                        {
                            if (IsLockZoom | LeftButtonsPress)
                            {
                                return;
                            }

                            try
                            {
                                image.Dispose();
                                File.Delete(filePaths[index]);
                                filePaths.RemoveAt(index);
                                if (filePaths.Count < 1)
                                {
                                    Exit();
                                    return;
                                }
                                Show(index < filePaths.Count ? filePaths[index] : filePaths[index = 0]);
                                System.Media.SystemSounds.Exclamation.Play();
                            }
                            catch
                            {
                            }
                            break;
                        }
                    case Keys.Enter:
                        {
                            System.Diagnostics.Process.Start("explorer.exe", @"/select, " + filePaths[index]);
                            Exit();
                            break;
                        }
                }
            }

        }


    }
}

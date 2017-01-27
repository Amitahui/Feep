using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

namespace Feep
{
    sealed internal partial class Viewer : Form
    {

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

        internal enum ScreenState
        {
            None,
            Full,
            Left,
            Right,
            Top,
            Bottom
        }

        sealed internal class StringLogicalComparer : IComparer<string>
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern int StrCmpLogicalW(string x, string y);

            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }

        }

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
                    foreach (FileInfo file in dir.GetFiles("*." + extension + ""))
                    {
                        string temp = dir.FullName + file.ToString();
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

        //文件路径
        List<string> filePaths;
        //图片
        Bitmap image;
        //缓存
        Bitmap cache;
        //调整窗体大小时的方向
        Direction direction;
        //屏幕模式
        internal ScreenState screenState;
        //光标状态
        bool cursorState = true;
        //是否可以移动窗体
        bool MoveWindow = false;
        //窗体是否移动过了
        bool MovedWindow = false;
        //是否可以调整窗体大小
        bool ResizeForm = false;
        //右键是否按下了
        bool RightButtonsPress = false;
        //左键是否按下了
        bool LeftButtonsPress = false;
        //是否为旋转图片的操作
        bool IsRotation = false;
        //是否为缩放图片的操作
        bool IsZoom = false;
        //是否为退出程序的操作
        bool IsClose = false;
        //是否处于半屏模式状态
        internal bool AtBorderline = false;
        //当前图片所在文件夹中的索引
        int Index;
        //移动时光标相对窗体的位置
        int MoveX;
        int MoveY;
        //切换到全屏和半屏模式之前窗体的位置和尺寸
        internal int ScreenBeforeX;
        internal int ScreenBeforeY;
        internal int ScreenBeforeWidth;
        internal int ScreenBeforeHeight;
        //查看原始比例时图片的坐标位置
        static int DistanceX, DistanceY;
        //缩放时屏幕中央的位置
        Point Center;
        //锁定缩放
        bool IsLockZoom = false;
        //释放控制
        bool IsLoseControl = false;
        //右边和下边的位置
        int? OptimizeLeftPosition;
        int? OptimizeTopPosition;
        //查看原始比例时隐藏光标之前的位置
        Point HideBeforePosition;
        //阴影的容器
        static Shadow Shadows;
        //启动时的窗体状态
        internal int StartState;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

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
            Index = filePaths.IndexOf(PicturePath);

            Picture.MouseDown += new MouseEventHandler(Viewer_MouseDown);
            Picture.MouseUp += new MouseEventHandler(Viewer_MouseUp);
            Picture.MouseMove += new MouseEventHandler(Viewer_MouseMove);
            this.MouseWheel += Viewer_MouseWheel;

            Shadows = new Shadow();

        }

        internal void ShowPicture(int index, bool orientation)
        {
            try
            {
                string PicturePath = filePaths[index];
                this.Text = PicturePath;

                if (cache != null && PicturePath == cache.Tag.ToString())
                {
                    image.Dispose();
                    Picture.Image = cache;
                    Picture.Width = cache.Width;
                    Picture.Height = cache.Height;
                }
                else
                {
                    if (cache != null)
                    {
                        cache.Dispose();
                    }
                    image = new Bitmap(PicturePath);
                    Picture.Image = image;
                    Picture.Width = image.Width;
                    Picture.Height = image.Height;
                }

                ChangeSize();
                Picture.Show();
                Index = index;
            }
            catch
            {
                if (orientation)
                {
                    ShowPicture(NextFilePath(index), orientation);
                }
                else
                {
                    ShowPicture(PreviousFilePath(index), orientation);
                }

                filePaths.RemoveAt(index);

                if (filePaths.Count == 0)
                {
                    this.Dispose();
                    Application.Exit();
                }
            }

            new Thread(action =>
            {
                BuildCache(orientation);
            }).Start();

        }

        private int PreviousFilePath(int index)
        {
            return index > 0 ? index - 1 : filePaths.Count - 1;
        }

        private int NextFilePath(int index)
        {
            return index < filePaths.Count - 1 ? index + 1 : 0;
        }

        private void PreviousPicture()
        {
            Picture.Image.Dispose();
            ShowPicture(PreviousFilePath(Index), false);
            if ((Index == filePaths.Count - 1))
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
        }

        private void NextPicture()
        {
            Picture.Image.Dispose();
            ShowPicture(NextFilePath(Index), true);
            if (Index == 0)
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }

        private void ChangeSize()
        {
            if ((Picture.Image.Width >= (this.Width)) || (Picture.Image.Height >= (this.Height)))
            {
                this.Picture.SizeMode = PictureBoxSizeMode.Zoom;
                this.Picture.Dock = DockStyle.Fill;
            }
            else if ((Picture.Image.Width < (this.Width)) && (Picture.Image.Height < (this.Height)))
            {
                this.Picture.Dock = DockStyle.None;
                this.Picture.SizeMode = PictureBoxSizeMode.Normal;
                Picture.Location = new Point((this.Width - Picture.Width) / 2, (this.Height - Picture.Height) / 2);
            }

        }

        private void ChangeScreenState(bool isKey)
        {
            Screen currentScreen = Screen.FromPoint(Control.MousePosition);

            switch (screenState)
            {
                case ScreenState.None:
                    if (MovedWindow == false)
                    {
                        screenState = ScreenState.Full;
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
                        screenState = ScreenState.None;
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

        private void BuildCache(bool orientation)
        {
            int index = Index;

            try
            {
                if (orientation)
                {
                    index = NextFilePath(index);
                }
                else
                {
                    index = PreviousFilePath(index);
                }

                cache = new Bitmap(filePaths[index]);
                cache.Tag = filePaths[index];
            }
            catch
            {
                filePaths.RemoveAt(index);
                BuildCache(orientation);
            }
        }

        private void DeleteFile(string filePath, bool sendToRecycleBin)
        {
            new Thread(action =>
            {
                while (File.Exists(filePath))
                {
                    try
                    {
                        if (sendToRecycleBin)
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filePath, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                        }
                        else
                        {
                            File.Delete(filePath);
                        }
                    }
                    catch
                    {
                        Thread.Sleep(100);
                    }
                }
                Thread.CurrentThread.Abort();
            }).Start();
        }

        private void Exit()
        {
            if (StartState != 2)
            {
                if (screenState != ScreenState.None)
                {

                    WritePrivateProfileString("Location", "X", ScreenBeforeX.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Location", "Y", ScreenBeforeY.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Size", "Width", ScreenBeforeWidth.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Size", "Height", ScreenBeforeHeight.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Form", "Screen", screenState.ToString(), Application.StartupPath + @"\configure.ini");
                }
                else
                {

                    WritePrivateProfileString("Location", "X", this.Location.X.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Location", "Y", this.Location.Y.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Size", "Width", this.Width.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Size", "Height", this.Height.ToString(), Application.StartupPath + @"\configure.ini");
                    WritePrivateProfileString("Form", "Screen", "None", Application.StartupPath + @"\configure.ini");
                }
            }
            WritePrivateProfileString("Form", "BackColor", ColorTranslator.ToHtml(this.BackColor), Application.StartupPath + @"\configure.ini");
            WritePrivateProfileString("Form", "StartState", StartState.ToString(), Application.StartupPath + @"\configure.ini");
            WritePrivateProfileString("Form", "ShowInTaskbar", ShowInTaskbar.ToString(), Application.StartupPath + @"\configure.ini");

            Shadows.Dispose();
            this.Close();

        }

        private void Viewer_Load(object sender, EventArgs e)
        {
            if (Index != -1)
            {
                ShowPicture(Index, true);
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
                cursorState = true;
            }

            if (LeftButtonsPress)
            {
                LeftButtonsPress = false;

                if (IsZoom == true)
                {
                    IsZoom = false;
                    ChangeSize();
                    Cursor.Show();
                    cursorState = true;
                }
            }

            if (Picture.Image != null)
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
                        HideBeforePosition = Cursor.Position;
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

                if (((mouseX < 5) || ((this.Location.X + this.Width) - Control.MousePosition.X < 5) || (mouseY < 5) || ((this.Location.Y + this.Height) - Control.MousePosition.Y < 5)) && screenState != ScreenState.Full)
                {
                    ResizeForm = true;
                    return;
                }

                IsZoom = true;
                HideBeforePosition = Cursor.Position;
                Cursor.Hide();

                if ((Picture.Image.Width > (this.Width)) || (Picture.Image.Height > (this.Height)))
                {
                    cursorState = false;
                    int PointX, PointY;
                    double w = Convert.ToDouble(Picture.Image.Width) / Convert.ToDouble(this.Width);
                    double h = Convert.ToDouble(Picture.Image.Height) / Convert.ToDouble(this.Height);

                    if (w > h)
                    {
                        PointX = Convert.ToInt32(mouseX * w);
                        PointY = Convert.ToInt32((mouseY - ((this.Height - (Picture.Image.Height / w)) / 2)) * w);
                    }
                    else if (w < h)
                    {
                        PointX = Convert.ToInt32((mouseX - ((this.Width - (Picture.Image.Width / h)) / 2)) * h);
                        PointY = Convert.ToInt32(mouseY * h);
                    }
                    else
                    {
                        PointX = Convert.ToInt32(mouseX * w);
                        PointY = Convert.ToInt32(mouseY * h);
                    }

                    PointX = (5 * mouseX + this.Width) / 7 - PointX;
                    PointY = (5 * mouseY + this.Height) / 7 - PointY;

                    if (PointX > 0)
                    {
                        PointX = 0;
                    }
                    else if (PointX < this.Width - Picture.Image.Width)
                    {
                        PointX = this.Width - Picture.Image.Width;
                    }

                    if (PointY > 0)
                    {
                        PointY = 0;
                    }
                    else if (PointY < this.Height - Picture.Image.Height)
                    {
                        PointY = this.Height - Picture.Image.Height;
                    }

                    if (Picture.Image.Width < this.Width)
                    {
                        PointX = (this.Width - Picture.Image.Width) / 2;
                    }

                    if (Picture.Image.Height < this.Height)
                    {
                        PointY = (this.Height - Picture.Image.Height) / 2;
                    }

                    this.Picture.SizeMode = PictureBoxSizeMode.Normal;
                    this.Picture.Dock = DockStyle.None;
                    Picture.Width = Picture.Image.Width;
                    Picture.Height = Picture.Image.Height;
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
                    cursorState = true;
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
                    cursorState = true;
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                if (LeftButtonsPress)
                {
                    IsLockZoom = true;
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

            if (screenState != ScreenState.Full && (Control.MousePosition.X - this.Location.X < 5))
            {
                if (ResizeForm == true && direction == Direction.Right)
                {
                    Picture.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }
                Cursor.Current = Cursors.SizeWE;
                direction = Direction.Left;
            }

            if (screenState != ScreenState.Full && ((this.Location.X + this.Width) - Control.MousePosition.X < 5))
            {
                if (ResizeForm == true && direction == Direction.Left)
                {
                    Picture.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }
                Cursor.Current = Cursors.SizeWE;
                direction = Direction.Right;
            }

            if (screenState != ScreenState.Full && (Control.MousePosition.Y - this.Location.Y < 5))
            {
                if (ResizeForm == true && direction == Direction.Bottom)
                {
                    Picture.Image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                Cursor.Current = Cursors.SizeNS;
                direction = Direction.Top;
            }

            if (screenState != ScreenState.Full && ((this.Location.Y + this.Height) - Control.MousePosition.Y < 5))
            {
                if (ResizeForm == true && direction == Direction.Top)
                {
                    Picture.Image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                Cursor.Current = Cursors.SizeNS;
                direction = Direction.Bottom;
            }

            if (screenState != ScreenState.Full && ((Control.MousePosition.X - this.Location.X < 10) && (Control.MousePosition.Y - this.Location.Y < 10)))
            {
                Cursor.Current = Cursors.SizeNWSE;
                direction = Direction.LeftTop;
            }

            if (screenState != ScreenState.Full && ((Control.MousePosition.X - this.Location.X < 10) && ((this.Location.Y + this.Height) - Control.MousePosition.Y < 10)))
            {
                Cursor.Current = Cursors.SizeNESW;
                direction = Direction.LeftBottom;
            }

            if (screenState != ScreenState.Full && (((this.Location.X + this.Width) - Control.MousePosition.X < 10) && (Control.MousePosition.Y - this.Location.Y < 10)))
            {
                Cursor.Current = Cursors.SizeNESW;
                direction = Direction.RightTop;
            }

            if (screenState != ScreenState.Full && (((this.Location.X + this.Width) - Control.MousePosition.X < 10) && ((this.Location.Y + this.Height) - Control.MousePosition.Y < 10)))
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
                    screenState = ScreenState.Left;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width / 2, currentScreen.WorkingArea.Height));
                }
                else if (Control.MousePosition.X > currentScreen.Bounds.Right - 2)
                {
                    screenState = ScreenState.Right;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.Width / 2 + currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width / 2, currentScreen.WorkingArea.Height));
                }
                else if (Control.MousePosition.Y < currentScreen.Bounds.Top + 2)
                {
                    screenState = ScreenState.Top;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width, currentScreen.WorkingArea.Height / 2));
                }
                else if (Control.MousePosition.Y > currentScreen.Bounds.Bottom - 2)
                {
                    screenState = ScreenState.Bottom;
                    Shadows.FillShadow(new Rectangle(currentScreen.WorkingArea.X, currentScreen.WorkingArea.Height / 2 + currentScreen.WorkingArea.Y, currentScreen.WorkingArea.Width, currentScreen.WorkingArea.Height / 2));
                }
                else
                {
                    if (screenState != ScreenState.Full)
                    {
                        screenState = ScreenState.None;
                    }
                    Shadows.ClearShadow();
                }

                #endregion

                if (screenState != ScreenState.Full)
                {
                    if (AtBorderline == true)
                    {
                        screenState = ScreenState.None;
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
            else if ((bool)cursorState == false && !IsLoseControl)
            {
                if (Cursor.Position.X == Center.X && Cursor.Position.Y == Center.Y)
                    return;

                DistanceX += Cursor.Position.X - Center.X;
                DistanceY += Cursor.Position.Y - Center.Y;
                Cursor.Position = Center;

                bool ImmovableX = false;
                bool ImmovableY = false;

                if (Picture.Image.Width < this.Width)
                {
                    ImmovableX = true;
                }

                if (Picture.Image.Height < this.Height)
                {
                    ImmovableY = true;
                }

                if (DistanceX > 0)
                {
                    DistanceX = 0;
                }
                if (DistanceX < (this.Width - Picture.Image.Width))
                {
                    DistanceX = this.Width - Picture.Image.Width;
                }
                if (DistanceY > 0)
                {
                    DistanceY = 0;
                }
                if (DistanceY < (this.Height - Picture.Image.Height))
                {
                    DistanceY = this.Height - Picture.Image.Height;
                }

                if (ImmovableX == true && ImmovableY == true)
                {
                    Picture.Location = new Point((this.Width - Picture.Image.Width) / 2, (this.Height - Picture.Image.Height) / 2);
                }
                else if (ImmovableX == true)
                {
                    Picture.Location = new Point((this.Width - Picture.Image.Width) / 2, DistanceY);
                }
                else if (ImmovableY == true)
                {
                    Picture.Location = new Point(DistanceX, (this.Height - Picture.Image.Height) / 2);
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
                    Picture.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    Picture.Width = Picture.Image.Width;
                    Picture.Height = Picture.Image.Height;
                    ChangeSize();
                    Picture.Refresh();
                    return;
                }

                if (LeftButtonsPress == true)
                {
                    return;
                }

                PreviousPicture();

            }
            else if (e.Delta < 0)
            {
                if (RightButtonsPress == true && LeftButtonsPress == false)
                {
                    IsRotation = true;
                    Picture.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    Picture.Width = Picture.Image.Width;
                    Picture.Height = Picture.Image.Height;
                    ChangeSize();
                    Picture.Refresh();
                    return;
                }

                if (LeftButtonsPress == true)
                {
                    return;
                }

                NextPicture();

            }
        }

        private void Viewer_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Delete)
            {
                if (IsLockZoom | LeftButtonsPress)
                {
                    return;
                }

                try
                {
                    string filePath = filePaths[Index];

                    Picture.Image.Dispose();

                    filePaths.RemoveAt(Index);

                    if (filePaths.Count < 1)
                    {
                        if (e.Shift)
                        {
                            DeleteFile(filePath, false);
                        }
                        else
                        {
                            DeleteFile(filePath, true);
                        }

                        Exit();
                        return;
                    }
                    else
                    {
                        ShowPicture(Index < filePaths.Count ? Index : Index = 0, true);
                        System.Media.SystemSounds.Exclamation.Play();

                        if (e.Shift)
                        {
                            DeleteFile(filePath, false);
                        }
                        else
                        {
                            DeleteFile(filePath, true);
                        }
                    }

                }
                catch
                {
                    return;
                }
            }

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
                            PreviousPicture();
                            break;
                        }
                    case Keys.Down:
                        {
                            NextPicture();
                            break;
                        }
                    case Keys.Left:
                        {
                            Picture.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            Picture.Width = Picture.Image.Width;
                            Picture.Height = Picture.Image.Height;
                            ChangeSize();
                            Picture.Refresh();
                            break;
                        }
                    case Keys.Right:
                        {
                            Picture.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            Picture.Width = Picture.Image.Width;
                            Picture.Height = Picture.Image.Height;
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
                            Clipboard.SetText(filePaths[Index]);
                            break;
                        }
                    case Keys.B:
                        {
                            System.Diagnostics.Process.Start("explorer.exe", @"/select, " + filePaths[Index]);
                            break;
                        }
                    case Keys.C:
                        {
                            Clipboard.SetImage(Picture.Image);
                            break;
                        }
                }
            }

        }

    }
}

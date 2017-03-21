using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace 決行小程式
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        GlobalHotKey hotkey1, hotkey2, hotkey3;
        int clickTimes = 0;
        
        float screenfactor;
        Point pt = new Point();
        int TrueX;
        int TrueY;
        List<object> ClickPriotity = new List<object>();

        NotifyIcon nicon = new NotifyIcon();
        bool isWorking = false;
        bool isWorkingH3 = false;
        private void Form1_Load(object sender, EventArgs e)
        {
            screenfactor = getScalingFactor();

            hotkey2 = new GlobalHotKey(this.Handle, Keys.F2, Keys.Control); //註冊Control+F2為熱鍵(record)
            hotkey2.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey2_OnHotkey);

            hotkey3 = new GlobalHotKey(this.Handle, Keys.F3, Keys.Control);//註冊Control+F3為熱鍵(play)
            hotkey3.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey3_OnHotkey);

            //開始監聽滑鼠位置
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(AutoGetCursorPosition), null);
            //-------------------------------
            
            nicon.Icon = new Icon("sun glasses.ico");
            nicon.Text = "QuickMacro";
            //this.Icon = nicon.Icon;
            nicon.Click += new EventHandler(nicon_Click);
        }
        void nicon_Click(object sender, EventArgs e)
        {
            nicon.Visible = false;
            this.Show();
            Win32Native.Methods.SetForegroundWindow(this.Handle);
        }

        #region Calculate Screen Scaling-Factor
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,

            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }
        private float getScalingFactor()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

            return ScreenScalingFactor; // 1.25 = 125%
        }
        #endregion
        private void GetCursorPosition()
        {
            Win32Native.Methods.GetCursorPos(out pt);
            TrueX = (int)(pt.X * screenfactor);
            TrueY = (int)(pt.Y * screenfactor);
        }
        void AutoGetCursorPosition(object obj)
        {                        
            while (true)
            {
                GetCursorPosition();
                try
                {
                    //TruePixel = point * scaling
                    SetText(this.label1, "滑鼠位置 : ( " + TrueX   + " , " + TrueY + " )");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace.ToString());
                    break;
                }
                System.Threading.Thread.Sleep(50);
            }
        }
        delegate void SetTextDelegate(Control c, String str);//Set Text
        void SetText(Control c, String str)
        {
            if (c.InvokeRequired) c.Invoke(new SetTextDelegate(SetText), c, str);
            else c.Text = str;
        }

        #region 按下"註冊"，測試mouse & keyboard hook
        private void button2_Click(object sender, EventArgs e)  //註冊.test用
        {
            hotkey1 = new GlobalHotKey(this.Handle, Keys.F2, Keys.Alt); //註冊F2為熱鍵
            hotkey1.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey1_OnHotkey); //hotkey1
        }
        private void hotkey1_OnHotkey(object sender, HotKeyEventArgs e)
        {
            WindwosHook.WindowsHook_Mouse.Enabled = true;
            //如何抓down & up 事件?????????????????????????????????????????????????????????
            WindwosHook.WindowsHook_Mouse.GlobalMouseDown += new EventHandler<WindwosHook.WindowsHook_Mouse.MouseEventArgs>(MouseHook_Mouse);
            WindwosHook.KeyboardHook.Enabled = true;
            //如何抓單鍵&組合鍵????????????????????????????????????????????????????????????
            WindwosHook.KeyboardHook.GlobalKeyDown += new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(KeyboardHook_Keyboard);
            
        }
        private void MouseHook_Mouse(object sender, WindwosHook.WindowsHook_Mouse.MouseEventArgs e)//抓mouse event
        {
            Win32Native.Methods.GetCursorPos(out pt); //直接紀錄pt即可，因winform的dpi scale難以轉換成window的正確scale
            ClickPriotity.Add(pt);
            textBox1.Text += e.Button + ", x=" + e.X.ToString() + ", y=" + e.Y.ToString() + "\r\n";
        }
        void KeyboardHook_Keyboard(object sender, WindwosHook.KeyboardHook.KeyEventArgs e)
        {
            ClickPriotity.Add(e.Keys);
            textBox1.Text += e.Keys + e.GetType().ToString()+"\r\n"; 
        }
        private void button4_Click(object sender, EventArgs e) //取消
        {
            WindwosHook.WindowsHook_Mouse.GlobalMouseDown -= new EventHandler<WindwosHook.WindowsHook_Mouse.MouseEventArgs>(MouseHook_Mouse);
            WindwosHook.KeyboardHook.GlobalKeyDown -= new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(KeyboardHook_Keyboard);
            hotkey1.Dispose();
            textBox1.Text = "";
            for(int i = 0; i < ClickPriotity.Count ; i++)
            {
                textBox1.Text += ClickPriotity[i].ToString() + "type=" + ClickPriotity[i].GetType().ToString()+ "\r\n";
            }
            label5.Text = ClickPriotity.Count.ToString();
        }
        #endregion
        
        private void hotkey2_OnHotkey(object sender, HotKeyEventArgs e) //Ctral + F2(record)
        {
            isWorking = !isWorking; //按一次=true開始(+= new EventHandler),第二次=false關掉(-= new EventHandler)
            if (isWorking)
            {   
                clickTimes = 0;
                ClickPriotity.RemoveAll(it => true);
                MessageBox.Show("開始紀錄典籍位置!\r\n本程式自動縮小。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //--開始監聽keyboard & mouse
                WindwosHook.WindowsHook_Mouse.Enabled = true;
                WindwosHook.WindowsHook_Mouse.GlobalMouseDown += new EventHandler<WindwosHook.WindowsHook_Mouse.MouseEventArgs>(MouseHook_GlobalMouseDown);
                WindwosHook.KeyboardHook.Enabled = true;
                WindwosHook.KeyboardHook.GlobalKeyDown += new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(KeyboardHook_Keyboard);
                //---------------------------//
                
                Thread.Sleep(50);
                this.Hide();
                this.nicon.Visible = true;
            }
            else
            {
                nicon_Click(this.nicon, new EventArgs());
                WindwosHook.WindowsHook_Mouse.GlobalMouseDown -= new EventHandler<WindwosHook.WindowsHook_Mouse.MouseEventArgs>(MouseHook_GlobalMouseDown);
                WindwosHook.KeyboardHook.GlobalKeyDown -= new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(KeyboardHook_Keyboard);               
            }
        }
        private void hotkey3_OnHotkey(object sender, HotKeyEventArgs e) //Ctral + F3(record)
        {
            isWorkingH3 = !isWorkingH3; //按一次=true開始(+= new EventHandler),第二次=false關掉(-= new EventHandler)
            if (isWorkingH3 && ClickPriotity.Count!=0)
            {            
                MessageBox.Show("開始鍵盤精靈!\r\n本程式自動縮小。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);               
                //---------------------------//
                Thread.Sleep(50);
                this.Hide();
                this.nicon.Visible = true;

                simulateRecord();
            }
            else
            {
                nicon_Click(this.nicon, new EventArgs());
            }
            nicon_Click(this.nicon, new EventArgs());
            MessageBox.Show("完成!!!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
        }
        
        private void MouseHook_GlobalMouseDown(object sender, WindwosHook.WindowsHook_Mouse.MouseEventArgs e)
        {
            clickTimes++;
            Win32Native.Methods.GetCursorPos(out pt); //直接紀錄pt即可，因winform的dpi scale難以轉換成window的正確scale
            ClickPriotity.Add(e.Button); 
            ClickPriotity.Add(pt);
            label5.Text = "第:" + clickTimes.ToString() + "次點擊, " + "x = " + pt.X.ToString() + ", y = " + pt.Y.ToString();
        }
        private void KeyboardHook_GlobalKeyDown(object sender, WindwosHook.KeyboardHook.KeyEventArgs e)
        {
            ClickPriotity.Add(e.Keys);
        }

        private void simulateRecord()
        {       
            for (int i = 0; i < ClickPriotity.Count - 2; ) //取消(clrl+F2)會再算2次keydown,故減2
            {
                Thread.Sleep(1000);
                if (Object.ReferenceEquals(ClickPriotity[i + 1].GetType(), pt.GetType()))
                {
                    Cursor.Position = (Point)ClickPriotity[i+1];
                    Thread.Sleep(200);
                    //textBox1.Text += "Mouse!" + "\r\n";
                    if (WindwosHook.Buttons.Left.ToString() == ClickPriotity[i].ToString())
                        SimulateControl.MouseControl.LeftClick();
                    else if (WindwosHook.Buttons.Right.ToString() == ClickPriotity[i].ToString())
                        SimulateControl.MouseControl.RightClick();
                    i += 2;
                }
                else
                {
                    SimulateControl.KeyboardControl.KeyDown((Keys)ClickPriotity[i]);
                    Thread.Sleep(30);
                    SimulateControl.KeyboardControl.KeyUp((Keys)ClickPriotity[i]);
                    textBox1.Text += "keyboard!" + "\r\n";
                    i++;
                }

            }
        }

        #region Simulation test
        private void button1_Click(object sender, EventArgs e) //move & click
        {
            int x = Int32.Parse(tX1.Text); //直接紀錄pt即可，因winform的dpi scale難以轉換成window的正確scale
            int y = Int32.Parse(tY1.Text);

            label5.Text = x.ToString();
            this.WindowState = FormWindowState.Minimized;
            Thread.Sleep(80);            
            Win32Native.Methods.SetCursorPos(x, y);
            Thread.Sleep(80);
            SimulateControl.MouseControl.DoubleLeftClick();
        }
        private void button5_Click(object sender, EventArgs e) //drag
        {
            this.WindowState = FormWindowState.Minimized;
            Thread.Sleep(1000);
            SimulateControl.MouseControl.DragTo(567, 449, 786, 28);
        }
        private void button6_Click(object sender, EventArgs e) //模擬KEY IN
        {
            Thread.Sleep(80);
            Win32Native.Methods.SetCursorPos(180, 420);
            Thread.Sleep(80);
            SimulateControl.MouseControl.LeftClick();
            Thread.Sleep(80);
            SimulateControl.KeyboardControl.KeyDown(Keys.A);
            SimulateControl.KeyboardControl.KeyUp(Keys.A);
        }
        #endregion

        private void button7_Click(object sender, EventArgs e) //秀Click priority 紀錄
        {
            textBox1.Text = "";
            for (int i = 0; i < ClickPriotity.Count-2; i++) //取消(clrl+F2)會再算2次keydown,故減2
            {
                textBox1.Text += ClickPriotity[i].ToString() + "\r\n";
            }
            label5.Text = ClickPriotity.Count.ToString();
        }



        private void button3_Click(object sender, EventArgs e) //ScreenShot
        {
            Bitmap myImage = new Bitmap(1920, 1080);
            Graphics g = Graphics.FromImage(myImage);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(1920, 1080));
            //IntPtr dc1 = g.GetHdc(); //這兩行不需要?
            //g.ReleaseHdc(dc1); //這兩行不需要?
            this.pictureBox1.Image = myImage;
            //myImage.Save(@"C:\Users\shen\Desktop\a1.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            using (Graphics graphics = this.CreateGraphics())
            {
                //label1.Text = ((Screen.PrimaryScreen.Bounds.Width * (int)graphics.DpiX) / 96).ToString(); //can't get the exactly screen size
                //label2.Text = Screen.PrimaryScreen.Bounds.Height.ToString();
            }
        }

        private void button8_Click(object sender, EventArgs e) //Macro record
        {
            for (int i = 0; i < ClickPriotity.Count - 2; ) //取消(clrl+F2)會再算2次keydown,故減2
            {
                if (Object.ReferenceEquals(ClickPriotity[i + 1].GetType(), pt.GetType()))
                {
                    Cursor.Position = (Point)ClickPriotity[1];
                    textBox1.Text += "Mouse!" + "\r\n";
                    if (WindwosHook.Buttons.Left.ToString() == ClickPriotity[0].ToString())
                        textBox1.Text += "left click!" + "\r\n"; //simulate left click
                    else if (WindwosHook.Buttons.Right.ToString() == ClickPriotity[0].ToString()) 
                        textBox1.Text += "right click!" + "\r\n";//simulate right click
                    i += 2;
                }
                else
                {
                    SimulateControl.KeyboardControl.KeyDown((Keys)ClickPriotity[0]);
                    SimulateControl.KeyboardControl.KeyUp((Keys)ClickPriotity[0]);
                    textBox1.Text += "keyboard!" + "\r\n";
                    i++;
                }

            }
        }

        

        

        


        
    }


    static public class Mouse
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern Int32 SendInput(Int32 cInputs, ref INPUT pInputs, Int32 cbSize);

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 28)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public INPUTTYPE dwType;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public KEYBOARDINPUT ki;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MOUSEINPUT
        {
            public Int32 dx;
            public Int32 dy;
            public Int32 mouseData;
            public MOUSEFLAG dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct KEYBOARDINPUT
        {
            public Int16 wVk;
            public Int16 wScan;
            public KEYBOARDFLAG dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HARDWAREINPUT
        {
            public Int32 uMsg;
            public Int16 wParamL;
            public Int16 wParamH;
        }

        public enum INPUTTYPE : int
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags()]
        public enum MOUSEFLAG : int
        {
            MOVE = 0x1,
            LEFTDOWN = 0x2,
            LEFTUP = 0x4,
            RIGHTDOWN = 0x8,
            RIGHTUP = 0x10,
            MIDDLEDOWN = 0x20,
            MIDDLEUP = 0x40,
            XDOWN = 0x80,
            XUP = 0x100,
            VIRTUALDESK = 0x400,
            WHEEL = 0x800,
            ABSOLUTE = 0x8000
        }

        [Flags()]
        public enum KEYBOARDFLAG : int
        {
            EXTENDEDKEY = 1,
            KEYUP = 2,
            UNICODE = 4,
            SCANCODE = 8
        }

        static public void LeftDown()
        {
            INPUT leftdown = new INPUT();

            leftdown.dwType = 0;
            leftdown.mi = new MOUSEINPUT();
            leftdown.mi.dwExtraInfo = IntPtr.Zero;
            leftdown.mi.dx = 0;
            leftdown.mi.dy = 0;
            leftdown.mi.time = 0;
            leftdown.mi.mouseData = 0;
            leftdown.mi.dwFlags = MOUSEFLAG.LEFTDOWN;

            SendInput(1, ref leftdown, Marshal.SizeOf(typeof(INPUT)));
        }

        static public void RightDown()
        {
            INPUT rightdown = new INPUT();

            rightdown.dwType = 0;
            rightdown.mi = new MOUSEINPUT();
            rightdown.mi.dwExtraInfo = IntPtr.Zero;
            rightdown.mi.dx = 0;
            rightdown.mi.dy = 0;
            rightdown.mi.time = 0;
            rightdown.mi.mouseData = 0;
            rightdown.mi.dwFlags = MOUSEFLAG.RIGHTDOWN;

            SendInput(1, ref rightdown, Marshal.SizeOf(typeof(INPUT)));
        }

        static public void LeftUp()
        {
            INPUT leftup = new INPUT();

            leftup.dwType = 0;
            leftup.mi = new MOUSEINPUT();
            leftup.mi.dwExtraInfo = IntPtr.Zero;
            leftup.mi.dx = 0;
            leftup.mi.dy = 0;
            leftup.mi.time = 0;
            leftup.mi.mouseData = 0;
            leftup.mi.dwFlags = MOUSEFLAG.LEFTUP;

            SendInput(1, ref leftup, Marshal.SizeOf(typeof(INPUT)));
        }

        static public void RightUp()
        {
            INPUT rightup = new INPUT();

            rightup.dwType = 0;
            rightup.mi = new MOUSEINPUT();
            rightup.mi.dwExtraInfo = IntPtr.Zero;
            rightup.mi.dx = 0;
            rightup.mi.dy = 0;
            rightup.mi.time = 0;
            rightup.mi.mouseData = 0;
            rightup.mi.dwFlags = MOUSEFLAG.RIGHTUP;

            SendInput(1, ref rightup, Marshal.SizeOf(typeof(INPUT)));
        }

        static public void LeftClick()
        {
            LeftDown();
            Thread.Sleep(20);
            LeftUp();
        }

        static public void RightClick()
        {
            RightDown();
            Thread.Sleep(20);
            RightUp();
        }


        static public void LeftDoubleClick()
        {
            LeftClick();
            Thread.Sleep(50);
            LeftClick();
        }

        static public void DragTo(string sor_X, string sor_Y, string des_X, string des_Y)
        {
            MoveTo(sor_X, sor_Y);
            LeftDown();
            Thread.Sleep(200);
            MoveTo(des_X, des_Y);
            LeftUp();
        }

        static public void MoveTo(string tx, string ty)
        {
            int x, y;
            int.TryParse(tx, out x);
            int.TryParse(ty, out y);

            Cursor.Position = new Point(x, y);
        }


    }
}

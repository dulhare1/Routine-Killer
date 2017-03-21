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

namespace RoutineKiller魯蛇殺手
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        GlobalHotKey hotkey1, hotkey2;

        float screenFactor;
        Point CursorPos = new Point();
        List<object> ClickPriotity = new List<object>();

        //NotifyIcon nicon = new NotifyIcon();
        bool isWorking = false;
        bool isWorkingH3 = false;
        private void Form1_Load(object sender, EventArgs e)
        {
            screenFactor = ScreenAndMouse.ScreenAndMouseDefault.getScalingFactor();

            hotkey1 = new GlobalHotKey(this.Handle, Keys.F1, Keys.Control); //註冊Control+F1為熱鍵(record)
            hotkey1.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey1_OnHotkey);
            
            hotkey2 = new GlobalHotKey(this.Handle, Keys.F2, Keys.Control);//註冊Control+F3為熱鍵(play)
            hotkey2.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey2_OnHotkey);

            
            //nicon.Icon = new Icon("Snake.ico");
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

        private void hotkey1_OnHotkey(object sender, HotKeyEventArgs e) //Ctral + F2(record)
        {
            isWorking = !isWorking; //按一次=true開始(+= new EventHandler),第二次=false關掉(-= new EventHandler)
            if (isWorking)
            {
                ClickPriotity.RemoveAll(it => true);
                MessageBox.Show("開始錄製動作!\r\n本程式自動縮小。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //--開始監聽keyboard & mouse
                startMouseHook();
                startKeyboardHook();
                //---------------------------//
                Thread.Sleep(50);
                this.Hide();
                this.nicon.Visible = true;
                this.nicon.ShowBalloonTip(5000);
            }
            else
            {
                nicon_Click(this.nicon, new EventArgs());
                WindwosHook.MouseHook.GlobalMouseDown -= new EventHandler<WindwosHook.MouseHook.MouseEventArgs>(mouseHook_GlobalMouseDown);
                WindwosHook.KeyboardHook.GlobalKeyDown -= new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(keyboardHook_GlobalKeyDown);
            }
        }

        private void startMouseHook()
        {
            WindwosHook.MouseHook.Enabled = true;
            WindwosHook.MouseHook.GlobalMouseDown += new EventHandler<WindwosHook.MouseHook.MouseEventArgs>(mouseHook_GlobalMouseDown);
        }
        private void startKeyboardHook()
        {
            WindwosHook.KeyboardHook.Enabled = true;
            WindwosHook.KeyboardHook.GlobalKeyDown += new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(keyboardHook_GlobalKeyDown);            
        }

        private void mouseHook_GlobalMouseDown(object sender, WindwosHook.MouseHook.MouseEventArgs e)
        {
            Win32Native.Methods.GetCursorPos(out CursorPos); //直接紀錄pt即可，因winform的dpi scale難以轉換成window的正確scale
            ClickPriotity.Add(e.Button);
            ClickPriotity.Add(CursorPos);
            //textBox1.Text += e.Button + ", x=" + e.X.ToString() + ", y=" + e.Y.ToString() + "\r\n";
        }     
        void keyboardHook_GlobalKeyDown(object sender, WindwosHook.KeyboardHook.KeyEventArgs e)
        {
            ClickPriotity.Add(e.Keys);
            //textBox1.Text += e.Keys + e.GetType().ToString() + "\r\n";
        }


        private void hotkey2_OnHotkey(object sender, HotKeyEventArgs e) //Ctral + F2(run)
        {
            if (textBox1.TextLength == 0) 
            {
                MessageBox.Show("請輸入執行間隔時間!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (ClickPriotity.Count == 0)
            {
                MessageBox.Show("請先錄製動作!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            isWorkingH3 = !isWorkingH3; //按一次=true開始(+= new EventHandler),第二次=false關掉(-= new EventHandler)
            if (isWorkingH3 && ClickPriotity.Count != 0)
            {
                MessageBox.Show("開始鍵盤精靈!\r\n本程式自動縮小。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //---------------------------//
                Thread.Sleep(50);
                this.Hide();
                this.nicon.Visible = true;

                simulateRecord();
                MessageBox.Show("完成!!!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                nicon_Click(this.nicon, new EventArgs());
            }
            nicon_Click(this.nicon, new EventArgs());
            
        }

        private void simulateRecord()
        {
            
            for (int i = 0; i < ClickPriotity.Count - 2; ) //取消(clrl+F2)會多算2次keydown,故減2
            {
                Thread.Sleep(1000);
                if (Object.ReferenceEquals(ClickPriotity[i + 1].GetType(), CursorPos.GetType()))
                {
                    Cursor.Position = (Point)ClickPriotity[i + 1];
                    Thread.Sleep(200);
                    //textBox1.Text += "Mouse!" + "\r\n";
                    if (WindwosHook.MouseButtons.Left.ToString() == ClickPriotity[i].ToString())
                        SimulateControl.MouseControl.LeftClick();
                    else if (WindwosHook.MouseButtons.Right.ToString() == ClickPriotity[i].ToString())
                        SimulateControl.MouseControl.RightClick();
                    i += 2;
                }
                else
                {
                    SimulateControl.KeyboardControl.KeyDown((Keys)ClickPriotity[i]);
                    Thread.Sleep(30);
                    SimulateControl.KeyboardControl.KeyUp((Keys)ClickPriotity[i]);
                    i++;
                }

            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)e.KeyChar < 48 | (int)e.KeyChar > 57 && (int)e.KeyChar!=8)
            { e.Handled = true; }
        }
    }
}

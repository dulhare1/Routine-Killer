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
        GlobalHotKey hotkey_Record, hotkey_Run, hotkey_Stop;

        float screenFactor;
        Point CursorPos = new Point();
        List<object> ClickPriotity = new List<object>();

        bool hotkey_Record_isWorking = false;
        bool hotkey_Run_isWorking = false;

        CancellationTokenSource cts_run = new CancellationTokenSource();
        
        private void Form1_Load(object sender, EventArgs e)
        {
            screenFactor = ScreenAndMouse.ScreenAndMouseDefault.getScalingFactor();

            hotkey_Stop = new GlobalHotKey(this.Handle, Keys.Escape,Keys.Alt);//註冊Alt+ESC為熱鍵(play)
            hotkey_Stop.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey_Stop_OnHotkey);
            
            hotkey_Record = new GlobalHotKey(this.Handle, Keys.F1, Keys.Alt); //註冊Alt+F1為熱鍵(record)
            hotkey_Record.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey_Record_OnHotkey);

            hotkey_Run = new GlobalHotKey(this.Handle, Keys.F2, Keys.Alt);//註冊Alt+F2為熱鍵(play)
            hotkey_Run.OnHotkey += new GlobalHotKey.HotkeyEventHandler(hotkey_Run_OnHotkey);
            
            nicon.Text = "QuickMacro";
            nicon.Click += new EventHandler(nicon_Click);
            
           
        }
        delegate void formCallback(Form1 form);
        private void showForm(Form1 form)
        {
            if (this.InvokeRequired)
            {
                formCallback d = new formCallback(showForm);
                this.Invoke(d, new object[] { form });
            }
            else { form.Show();  }
        }
        void nicon_Click(object sender, EventArgs e)
        {
            nicon.Visible = false;
            showForm(this);
           // Win32Native.Methods.SetForegroundWindow(this.Handle); intptr??
        }

        private void hotkey_Record_OnHotkey(object sender, HotKeyEventArgs e) //Alt + F1(record)
        { 
            if (hotkey_Run_isWorking) { return; }
            hotkey_Record_isWorking = !hotkey_Record_isWorking; //按一次=true開始(+= new EventHandler),第二次=false關掉(-= new EventHandler)
            if (hotkey_Record_isWorking==true)
            {
                ClickPriotity.RemoveAll(it => true);
                MessageBox.Show("開始錄製動作!\r\n本程式自動縮小。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                //--開始監聽keyboard & mouse
                startHook();
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

        private void startHook()
        {
            WindwosHook.MouseHook.Enabled = true;
            WindwosHook.MouseHook.GlobalMouseDown += new EventHandler<WindwosHook.MouseHook.MouseEventArgs>(mouseHook_GlobalMouseDown);

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


        private void hotkey_Run_OnHotkey(object sender, HotKeyEventArgs e) //Alt + F2(run)
        {
            trtBug();

            hotkey_Run_isWorking = !hotkey_Run_isWorking; //按一次=true開始(+= new EventHandler),第二次=false關掉(-= new EventHandler)         
            if (hotkey_Run_isWorking)
            {
                MessageBox.Show("開始鍵盤精靈!\r\n本程式自動縮小。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //---------------------------//
                cts_run = new CancellationTokenSource();
                Task tRun = Task.Factory.StartNew(simulateRecord, cts_run.Token);
                Thread.Sleep(50);
                this.Hide();
                this.nicon.Visible = true;          
            }
            else
            {
                cts_run.Cancel();
                MessageBox.Show("終止!\r\n請重新開始!!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                nicon_Click(this.nicon, new EventArgs());
            }
        }

        private void simulateRecord()
        {
            int sleepTime = Int16.Parse(textBox1.Text);
            int times = Int16.Parse(textBox2.Text);
            while (times != 0)
            {
                for (int i = 0; i < ClickPriotity.Count - 2; ) //錄製結束時(Alt+F1)會多2次keydown,故減2
                {
                    Thread.Sleep(sleepTime);

                    if (cts_run.IsCancellationRequested) 
                        return;

                    if (Object.ReferenceEquals(ClickPriotity[i + 1].GetType(), CursorPos.GetType()))
                    {
                        Cursor.Position = (Point)ClickPriotity[i + 1];
                        Thread.Sleep(80);
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
                times--;
            }
            hotkey_Run_isWorking = false;
            MessageBox.Show("完成!!!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            nicon_Click(this.nicon, new EventArgs());
        }

        private void hotkey_Stop_OnHotkey(object sender, HotKeyEventArgs e)
        {
            if (hotkey_Record_isWorking)
            {
                hotkey_Record_isWorking = false;
                MessageBox.Show("終止!\r\n請重新開使!!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                nicon_Click(this.nicon, new EventArgs());
                WindwosHook.MouseHook.GlobalMouseDown -= new EventHandler<WindwosHook.MouseHook.MouseEventArgs>(mouseHook_GlobalMouseDown);
                WindwosHook.KeyboardHook.GlobalKeyDown -= new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(keyboardHook_GlobalKeyDown);
                ClickPriotity.RemoveAll(it => true);
                return;
            }
            else if (hotkey_Run_isWorking)
            {
                cts_run.Cancel();
                hotkey_Run_isWorking = false;
                MessageBox.Show("終止!\r\n請重新開始!!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                nicon_Click(this.nicon, new EventArgs());
            }
        }

        private void trtBug()
        {
            if (hotkey_Record_isWorking)
            {
                hotkey_Record_isWorking = false;
                MessageBox.Show("尚未終止錄製!\r\n請重新開啟程式錄製!!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                nicon_Click(this.nicon, new EventArgs());
                WindwosHook.MouseHook.GlobalMouseDown -= new EventHandler<WindwosHook.MouseHook.MouseEventArgs>(mouseHook_GlobalMouseDown);
                WindwosHook.KeyboardHook.GlobalKeyDown -= new EventHandler<WindwosHook.KeyboardHook.KeyEventArgs>(keyboardHook_GlobalKeyDown);
                ClickPriotity.RemoveAll(it => true);
                return;
            }
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
            if (textBox2.TextLength == 0)
            {
                MessageBox.Show("請輸入重複次數!", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)e.KeyChar < 48 | (int)e.KeyChar > 57 && (int)e.KeyChar!=8)
            { e.Handled = true; }
        }
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)e.KeyChar < 48 | (int)e.KeyChar > 57 && (int)e.KeyChar != 8)
            { e.Handled = true; }
        }

    }
}

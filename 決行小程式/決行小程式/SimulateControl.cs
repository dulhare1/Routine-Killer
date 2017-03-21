using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace SimulateControl
{
    public static class MouseControl
    {
        public static void LeftClick()
        {
            Win32Native.Methods.mouse_event((uint)Win32Native.Structures.MOUSEFLAG.LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(20);
            Win32Native.Methods.mouse_event((uint)Win32Native.Structures.MOUSEFLAG.LEFTUP, 0, 0, 0, 0);
        }
        public static void RightClick()
        {
            Win32Native.Methods.mouse_event((uint)Win32Native.Structures.MOUSEFLAG.RIGHTDOWN, 0, 0, 0, 0);
            Thread.Sleep(20);
            Win32Native.Methods.mouse_event((uint)Win32Native.Structures.MOUSEFLAG.RIGHTUP, 0, 0, 0, 0);
        }
        public static void DoubleLeftClick()
        {
            LeftClick();
            Thread.Sleep(20);
            LeftClick();
        }

        /// <summary>
        /// 點選並拖曳滑鼠
        /// </summary>
        /// <param name="sor_X">起始X座標</param>
        /// <param name="sor_Y">起始Y座標</param>
        /// <param name="des_X">目標X座標</param>
        /// <param name="des_Y">目標Y座標</param>
        static public void DragTo(int sor_X, int sor_Y, int des_X, int des_Y)
        {
            Win32Native.Methods.SetCursorPos(sor_X, sor_Y);
            Win32Native.Methods.mouse_event((uint)Win32Native.Structures.MOUSEFLAG.LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(200);
            Win32Native.Methods.SetCursorPos(des_X, des_Y);
            Win32Native.Methods.mouse_event((uint)Win32Native.Structures.MOUSEFLAG.LEFTUP, 0, 0, 0, 0);
        }


    }


    public static class KeyboardControl
    {
        public static void KeyDown(System.Windows.Forms.Keys key)
        {
            Win32Native.Methods.keybd_event((byte)key, 0, 0, 0);
        }
        public static void KeyUp(System.Windows.Forms.Keys key)
        {
            Win32Native.Methods.keybd_event((byte)key, 0, 0x7F, 0);
        }
 
    }
}

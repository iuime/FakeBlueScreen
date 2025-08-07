using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace 模拟蓝屏
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Cursor.Hide();
            #if DEBUG == false
            //Console.WriteLine("禁用");
            Hook_Start();
            #endif
            Thread trd = new Thread(events)
            {
                IsBackground = true
            };
            trd.Start();
        }

        private void events()
        {
            Thread.Sleep(20000);
            Hook_Clear();
            Process.GetCurrentProcess().CloseMainWindow();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            #if DEBUG == false
            //Console.WriteLine("删除");
            //deleteSelf();
            #endif
        }

        private void deleteSelf()
        {
            string file = Process.GetCurrentProcess().MainModule.FileName;
            string bat = "@echo off\n:again\ndel %1\nif exist %1 goto again\ndel %0";
            File.WriteAllText("0.bat", bat);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "0.bat";
            psi.Arguments = file;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);
        }


        #region 屏蔽WIN功能键
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        private static int hHook = 0;
        public const int WH_KEYBOARD_LL = 13;

        //LowLevel键盘截获，如果是WH_KEYBOARD＝2，并不能对系统键盘截取，会在你截取之前获得键盘。 
        private static HookProc KeyBoardHookProcedure;
        //键盘Hook结构函数 
        [StructLayout(LayoutKind.Sequential)]
        public class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        //设置钩子 
        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        //抽掉钩子 
        public static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll")]
        //调用下一个钩子 
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);

        public static void Hook_Start()
        {
            // 安装键盘钩子 
            if (hHook == 0)
            {
                KeyBoardHookProcedure = new HookProc(KeyBoardHookProc);
                hHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyBoardHookProcedure,
                        GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                //如果设置钩子失败. 
                if (hHook == 0)
                {
                    Hook_Clear();
                }
            }
        }

        //取消钩子事件 
        public static void Hook_Clear()
        {
            bool retKeyboard = true;
            if (hHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
            //如果去掉钩子失败. 
            if (!retKeyboard) throw new Exception("UnhookWindowsHookEx failed.");
        }

        //这里可以添加自己想要的信息处理 
        private static int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));

                if (kbh.vkCode == 91) // 截获左win(开始菜单键) 
                {
                    return 1;
                }

                if (kbh.vkCode == 92)// 截获右win 
                {
                    return 1;
                }

                if (kbh.vkCode == (int)Keys.Escape && (int)Control.ModifierKeys == (int)Keys.Control) //截获Ctrl+Esc 
                {
                    return 1;
                }

                if (kbh.vkCode == (int)Keys.F4 && (int)Control.ModifierKeys == (int)Keys.Alt) //截获alt+f4 
                {
                    return 1;
                }

                if (kbh.vkCode == (int)Keys.Tab && (int)Control.ModifierKeys == (int)Keys.Alt) //截获alt+tab
                {
                    return 1;
                }

                if (kbh.vkCode == (int)Keys.Escape && (int)Control.ModifierKeys == (int)Keys.Control + (int)Keys.Shift) //截获Ctrl+Shift+Esc
                {
                    return 1;
                }

                if (kbh.vkCode == (int)Keys.Space && (int)Control.ModifierKeys == (int)Keys.Alt) //截获alt+空格 
                {
                    return 1;
                }

                if (kbh.vkCode == 241) //截获F1 
                {
                    return 1;
                }

                if ((int)Control.ModifierKeys == (int)Keys.Control + (int)Keys.Alt + (int)Keys.Delete)      //截获Ctrl+Alt+Delete 
                {
                    return 1;
                }

                if ((int)Control.ModifierKeys == (int)Keys.Control + (int)Keys.Shift) //截获Ctrl+Shift 
                {
                    return 1;
                }

                if (kbh.vkCode == (int)Keys.Space && (int)Control.ModifierKeys == (int)Keys.Control + (int)Keys.Alt) //截获Ctrl+Alt+空格 
                {
                    return 1;
                }
            }

            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }
        public static void TaskMgrLocking(bool bLock)
        {
            if (bLock)
            {
                try
                {
                    RegistryKey r = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", true);
                    r.SetValue("DisableTaskmgr", "1"); //屏蔽任务管理器 
                }
                catch
                {
                    RegistryKey r = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
                    r.SetValue("DisableTaskmgr", "0");
                }
            }
            else
            {
                Registry.CurrentUser.DeleteSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
            }
        }
        #endregion

        //  调用，这个需要放在主窗体的load方法中
        //private void Load(object sender, EventArgs e)
        //{
        //    Hook_Start();
        //}

        //卸载
        /*private void FormClosing(object sender, FormClosingEventArgs e)
        {
            Hook_Clear();
        }*/
    }
}

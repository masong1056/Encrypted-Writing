using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace CaesarCipherApp
{
    public partial class MainWindow : Window
    {
        private KeyboardHook keyboardHook;
        private HotKeyManager? hotKeyManager;
        private bool isEnabled = false;
        private bool isDarkMode = false;

        public MainWindow()
        {
            InitializeComponent();
            keyboardHook = new KeyboardHook();

            // Apply theme based on Windows settings
            ApplyWindowsTheme();

            // Check if running as admin
            if (!IsAdministrator())
            {
                WarningPanel.Visibility = Visibility.Visible;
            }

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            // Listen for Windows theme changes
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                Dispatcher.Invoke(() => ApplyWindowsTheme());
            }
        }

        private void ApplyWindowsTheme()
        {
            isDarkMode = IsWindowsDarkMode();

            if (isDarkMode)
            {
                // Dark mode colors
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                TitleText.Foreground = new SolidColorBrush(Colors.White);
                ToggleBorder.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                EnableText.Foreground = new SolidColorBrush(Colors.White);
                ShortcutBorder.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                ShortcutText.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            }
            else
            {
                // Light mode colors
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                TitleText.Foreground = new SolidColorBrush(Colors.Black);
                ToggleBorder.Background = new SolidColorBrush(Colors.White);
                EnableText.Foreground = new SolidColorBrush(Colors.Black);
                ShortcutBorder.Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                ShortcutText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            }
        }

        private bool IsWindowsDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value is int i && i == 0;
                }
            }
            catch
            {
                return false; // Default to light mode if we can't read the registry
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Register global hotkey: Ctrl+Shift+Alt+C
            hotKeyManager = new HotKeyManager(this);
            hotKeyManager.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt, Key.C, () =>
            {
                EnableToggle.IsChecked = !EnableToggle.IsChecked;
            });
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            keyboardHook?.Dispose();
            hotKeyManager?.Dispose();
        }

        private void EnableToggle_Changed(object sender, RoutedEventArgs e)
        {
            isEnabled = EnableToggle.IsChecked == true;

            if (isEnabled)
            {
                if (!IsAdministrator())
                {
                    MessageBox.Show("Please run this application as Administrator to enable encryption.",
                                    "Administrator Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EnableToggle.IsChecked = false;
                    return;
                }

                keyboardHook.Start();
                StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                StatusText.Text = "Active";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                keyboardHook.Stop();
                StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
                StatusText.Text = "Inactive";
                StatusText.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void OpenConverter_Click(object sender, RoutedEventArgs e)
        {
            var converter = new ConverterWindow();
            converter.Owner = this;
            converter.ShowDialog();
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }

    // Keyboard Hook Implementation
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int CAESAR_SHIFT = 7; // kept for reference
        private LowLevelKeyboardProc hookProc;
        private IntPtr hookId = IntPtr.Zero;

        // Define the keyboard hook structure
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        public KeyboardHook()
        {
            hookProc = HookCallback;
        }

        public void Start()
        {
            if (hookId == IntPtr.Zero)
            {
                hookId = SetHook(hookProc);
            }
        }

        public void Stop()
        {
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule? curModule = curProcess.MainModule)
                {
                    if (curModule != null)
                    {
                        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
            }
            return IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                int vkCode = hookStruct.vkCode;

                // Get modifier state
                bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
                bool altPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU (Alt)
                bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
                bool capsLock = (GetKeyState(0x14) & 0x0001) != 0; // VK_CAPITAL

                // Don't intercept if Ctrl, Alt is pressed
                if (!ctrlPressed && !altPressed)
                {
                    // Check if it's a letter key (A-Z)
                    if (vkCode >= 0x41 && vkCode <= 0x5A)
                    {
                        // Determine case for output: XOR: shift ^ caps
                        bool isUpper = shiftPressed ^ capsLock;

                        // Compute the cipher character directly and send as Unicode (robust across layouts)
                        int offset = vkCode - 0x41; // 0-25
                        int shifted = (offset + CAESAR_SHIFT) % 26;
                        char mapped = (char)('A' + shifted);
                        if (!isUpper) mapped = char.ToLower(mapped);

                        // Send the exact character using Unicode input to avoid VK/layout problems
                        SendUnicodeChar(mapped);
                        return (IntPtr)1; // Block the original key
                    }
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        // Send the character using SendInput with KEYEVENTF_UNICODE
        private void SendUnicodeChar(char ch)
        {
            const int INPUT_KEYBOARD = 1;
            const uint KEYEVENTF_UNICODE = 0x0004;
            const uint KEYEVENTF_KEYUP = 0x0002;

            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = 0;
            inputs[0].U.ki.wScan = (ushort)ch;
            inputs[0].U.ki.dwFlags = KEYEVENTF_UNICODE;
            inputs[0].U.ki.time = 0;
            inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = 0;
            inputs[1].U.ki.wScan = (ushort)ch;
            inputs[1].U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            inputs[1].U.ki.time = 0;
            inputs[1].U.ki.dwExtraInfo = IntPtr.Zero;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        public void Dispose()
        {
            Stop();
        }

        // P/Invoke declarations
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }

    // HotKey Manager
    public class HotKeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private int hotkeyId = 9000;
        private Window window;
        private HwndSource? hwndSource;
        private Action? callback;

        public HotKeyManager(Window window)
        {
            this.window = window;
            hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            hwndSource.AddHook(WndProc);
        }

        public void RegisterHotKey(ModifierKeys modifiers, Key key, Action callback)
        {
            this.callback = callback;
            uint mod = 0;

            if (modifiers.HasFlag(ModifierKeys.Control))
                mod |= 0x0002; // MOD_CONTROL
            if (modifiers.HasFlag(ModifierKeys.Shift))
                mod |= 0x0004; // MOD_SHIFT
            if (modifiers.HasFlag(ModifierKeys.Alt))
                mod |= 0x0001; // MOD_ALT

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            RegisterHotKey(new WindowInteropHelper(window).Handle, hotkeyId, mod, vk);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == hotkeyId)
            {
                callback?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotKey(new WindowInteropHelper(window).Handle, hotkeyId);
            hwndSource?.RemoveHook(WndProc);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
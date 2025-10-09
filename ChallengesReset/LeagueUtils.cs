using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ChallengesReset
{
    /// <summary>
    /// Tiện ích hỗ trợ dò tìm tiến trình League Client và thao tác với cửa sổ của nó.
    /// </summary>
    static class LeagueUtils
    {
        // Biểu thức chính quy dùng để tìm token & port trong CommandLine.
        private static readonly Regex AUTH_TOKEN_REGEX = new Regex("--remoting-auth-token=([^\"]+)");
        private static readonly Regex PORT_REGEX = new Regex("--app-port=([0-9]+)");

        /// <summary>
        /// Quét toàn bộ tiến trình, tìm tiến trình LeagueClient thật sự.
        /// Trả về tuple gồm: (Process, AuthToken, Port)
        /// Nếu không tìm thấy, trả về null.
        /// </summary>
        public static Tuple<Process, string, string> GetLeagueStatus()
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    // Dò CommandLine của tiến trình (qua WMI)
                    using (var mos = new ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                    using (var moc = mos.Get())
                    {
                        var commandLine = (string)moc.OfType<ManagementObject>().FirstOrDefault()?["CommandLine"];
                        if (string.IsNullOrEmpty(commandLine))
                            continue;

                        // Nếu tiến trình có chứa 2 tham số này => chính là League Client
                        if (commandLine.Contains("--app-port") && commandLine.Contains("--remoting-auth-token"))
                        {
                            var authMatch = AUTH_TOKEN_REGEX.Match(commandLine);
                            var portMatch = PORT_REGEX.Match(commandLine);

                            if (authMatch.Success && portMatch.Success)
                            {
                                return new Tuple<Process, string, string>(
                                    process,
                                    authMatch.Groups[1].Value,
                                    portMatch.Groups[1].Value
                                );
                            }
                        }
                    }
                }
                catch
                {
                    // Có thể gặp AccessDenied với 1 số tiến trình hệ thống → bỏ qua
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra xem cửa sổ chính của tiến trình có đang được focus không.
        /// </summary>
        public static bool IsWindowFocused(Process process)
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) return false;

            GetWindowThreadProcessId(handle, out var focusedPid);
            return focusedPid == process.Id;
        }

        /// <summary>
        /// Đưa cửa sổ chính của tiến trình ra trước màn hình (focus lại).
        /// </summary>
        public static void FocusWindow(Process process)
        {
            if (process == null || process.MainWindowHandle == IntPtr.Zero)
                return;

            SetForegroundWindow(process.MainWindowHandle);
        }

        // ====== native Win32 API ======
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ChallengesReset
{
    static class LeagueUtils
    {
        /// <summary>
        /// Quét toàn bộ tiến trình LeagueClient, lấy ra port và password từ lockfile.
        /// Trả về Tuple(Process, port, password)
        /// </summary>
        public static Tuple<Process, string, string> GetLeagueStatus()
        {
            try
            {
                Debug.WriteLine("========== [LeagueUtils] Bắt đầu dò client ==========");

                // 🔍 Tìm tiến trình LeagueClientUx hoặc LeagueClient
                Process process = Process.GetProcessesByName("LeagueClientUx").FirstOrDefault()
                               ?? Process.GetProcessesByName("LeagueClient").FirstOrDefault();

                if (process == null)
                {
                    Debug.WriteLine("[LeagueUtils] ❌ Không tìm thấy tiến trình LeagueClient.");
                    return null;
                }

                Debug.WriteLine($"[LeagueUtils] ✅ Tìm thấy tiến trình: {process.ProcessName} (PID {process.Id})");

                string installDir = null;

                // 🧭 Cố lấy đường dẫn từ MainModule (nếu chạy cùng kiến trúc)
                try
                {
                    installDir = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(installDir))
                        installDir = Path.GetDirectoryName(installDir);
                }
                catch
                {
                    Debug.WriteLine("[LeagueUtils] ⚠️ Không thể truy cập MainModule (khác kiến trúc 32/64-bit).");
                }

                // 🔎 Nếu không lấy được đường dẫn, quét các ổ đĩa để tìm lockfile
                if (string.IsNullOrEmpty(installDir))
                {
                    foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                    {
                        try
                        {
                            var files = Directory.GetFiles(drive.RootDirectory.FullName, "lockfile", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                installDir = Path.GetDirectoryName(files[0]);
                                Debug.WriteLine($"[LeagueUtils] 🔍 lockfile tìm thấy tại {installDir}");
                                break;
                            }
                        }
                        catch { /* Bỏ qua lỗi truy cập */ }
                    }
                }

                if (string.IsNullOrEmpty(installDir))
                {
                    Debug.WriteLine("[LeagueUtils] ❌ Không tìm được thư mục cài đặt client.");
                    return null;
                }

                Debug.WriteLine($"[LeagueUtils] 📁 installDir = {installDir}");

                string lockfilePath = Path.Combine(installDir, "lockfile");
                if (!File.Exists(lockfilePath))
                {
                    Debug.WriteLine("[LeagueUtils] ❌ Không tìm thấy lockfile.");
                    return null;
                }

                Debug.WriteLine($"[LeagueUtils] ✅ lockfile = {lockfilePath}");

                // 📖 Đọc lockfile an toàn kể cả khi bị khóa bởi tiến trình khác
                string content;
                using (var fs = new FileStream(lockfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    content = reader.ReadToEnd();
                }

                string[] parts = content.Split(':');
                if (parts.Length >= 5)
                {
                    string port = parts[2];
                    string password = parts[3];

                    Debug.WriteLine($"[LeagueUtils] 🔑 port={port}, token={password}");
                    return Tuple.Create(process, port, password);
                }

                Debug.WriteLine("[LeagueUtils] ⚠️ lockfile không hợp lệ hoặc thiếu dữ liệu.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LeagueUtils] ❌ Lỗi khi đọc lockfile: {ex}");
                return null;
            }
        }

        public static bool IsWindowFocused(Process process)
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) return false;

            GetWindowThreadProcessId(handle, out var focusedPid);
            return focusedPid == process.Id;
        }

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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ChallengesReset
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Kiểm tra xem có "mật lệnh" dọn dẹp được gửi đến không
            if (args.Length == 3 && args[0] == "--cleanup")
            {
                try
                {
                    int processId = int.Parse(args[1]);
                    string pathToClean = args[2];

                    // Chờ cho tiến trình LoveCat kết thúc
                    Process petProcess = Process.GetProcessById(processId);
                    petProcess.WaitForExit();

                    // Đợi thêm một chút để đảm bảo file không bị khóa
                    Thread.Sleep(1000);

                    // Dọn dẹp thư mục
                    if (Directory.Exists(pathToClean))
                    {
                        Directory.Delete(pathToClean, true);
                    }
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi nếu có sự cố (ví dụ: không tìm thấy process)
                }

                // Quan trọng: Kết thúc ngay tại đây, không chạy giao diện
                return;
            }

            // Nếu không có "mật lệnh", chạy ứng dụng với giao diện như bình thường
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChallengesResetForm());
        }
    }
}
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChallengesReset
{
    public partial class ChallengesResetForm : Form
    {
        LeagueConnection lc;

        public ChallengesResetForm()
        {
            InitializeComponent();
        }

        private void ChallengesResetForm_Load(object sender, EventArgs e)
        {
            lc = new LeagueConnection();
        }

        private async void resetButton_Click(object sender, EventArgs e)
        {
            if (!checkIfLeagueIsConnected()) return;

            SetControlsEnabled(false);
            progressBar.Visible = true;
            editMessageLabel("Đang gửi yêu cầu...", Color.Black);

            try
            {
                await lc.Post("/lol-challenges/v1/update-player-preferences/", "{\"challengeIds\": []}");
                editMessageLabel("Đặt lại Thử thách thành công!", Color.Green);
            }
            catch (Exception ex)
            {
                editMessageLabel($"Lỗi: {ex.Message}", Color.Red);
            }
            finally
            {
                await Task.Delay(2000);
                progressBar.Visible = false;
                editMessageLabel("", Color.Black);
                SetControlsEnabled(true);
            }
        }

        // petButton_Click
        private void petButton_Click(object sender, EventArgs e)
        {
            string resourceName = "ChallengesReset.Resources.LoveCat.zip";
            string subFolderName = "LoveCat";
            string exeNameInZip = "LoveCat.exe";

            string extractPath = Path.Combine(Path.GetTempPath(), "MyPetApp_Chill4Share");
            string exePath = Path.Combine(extractPath, subFolderName, exeNameInZip);

            SetControlsEnabled(false);
            editMessageLabel("Đang chuẩn bị khởi chạy My Cat...", Color.Black);

            try
            {
                if (!File.Exists(exePath))
                {
                    editMessageLabel("Giải nén tài nguyên lần đầu...", Color.Black);
                    var assembly = Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            MessageBox.Show(
                                "Phiên bản này không bao gồm gói My Cat.\n(Build nhẹ, không kèm LoveCat.zip)",
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information
                            );
                            return; // Dừng luôn, không giải nén nữa
                        }

                        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                        {
                            if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                            Directory.CreateDirectory(extractPath);
                            archive.ExtractToDirectory(extractPath);
                        }
                    }
                }

                editMessageLabel("Đang khởi chạy My Cat...", Color.Black);

                // Khởi chạy LoveCat.exe và lấy thông tin Process của nó
                Process petProcess = System.Diagnostics.Process.Start(exePath);

                // Khởi chạy "người dọn dẹp" (một bản sao ẩn của chính ChallengesReset.exe)
                // và truyền cho nó "mật lệnh" cùng thông tin cần thiết
                string selfPath = Application.ExecutablePath;
                string arguments = $"--cleanup {petProcess.Id} \"{extractPath}\"";

                ProcessStartInfo startInfo = new ProcessStartInfo(selfPath, arguments);
                startInfo.WindowStyle = ProcessWindowStyle.Hidden; // Chạy ẩn
                startInfo.CreateNoWindow = true;                   // Không tạo cửa sổ console
                System.Diagnostics.Process.Start(startInfo);

                Task.Delay(1000).ContinueWith(_ =>
                {
                    if (this.IsDisposed || this.Disposing) return;
                    if (messageLabel.InvokeRequired)
                    {
                        messageLabel.Invoke(new Action(() => editMessageLabel("", Color.Black)));
                    }
                    else
                    {
                        editMessageLabel("", Color.Black);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khởi chạy ứng dụng PET:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Challenges Reset Tool\nPhiên bản 1.0\nTạo bởi Chill4Share", "Thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---- Các hàm tiện ích ----

        private void editMessageLabel(string msg, Color color)
        {
            messageLabel.Text = msg;
            messageLabel.ForeColor = color;
        }

        private void SetControlsEnabled(bool enabled)
        {
            resetButton.Enabled = enabled;
            petButton.Enabled = enabled;
            aboutButton.Enabled = enabled;
        }

        private Boolean checkIfLeagueIsConnected()
        {
            if (!lc.IsConnected)
            {
                editMessageLabel("Chưa kết nối! Vui lòng đăng nhập vào game trước.", Color.Red);
                return false;
            }
            return true;
        }
    }
}
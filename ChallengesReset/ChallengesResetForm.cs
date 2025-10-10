using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

            editMessageLabel("🔍 Đang tìm client Liên Minh Huyền Thoại...", Color.DarkOrange);

            lc.OnConnected += () =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() =>
                    {
                        resetButton.Enabled = true;
                        editMessageLabel("✅ Đã kết nối với Liên Minh Huyền Thoại.", Color.Green);
                    }));
                else
                {
                    resetButton.Enabled = true;
                    editMessageLabel("✅ Đã kết nối với Liên Minh Huyền Thoại.", Color.Green);
                }
            };

            lc.OnDisconnected += () =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() =>
                    {
                        resetButton.Enabled = false;
                        editMessageLabel("❌ Mất kết nối. Vui lòng mở lại Liên Minh.", Color.Red);
                    }));
                else
                {
                    resetButton.Enabled = false;
                    editMessageLabel("❌ Mất kết nối. Vui lòng mở lại Liên Minh.", Color.Red);
                }
            };

            resetButton.Enabled = lc.IsConnected;
        }

        private async void resetButton_Click(object sender, EventArgs e)
        {
            if (!checkIfLeagueIsConnected()) return;

            SetControlsEnabled(false);
            progressBar.Visible = true;
            editMessageLabel("Đang gửi yêu cầu...", Color.Black);

            try
            {
                await Task.Run(async () =>
                {
                    await lc.Post("/lol-challenges/v1/update-player-preferences/", "{\"challengeIds\": []}");
                });

                editMessageLabel("Đặt lại Thử thách thành công!", Color.Green);
            }
            catch (Exception ex)
            {
                editMessageLabel($"Lỗi: {ex.Message}", Color.Red);
            }
            finally
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    if (IsDisposed || Disposing) return;
                    Invoke(new Action(() =>
                    {
                        progressBar.Visible = false;
                        editMessageLabel("", Color.Black);
                        SetControlsEnabled(true);
                    }));
                });
            }
        }

        private void petButton_Click(object sender, EventArgs e)
        {
            const string resourceName = "ChallengesReset.Resources.LoveCat.zip";
            const string subFolderName = "LoveCat";
            const string exeNameInZip = "LoveCat.exe";

            string extractPath = Path.Combine(Path.GetTempPath(), "MyPetApp_Chill4Share");
            string exePath = Path.Combine(extractPath, subFolderName, exeNameInZip);

            SetControlsEnabled(false);
            editMessageLabel("Đang chuẩn bị khởi chạy My Cat...", Color.Black);

            try
            {
                // 🔍 Kiểm tra xem bản build này có nhúng resource LoveCat.zip hay không
                var assembly = Assembly.GetExecutingAssembly();
                string[] availableResources = assembly.GetManifestResourceNames();
                bool hasLoveCat = availableResources.Contains(resourceName);

                if (!hasLoveCat)
                {
                    MessageBox.Show(
                        "Phiên bản này không bao gồm phần mở rộng My Cat.\n(Build nhẹ, không kèm LoveCat.zip)",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                    return;
                }

                // ✅ Có resource, kiểm tra xem đã giải nén chưa
                if (!File.Exists(exePath))
                {
                    editMessageLabel("Giải nén My Cat lần đầu...", Color.Black);

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        if (Directory.Exists(extractPath))
                            Directory.Delete(extractPath, true);
                        Directory.CreateDirectory(extractPath);
                        archive.ExtractToDirectory(extractPath);
                    }
                }

                // 🚀 Chạy LoveCat.exe
                editMessageLabel("Đang khởi chạy My Cat...", Color.Black);
                Process petProcess = Process.Start(exePath);

                // 🧹 Gọi lại ChallengesReset.exe với mật lệnh dọn dẹp sau khi LoveCat thoát
                string selfPath = Application.ExecutablePath;
                string arguments = $"--cleanup {petProcess.Id} \"{extractPath}\"";
                var startInfo = new ProcessStartInfo(selfPath, arguments)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);

                // 🕐 Làm sạch UI sau một lúc
                Task.Delay(1000).ContinueWith(_ =>
                {
                    if (IsDisposed || Disposing) return;
                    if (messageLabel.InvokeRequired)
                        messageLabel.Invoke(new Action(() => editMessageLabel("", Color.Black)));
                    else
                        editMessageLabel("", Color.Black);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khởi chạy My Cat:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Challenges Reset Tool\nPhiên bản 1.0\nTạo bởi Chill4Share",
                "Thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void editMessageLabel(string msg, Color color)
        {
            messageLabel.Text = msg;
            messageLabel.ForeColor = color;
        }

        private void SetControlsEnabled(bool enabled)
        {
            resetButton.Enabled = enabled && lc.IsConnected;
            petButton.Enabled = enabled;
            aboutButton.Enabled = enabled;
        }

        private bool checkIfLeagueIsConnected()
        {
            if (!lc.IsConnected)
            {
                editMessageLabel("❌ Chưa kết nối! Vui lòng đăng nhập vào game trước.", Color.Red);
                return false;
            }
            return true;
        }
    }
}

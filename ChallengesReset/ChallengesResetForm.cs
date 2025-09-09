using System;
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
            // Tạm thời hiển thị một thông báo.
            // Sau này sẽ thay thế nội dung của hàm này bằng logic giải nén và chạy file PET.
            MessageBox.Show("Chức năng mở ứng dụng PET sẽ được tích hợp ở đây.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            petButton.Enabled = enabled; // Đổi ở đây
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
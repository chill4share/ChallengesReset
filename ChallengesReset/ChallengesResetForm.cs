using System;
using System.Drawing;
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

        // Hàm này giờ có thêm async Task
        private async void resetButton_Click(object sender, EventArgs e)
        {
            if (!checkIfLeagueIsConnected())
            {
                return;
            }

            // Vô hiệu hóa các nút và hiện progress bar
            SetControlsEnabled(false);
            progressBar.Visible = true;
            editMessageLabel("Đang gửi yêu cầu...", Color.Black);

            try
            {
                // Gửi yêu cầu và chờ kết quả
                await lc.Post("/lol-challenges/v1/update-player-preferences/", "{\"challengeIds\": []}");
                editMessageLabel("Đặt lại Thử thách thành công!", Color.Green);
            }
            catch (Exception ex)
            {
                // Báo lỗi nếu có sự cố
                editMessageLabel($"Lỗi: {ex.Message}", Color.Red);
            }
            finally
            {
                // Dù thành công hay thất bại, cũng reset lại giao diện
                await Task.Delay(2000); // Chờ 2 giây để người dùng đọc thông báo
                progressBar.Visible = false;
                editMessageLabel("", Color.Black);
                SetControlsEnabled(true);
            }
        }

        private void myCatButton_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Trình duyệt sẽ mở trang GitHub của dự án MyCat.", "Mở liên kết", MessageBoxButtons.OK, MessageBoxIcon.Information);
            System.Diagnostics.Process.Start("https://github.com/chill4share/MyCat");
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            // Tạm thời hiển thị một MessageBox. 
            // Sau này có thể tạo một Form mới để hiển thị thông tin chi tiết.
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
            myCatButton.Enabled = enabled;
            aboutButton.Enabled = enabled;
        }

        private Boolean checkIfLeagueIsConnected()
        {
            if (!lc.IsConnected)
            {
                editMessageLabel("Chưa kết nối đến client Liên Minh!\nHãy đăng nhập và chờ vài giây.", Color.Red);
                return false;
            }
            return true;
        }
    }
}
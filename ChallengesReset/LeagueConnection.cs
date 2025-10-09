using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace ChallengesReset
{
    class LeagueConnection
    {
        private static HttpClient HTTP_CLIENT;

        private WebSocket socketConnection;
        private Tuple<Process, string, string> processInfo;
        private bool connected;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<OnWebsocketEventArgs> OnWebsocketEvent;

        public bool IsConnected => connected;

        public LeagueConnection()
        {
            try
            {
                HTTP_CLIENT = new HttpClient(new HttpClientHandler()
                {
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                    ServerCertificateCustomValidationCallback = (a, b, c, d) => true
                });
            }
            catch
            {
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                HTTP_CLIENT = new HttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (a, b, c, d) => true
                });
            }

            Task.Delay(2000).ContinueWith(e => TryConnectOrRetry());
        }

        public void ClearAllListeners()
        {
            OnWebsocketEvent = null;
        }

        private void TryConnect()
        {
            try
            {
                if (connected) return;

                var status = LeagueUtils.GetLeagueStatus();
                if (status == null)
                {
                    Debug.WriteLine("[LeagueConnection] ⏳ Đang tìm client...");
                    return;
                }

                // Tuple(Process, port, password)
                var port = status.Item2;
                var password = status.Item3;

                Debug.WriteLine($"[LeagueConnection] ✅ Tìm thấy client, port={port}, token={password}");

                var byteArray = Encoding.ASCII.GetBytes("riot:" + password);
                HTTP_CLIENT.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                string wsUrl = $"wss://127.0.0.1:{port}/";
                socketConnection = new WebSocket(wsUrl, "wamp");

                socketConnection.SetCredentials("riot", password, true);
                socketConnection.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
                socketConnection.SslConfiguration.ServerCertificateValidationCallback = (a, b, c, d) => true;

                socketConnection.OnMessage += HandleMessage;
                socketConnection.OnClose += HandleDisconnect;

                socketConnection.Connect();
                socketConnection.Send("[5,\"OnJsonApiEvent\"]");

                processInfo = status;
                connected = true;
                Debug.WriteLine("[LeagueConnection] 🔗 Kết nối thành công với LCU WebSocket.");
                OnConnected?.Invoke();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[LeagueConnection] ❌ Lỗi khi kết nối: {e.Message}");
                processInfo = null;
                connected = false;
            }
        }

        private void TryConnectOrRetry()
        {
            if (connected) return;
            TryConnect();
            Task.Delay(2000).ContinueWith(a => TryConnectOrRetry());
        }

        private void HandleDisconnect(object sender, CloseEventArgs args)
        {
            Debug.WriteLine("[LeagueConnection] ⚠️ Mất kết nối với LCU.");

            processInfo = null;
            connected = false;
            socketConnection = null;

            OnDisconnected?.Invoke();
            TryConnectOrRetry();
        }

        private void HandleMessage(object sender, MessageEventArgs args)
        {
            try
            {
                if (!args.IsText || string.IsNullOrWhiteSpace(args.Data))
                    return;

                // Bỏ qua nếu không phải chuỗi JSON mảng
                if (!args.Data.TrimStart().StartsWith("["))
                    return;

                var payload = SimpleJson.DeserializeObject<JsonArray>(args.Data);
                if (payload == null || payload.Count < 3)
                    return;

                if ((long)payload[0] != 8 || !((string)payload[1]).Equals("OnJsonApiEvent"))
                    return;

                dynamic ev = payload[2];
                if (ev == null) return;

                OnWebsocketEvent?.Invoke(new OnWebsocketEventArgs()
                {
                    Path = ev["uri"],
                    Type = ev["eventType"],
                    Data = ev["eventType"] == "Delete" ? null : ev["data"]
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LeagueConnection] ⚠️ Lỗi xử lý WebSocket message: {ex.Message}");
            }
        }

        public async Task<dynamic> Get(string url)
        {
            if (!connected)
                throw new InvalidOperationException("Not connected to LCU");

            var res = await HTTP_CLIENT.GetAsync("https://127.0.0.1:" + processInfo.Item2 + url);
            var stringContent = await res.Content.ReadAsStringAsync();

            if (res.StatusCode == HttpStatusCode.NotFound)
                return null;

            return SimpleJson.DeserializeObject(stringContent);
        }

        public async Task Post(string url, string body)
        {
            if (!connected)
                throw new InvalidOperationException("Not connected to LCU");

            await HTTP_CLIENT.PostAsync(
                "https://127.0.0.1:" + processInfo.Item2 + url,
                new StringContent(body, Encoding.UTF8, "application/json")
            );
        }

        public async void Observe(string url, Action<dynamic> handler)
        {
            OnWebsocketEvent += data =>
            {
                if (data.Path == url) handler(data.Data);
            };

            if (connected)
            {
                handler(await Get(url));
            }
            else
            {
                Action connectHandler = null;
                connectHandler = async () =>
                {
                    OnConnected -= connectHandler;
                    handler(await Get(url));
                };

                OnConnected += connectHandler;
            }
        }
    }

    public class OnWebsocketEventArgs : EventArgs
    {
        public string Path { get; set; }
        public string Type { get; set; }
        public dynamic Data { get; set; }
    }
}

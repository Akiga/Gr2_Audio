using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using System.IO.Compression;

namespace Gr2_Audio
{
    public partial class Form1 : Form
    {
        private const string HISTORY_FILE_PATH = "history.txt";
        private WaveIn waveIn;
        private WaveOut waveOut;
        private BufferedWaveProvider waveProvider;
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;
        private TcpListener videoServer;
        private TcpClient videoClient;
        private NetworkStream videoStream;
        private bool isServer = false;
        private bool isConnected = false;
        private bool isMicMuted = false;
        private List<string> connectionHistory = new List<string>();
        private CancellationTokenSource cancellationTokenSource;
        private VideoCaptureDevice videoDevice;
        private PictureBox videoBox;
        private PictureBox localVideoBox;
        private bool isVideoStreaming = false;
        private Button toggleVideoButton;
        private WaveFileWriter waveFileWriter;
        private string audioFilePath;
        private bool isRecording = false;
        private DateTime lastSent = DateTime.MinValue;
        private bool isProcessingFrame = false;

        public Form1()
        {
            InitializeComponent();
            connectionHistory = new List<string>();
            ShowIntroduction();
            LoadConnectionHistory();
            InitializeAudio();
        }
        
        private void SetupSuccessfulConnection()
        {
            isConnected = true;
            UpdateButtonStates(true);
            InitializeAudioDevices();
            StartReceivingAudio();
            StartReceivingVideo();
            waveIn?.StartRecording();
        }

        private void ShowIntroduction()
        {
            mainPanel.Controls.Clear();
            Label introLabel = new Label
            {
                Text = "Ứng dụng gọi Audio giữa Client và Server \n Nhóm 2",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Black,
            };
            mainPanel.Controls.Add(introLabel);
        }

        private void LoadConnectionHistory()
        {
            connectionHistory.Clear();

            try
            {
                if (File.Exists(HISTORY_FILE_PATH))
                {
                    var lines = File.ReadAllLines(HISTORY_FILE_PATH);
                    connectionHistory.AddRange(lines);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading connection history: {ex.Message}",
                    "Load History Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeAudio()
        {

            waveIn = new WaveIn();
            waveIn.WaveFormat = new WaveFormat(44100, 1);  
            waveIn.DataAvailable += WaveIn_DataAvailable; 
            waveIn.BufferMilliseconds = 50;
            audioFilePath = "audio_recording_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
            waveFileWriter = new WaveFileWriter(audioFilePath, waveIn.WaveFormat);
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                // Kiểm tra xem stream có thể ghi, microphone không bị mute và waveFileWriter còn tồn tại
                if (stream != null && stream.CanWrite && !isMicMuted && waveFileWriter != null)
                {
                    // Ghi âm thanh vào file
                    waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
                    stream.Write(e.Buffer, 0, e.BytesRecorded);
                }
            }
            catch (Exception ex)
            {
                var statusLabelArray = mainPanel.Controls.Find("statusLabel", false);
                if (statusLabelArray.Length > 0)
                {
                    var statusLabel = statusLabelArray[0] as Label;
                    statusLabel.Text = $"Status: Audio sending error - {ex.Message}";
                    statusLabel.ForeColor = Color.Red;
                }
                // Không hiện MessageBox nữa để tránh popup liên tục
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            MakeButtonRound(callButton);
            MakeButtonRound(historyButton);
            MakeButtonRound(exitButton);
        }

        private void MakeButtonRound(Button btn)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = Math.Min(btn.Width, btn.Height); // hình tròn

            path.AddEllipse(20, 0, radius, radius);
            btn.Region = new Region(path);
        }

        private void SaveConnectionHistory()
        {
            try
            {
                File.WriteAllLines(HISTORY_FILE_PATH, connectionHistory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving connection history: {ex.Message}",
                    "Save History Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetWifiIPv4Address()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return "No Wi-Fi IPv4 address found";
        }

        private void ShowCallInterface()
        {
            mainPanel.Controls.Clear();
            mainPanel.Size = new Size(888, 753);

            GroupBox modeGroup = new GroupBox
            {
                Text = "Connection Mode",
                Location = new Point(30, 20),
                Size = new Size(350, 60),
                ForeColor = Color.Black
            };
            RadioButton hostRadio = new RadioButton
            {
                Text = "Host Call",
                Location = new Point(20, 25),
                ForeColor = Color.Black,
                Checked = false
            };
            RadioButton clientRadio = new RadioButton
            {
                Text = "Join Call",
                Location = new Point(150, 25),
                ForeColor = Color.Black,
                Checked = true
            };
            modeGroup.Controls.AddRange(new Control[] { hostRadio, clientRadio });

            TextBox ipAddressTextBox = new TextBox
            {
                Location = new Point(30, 100),
                Name = "ipAddressTextBox",
                Size = new Size(350, 30),
                Font = new Font("Arial", 12),
                ForeColor = Color.Gray,
                Text = "Enter IP address to connect"
            };
            ipAddressTextBox.GotFocus += (s, e) =>
            {
                if (ipAddressTextBox.Text == "Enter IP address to connect")
                {
                    ipAddressTextBox.Text = "";
                    ipAddressTextBox.ForeColor = Color.Black;
                }
            };
            ipAddressTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(ipAddressTextBox.Text))
                {
                    ipAddressTextBox.Text = "Enter IP address to connect";
                    ipAddressTextBox.ForeColor = Color.Gray;
                }
            };

            TextBox portTextBox = new TextBox
            {
                Location = new Point(30, 140),
                Name = "portTextBox",
                Size = new Size(350, 30),
                Font = new Font("Arial", 12),
                ForeColor = Color.Gray,
                Text = "Enter port number"
            };
            portTextBox.GotFocus += (s, e) =>
            {
                if (portTextBox.Text == "Enter port number")
                {
                    portTextBox.Text = "";
                    portTextBox.ForeColor = Color.Black;
                }
            };
            mainPanel.Controls.Add(portTextBox);

            TextBox portVideoTextBox = new TextBox
            {
                Location = new Point(30, 180),
                Name = "portVideoTextBox",
                Size = new Size(350, 30),
                Font = new Font("Arial", 12),
                ForeColor = Color.Gray,
                Text = "Enter video port number"
            };
            portVideoTextBox.GotFocus += (s, e) =>
            {
                if (portVideoTextBox.Text == "Enter video port number")
                {
                    portVideoTextBox.Text = "";
                    portVideoTextBox.ForeColor = Color.Black;
                }
            };
            mainPanel.Controls.Add(portVideoTextBox);

            Button connectButton = new Button
            {
                Location = new Point(30, 230),
                Name = "connectButton",
                Size = new Size(120, 50),
                Text = "Connect",
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            Button endButton = new Button
            {
                Location = new Point(170, 230),
                Name = "endButton",
                Size = new Size(120, 50),
                Text = "End Call",
                BackColor = Color.IndianRed,
                Enabled = false,
                Font = new Font("Arial", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            Button muteButton = new Button
            {
                Location = new Point(310, 230),
                Name = "muteButton",
                Size = new Size(120, 50),
                Text = "Mute Mic",
                BackColor = Color.LightGray,
                Enabled = false,
                Font = new Font("Arial", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            Button recordButton = new Button
            {
                Location = new Point(30, 400),   // Vị trí của nút ghi âm
                Name = "recordButton",
                Size = new Size(120, 50),
                Text = "Start Recording",         // Text ban đầu khi chưa ghi âm
                BackColor = Color.LightBlue,
                Font = new Font("Arial", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            // Gắn sự kiện Click cho nút
            recordButton.Click += RecordButton_Click;

            // Thêm nút vào giao diện
            mainPanel.Controls.Add(recordButton);

            Label statusLabel = new Label
            {
                Location = new Point(30, 300),
                Name = "statusLabel",
                Size = new Size(400, 60),
                Text = "Status: Ready",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label volumeTitle = new Label
            {
                Location = new Point(30, 370),
                Text = "Volume:",
                Size = new Size(70, 30),
                Font = new Font("Arial", 12),
                ForeColor = Color.Black
            };

            TrackBar volumeBar = new TrackBar
            {
                Location = new Point(110, 370),
                Name = "volumeBar",
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 10
            };

            Label volumeLabel = new Label
            {
                Location = new Point(320, 370),
                Name = "volumeLabel",
                Size = new Size(50, 30),
                Text = "50%",
                Font = new Font("Arial", 12),
                ForeColor = Color.White
            };

            // Sự kiện cho các thành phần giao diện
            hostRadio.CheckedChanged += (s, e) =>
            {
                bool isChecked = hostRadio.Checked;
                isServer = isChecked;
                ipAddressTextBox.Enabled = !isChecked;

                var ipBox = mainPanel.Controls["ipAddressTextBox"] as TextBox;
                var portBox = mainPanel.Controls["portTextBox"] as TextBox;
                var portVideoBox = mainPanel.Controls["portVideoTextBox"] as TextBox;

                if (ipBox != null) ipBox.Visible = !isChecked;
                if (portBox != null) portBox.Visible = !isChecked;
                if (portVideoBox != null) portVideoBox.Visible = !isChecked;

                if (isChecked)
                {
                    ipAddressTextBox.Text = GetWifiIPv4Address();
                    ipAddressTextBox.ForeColor = Color.Gray;
                }

                connectButton.Text = isChecked ? "Start Hosting" : "Connect";
            };

            volumeBar.ValueChanged += (s, e) =>
            {
                volumeLabel.Text = $"{volumeBar.Value}%";
                if (waveOut != null)
                    waveOut.Volume = volumeBar.Value / 100f;
            };

            connectButton.Click += async (s, e) =>
            {
                connectButton.Enabled = false;
                try
                {
                    if (isServer)
                    {
                        await StartHosting();
                    }
                    else
                    {
                        if (!int.TryParse(portTextBox.Text, out int portAudio))
                        {
                            MessageBox.Show("Please enter a valid audio port number.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        if (!int.TryParse(portVideoTextBox.Text, out int portVideo))
                        {
                            MessageBox.Show("Please enter a valid video port number.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        await StartClient(ipAddressTextBox.Text, portAudio, portVideo);
                    }
                }
                finally
                {
                    connectButton.Enabled = true;
                }
            };

            endButton.Click += EndCall;
            muteButton.Click += MuteButton_Click;

            mainPanel.Controls.AddRange(new Control[] {
                modeGroup, ipAddressTextBox, connectButton, endButton,
                muteButton, statusLabel, volumeTitle, volumeBar, volumeLabel
            });

            videoBox = new PictureBox
            {
                Name = "videoBox",
                Location = new Point(450, 40),
                Size = new Size(400, 300),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            mainPanel.Controls.Add(videoBox);

            localVideoBox = new PictureBox
            {
                Name = "localVideoBox",
                Location = new Point(700, 360),
                Size = new Size(150, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            mainPanel.Controls.Add(localVideoBox);

            toggleVideoButton = new Button
            {
                Name = "toggleVideoButton",
                Text = "Start Video",
                Location = new Point(450, 360),
                Size = new Size(200, 50),
                BackColor = Color.LightBlue,
                Font = new Font("Arial", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            toggleVideoButton.Click += ToggleVideoButton_Click;
            mainPanel.Controls.Add(toggleVideoButton);
        }
        private void RecordButton_Click(object sender, EventArgs e)
        {
            Button recordButton = sender as Button;

            if (isRecording)
            {
                if (waveIn != null)
                {
                    waveIn.StopRecording();
                    waveIn.Dispose();
                    waveIn = null;
                }
                if (waveFileWriter != null)
                {
                    waveFileWriter.Dispose();
                    waveFileWriter = null;
                }

                recordButton.Text = "Start Recording";
                MessageBox.Show("Recording stopped and saved to: " + audioFilePath);
                isRecording = false;
            }
            else
            {
                audioFilePath = "audio_recording_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
                waveIn = new WaveIn();
                waveIn.WaveFormat = new WaveFormat(44100, 1);
                waveIn.DataAvailable += (s, args) =>
                {
                    // Chỉ ghi nếu writer đã được khởi tạo
                    if (waveFileWriter != null)
                        waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
                };
                waveFileWriter = new WaveFileWriter(audioFilePath, waveIn.WaveFormat);

                waveIn.StartRecording();
                recordButton.Text = "Stop Recording";
                isRecording = true;
            }
        }


        private async Task StartHosting()
        {
            try
            {
                UpdateStatus("Starting host...", Color.Yellow);
                server = new TcpListener(IPAddress.Any, 8000);
                videoServer = new TcpListener(IPAddress.Any, 8001);
                server.Start();
                videoServer.Start();
                UpdateStatus($"Waiting for connection on {GetWifiIPv4Address()}:8000", Color.Yellow);

                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
                {
                    client = await server.AcceptTcpClientAsync();
                    stream = client.GetStream();

                    videoClient = await videoServer.AcceptTcpClientAsync();
                    videoStream = videoClient.GetStream();

                    SetupSuccessfulConnection();
                    UpdateStatus("Client connected! Call in progress.", Color.Green);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hosting error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Hosting failed", Color.Red);
                ResetConnection();
            }
        }

        private async Task StartClient(string serverIP, int portAudio, int portVideo)
        {
            try
            {
                UpdateStatus("Connecting...", Color.Yellow);
                client = new TcpClient();
                await client.ConnectAsync(serverIP, portAudio);
                stream = client.GetStream();

                videoClient = new TcpClient();
                await videoClient.ConnectAsync(serverIP, portVideo);
                videoStream = videoClient.GetStream();

                SetupSuccessfulConnection();
                AddToConnectionHistory(serverIP);
                UpdateStatus("Connected! Call in progress.", Color.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Connection failed", Color.Red);
                ResetConnection();
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            this.Invoke((MethodInvoker)delegate
            {
                var statusLabel = mainPanel.Controls.Find("statusLabel", false)[0] as Label;
                statusLabel.Text = $"Status: {message}";
                statusLabel.ForeColor = color;
            });
        }

        private void MuteButton_Click(object sender, EventArgs e)
        {
            Button muteButton = sender as Button;
            isMicMuted = !isMicMuted;

            if (isMicMuted)
            {
                muteButton.Text = "Unmute Mic";
                muteButton.BackColor = Color.Orange;
                waveIn?.StopRecording();
            }
            else
            {
                muteButton.Text = "Mute Mic";
                muteButton.BackColor = Color.LightGray;
                waveIn?.StartRecording();
            }
        }

        private void InitializeAudioDevices()
        {
            try
            {
                waveOut = new WaveOut();
                waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1));
                waveOut.Init(waveProvider);
                waveOut.Play();

                var volumeBar = mainPanel.Controls.Find("volumeBar", false)[0] as TrackBar;
                waveOut.Volume = volumeBar.Value / 100f;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing audio devices: {ex.Message}",
                    "Audio Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartReceivingAudio()
        {
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                byte[] buffer = new byte[4096];
                while (isConnected && stream != null)
                {
                    try
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                            break;

                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                        if (bytesRead > 0)
                        {
                            waveProvider?.AddSamples(buffer, 0, bytesRead);
                        }
                    }
                    catch (Exception)
                    {
                        if (isConnected)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                UpdateStatus("Audio receiving error", Color.Red);
                            });
                        }
                        break;
                    }
                }
            }, cancellationTokenSource.Token);
        }

        private void StartReceivingVideo()
        {
            Task.Run(() =>
            {
                while (isConnected && videoStream != null)
                {
                    try
                    {
                        byte[] lengthBytes = new byte[4];
                        int read = videoStream.Read(lengthBytes, 0, 4);
                        if (read < 4) break;
                        int imageLength = BitConverter.ToInt32(lengthBytes, 0);

                        byte[] imageBytes = new byte[imageLength];
                        int totalRead = 0;
                        while (totalRead < imageLength)
                        {
                            int bytesRead = videoStream.Read(imageBytes, totalRead, imageLength - totalRead);
                            if (bytesRead == 0) break;
                            totalRead += bytesRead;
                        }

                        using (var ms = new MemoryStream(imageBytes))
                        {
                            Bitmap bmp = new Bitmap(ms);
                            this.Invoke((MethodInvoker)delegate
                            {
                                videoBox.Image?.Dispose();
                                videoBox.Image = (Bitmap)bmp.Clone();
                            });
                        }
                    }
                    catch { break; }
                }
            });
        }

        private void UpdateButtonStates(bool connected)
        {
            var connectButton = mainPanel.Controls["connectButton"] as Button;
            var endButton = mainPanel.Controls["endButton"] as Button;
            var muteButton = mainPanel.Controls["muteButton"] as Button;
            var toggleVideoButton = mainPanel.Controls["toggleVideoButton"] as Button;

            if (connectButton != null) connectButton.Enabled = !connected;
            if (endButton != null) endButton.Enabled = connected;
            if (muteButton != null) muteButton.Enabled = connected;
            if (toggleVideoButton != null) toggleVideoButton.Enabled = connected;
        }

        private void EndCall(object sender, EventArgs e)
        {
            ResetConnection();
            UpdateStatus("Call ended. Audio saved to: " + audioFilePath, Color.White);
            // Đóng file ghi âm sau khi kết thúc cuộc gọi
            waveFileWriter?.Close();
        }

        private void ResetConnection()
        {
            isConnected = false;
            isMicMuted = false;

            cancellationTokenSource?.Cancel();

            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }

            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }

            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }

            if (server != null)
            {
                server.Stop();
                server = null;
            }

            if (videoStream != null)
            {
                videoStream.Close();
                videoStream = null;
            }

            if (videoClient != null)
            {
                videoClient.Close();
                videoClient = null;
            }

            if (videoServer != null)
            {
                videoServer.Stop();
                videoServer = null;
            }

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            InitializeAudio();
            UpdateButtonStates(false);
        }

        private void AddToConnectionHistory(string serverInfo)
        {
            if (string.IsNullOrWhiteSpace(serverInfo) ||
                serverInfo.Contains("Enter IP address to connect") ||
                serverInfo.Contains("No Wi-Fi IPv4 address found"))
                return;

            try
            {
                string connectionInfo = $"{serverInfo} - {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                connectionHistory.Insert(0, connectionInfo);

                // Giữ lại tối đa 10 bản ghi gần nhất, loại bỏ bản ghi không hợp lệ
                connectionHistory = connectionHistory
                    .Where(entry =>
                    {
                        var idx = entry.LastIndexOf('-');
                        if (idx < 0) return false;
                        var datePart = entry.Substring(idx + 1).Trim();
                        DateTime dt;
                        return DateTime.TryParseExact(
                            datePart,
                            "dd/MM/yyyy HH:mm:ss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out dt);
                    })
                    .Take(10)
                    .ToList();

                SaveConnectionHistory();

                // Kiểm tra file có tồn tại không, nếu không thì tạo file mẫu
                if (!File.Exists(HISTORY_FILE_PATH))
                {
                    File.WriteAllText(HISTORY_FILE_PATH, "Test file created by AddToConnectionHistory\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating connection history: {ex.Message}",
                    "History Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowHistory()
        {
            mainPanel.Controls.Clear();

            Label titleLabel = new Label
            {
                Text = "Connection History",
                Location = new Point(50, 20),
                Size = new Size(500, 30),
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };

            ListBox historyListBox = new ListBox
            {
                Location = new Point(50, 60),
                Size = new Size(500, 300),
                Font = new Font("Arial", 12),
                BackColor = Color.FromArgb(47, 54, 64),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            if (connectionHistory.Count == 0)
            {
                historyListBox.Items.Add("No connection history available");
            }
            else
            {
                foreach (string connection in connectionHistory)
                {
                    historyListBox.Items.Add(connection);
                }
            }

            Button clearButton = new Button
            {
                Text = "Clear History",
                Location = new Point(50, 370),
                Size = new Size(150, 40),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.IndianRed,
                FlatStyle = FlatStyle.Flat
            };
            clearButton.Click += (s, e) =>
            {
                connectionHistory.Clear();
                File.Delete(HISTORY_FILE_PATH);
                historyListBox.Items.Clear();
                historyListBox.Items.Add("No connection history available");
            };

            Button backButton = new Button
            {
                Text = "Back",
                Location = new Point(400, 370),
                Size = new Size(150, 40),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            backButton.Click += (s, e) => ShowCallInterface();

            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(historyListBox);
            mainPanel.Controls.Add(clearButton);
            mainPanel.Controls.Add(backButton);
        }

        private void EndButton_Click(object sender, EventArgs e)
        {
            EndCall();
        }

        private void EndCall()
        {
            isConnected = false;
            isMicMuted = false;

            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }

            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }

            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }

            if (server != null)
            {
                server.Stop();
                server = null;
            }

            if (videoStream != null)
            {
                videoStream.Close();
                videoStream = null;
            }

            if (videoClient != null)
            {
                videoClient.Close();
                videoClient = null;
            }

            if (videoServer != null)
            {
                videoServer.Stop();
                videoServer = null;
            }

            var connectButton = mainPanel.Controls.Find("connectButton", false)[0] as Button;
            var endButton = mainPanel.Controls.Find("endButton", false)[0] as Button;
            var ipAddressTextBox = mainPanel.Controls.Find("ipAddressTextBox", false)[0] as TextBox;
            var statusLabel = mainPanel.Controls.Find("statusLabel", false)[0] as Label;
            var muteButton = mainPanel.Controls.Find("muteButton", false)[0] as Button;

            connectButton.Enabled = true;
            endButton.Enabled = false;
            ipAddressTextBox.Enabled = true;
            muteButton.Enabled = false;
            muteButton.Text = "Mute Mic";
            muteButton.BackColor = Color.LightGray;
            statusLabel.Text = "Status: Disconnected";
            statusLabel.ForeColor = Color.White;

            StopVideo();
        }

        private void ToggleVideoButton_Click(object sender, EventArgs e)
        {
            if (!isVideoStreaming)
                StartVideo();
            else
                StopVideo();
        }

        private void StartVideo()
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("No webcam found.");
                return;
            }
            videoDevice = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoDevice.NewFrame += VideoDevice_NewFrame;
            videoDevice.Start();
            isVideoStreaming = true;
            toggleVideoButton.Text = "Stop Video";
        }

        private void StopVideo()
        {
            if (videoDevice != null && videoDevice.IsRunning)
            {
                videoDevice.SignalToStop();
                videoDevice.NewFrame -= VideoDevice_NewFrame;
                videoDevice = null;
            }
            isVideoStreaming = false;
            toggleVideoButton.Text = "Start Video";
            videoBox.Image = null;
        }

        private void VideoDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (isProcessingFrame) return;
            isProcessingFrame = true;
            try
            {
                // Chỉ gửi frame nếu đã đủ thời gian (ví dụ 100ms = 10fps)
                if ((DateTime.Now - lastSent).TotalMilliseconds < 100)
                    return;
                lastSent = DateTime.Now;

                using (var ms = new MemoryStream())
                {
                    // Resize frame nhỏ lại (ví dụ 320x240)
                    Bitmap original = (Bitmap)eventArgs.Frame.Clone();
                    Bitmap resized = new Bitmap(original, new Size(320, 240));
                    // Nén JPEG chất lượng thấp hơn (ví dụ quality = 40)
                    var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 40L);
                    resized.Save(ms, encoder, encoderParams);

                    byte[] imageBytes = ms.ToArray();

                    if (videoStream != null && videoStream.CanWrite)
                    {
                        byte[] lengthBytes = BitConverter.GetBytes(imageBytes.Length);
                        videoStream.Write(lengthBytes, 0, lengthBytes.Length);
                        videoStream.Write(imageBytes, 0, imageBytes.Length);
                    }

                    // Hiển thị lên localVideoBox (local preview)
                    this.Invoke((MethodInvoker)delegate
                    {
                        var localVideoBox = mainPanel.Controls["localVideoBox"] as PictureBox;
                        if (localVideoBox != null)
                        {
                            localVideoBox.Image?.Dispose();
                            localVideoBox.Image = (Bitmap)resized.Clone();
                        }
                    });

                    original.Dispose();
                    resized.Dispose();
                }
            }
            catch { /* Xử lý lỗi nếu cần */ }
            finally
            {
                isProcessingFrame = false;
            }
        }

        private void introductionButton_Click(object sender, EventArgs e)
        {
            ShowIntroduction();
        }

        private void callButton_Click(object sender, EventArgs e)
        {
            ShowCallInterface();
        }

        

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void historyButton_Click(object sender, EventArgs e)
        {
            ShowHistory();
        }
    }
}


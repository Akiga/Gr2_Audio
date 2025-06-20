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



namespace Gr2_Audio
{
    public partial class Form1 : Form
    {
        private const string HISTORY_FILE_PATH = "history.txt";
        private WaveIn waveIn;
        private WaveOut waveOut;
        private List<string> connectionHistory = new List<string>();
        private bool isServer = false;
    


        public Form1()
        {

            InitializeComponent();
            connectionHistory = new List<string>();
            ShowIntroduction();
            LoadConnectionHistory();

           

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

            GroupBox modeGroup = new GroupBox
            {
                Text = "Connection Mode",
                Location = new Point(50, 20),
                Size = new Size(400, 60),
                ForeColor = Color.White
            };
            RadioButton hostRadio = new RadioButton
            {
                Text = "Host Call",
                Location = new Point(20, 25),
                ForeColor = Color.White,
                Checked = false
            };

            RadioButton clientRadio = new RadioButton
            {
                Text = "Join Call",
                Location = new Point(200, 25),
                ForeColor = Color.White,
                Checked = true
            };
            modeGroup.Controls.AddRange(new Control[] { hostRadio, clientRadio });
            TextBox ipAddressTextBox = new TextBox
            {
                Location = new Point(50, 100),
                Name = "ipAddressTextBox",
                Size = new Size(400, 30),
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
                Location = new Point(50, 140),
                Name = "portTextBox",
                Size = new Size(400, 30),
                Font = new Font("Arial", 12),
                ForeColor = Color.Gray,
                Text = "Enter port number"
            };
            mainPanel.Controls.Add(portTextBox);

            Button connectButton = new Button
            {
                Location = new Point(50, 150),
                Name = "connectButton",
                Size = new Size(120, 50),
                Text = "Connect",
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            Button endButton = new Button
            {
                Location = new Point(190, 150),
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
                Location = new Point(330, 150),
                Name = "muteButton",
                Size = new Size(120, 50),
                Text = "Mute Mic",
                BackColor = Color.LightGray,
                Enabled = false,
                Font = new Font("Arial", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            Label statusLabel = new Label
            {
                Location = new Point(50, 220),
                Name = "statusLabel",
                Size = new Size(400, 60),
                Text = "Status: Ready",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label volumeTitle = new Label
            {
                Location = new Point(50, 290),
                Text = "Volume:",
                Size = new Size(70, 30),
                Font = new Font("Arial", 12),
                ForeColor = Color.White
            };

            TrackBar volumeBar = new TrackBar
            {
                Location = new Point(120, 290),
                Name = "volumeBar",
                Size = new Size(250, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 10
            };

            Label volumeLabel = new Label
            {
                Location = new Point(380, 290),
                Name = "volumeLabel",
                Size = new Size(50, 30),
                Text = "50%",
                Font = new Font("Arial", 12),
                ForeColor = Color.White
            };
            //Gán sự kiện cho các thành phần giao diện: hostRadio, volumeBar, connectButton,...
            hostRadio.CheckedChanged += (s, e) =>
            {
                bool isChecked = hostRadio.Checked;
                isServer = isChecked;
                ipAddressTextBox.Enabled = !isChecked;

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
                waveOut?.SetVolume(volumeBar.Value / 100f);
            };

            connectButton.Click += async (s, e) =>
            {
                connectButton.Enabled = false;

                try
                {
                    if (isServer)
                        await StartHosting();
                    else
                        await StartClient(ipAddressTextBox.Text);
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
    }   

}              
                

















    












        
    


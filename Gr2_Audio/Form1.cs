using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;




namespace Gr2_Audio
{
    public partial class Form1: Form
    {
        private const string HISTORY_FILE_PATH = "history.txt";
        private List<string> connectionHistory = new List<string>();
       
        public Form1()
        {

            InitializeComponent();
            connectionHistory = new List<string>();
            ShowIntroduction();
            LoadConnectionHistory();

        }

        private void MakeButtonRound(Button btn)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = Math.Min(btn.Width, btn.Height); // hình tròn

            path.AddEllipse(20, 0, radius, radius);
            btn.Region = new Region(path);
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

        private void introductionButton_Click(object sender, EventArgs e)
        {
            ShowIntroduction();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MakeButtonRound(callButton);
            MakeButtonRound(historyButton);
            MakeButtonRound(exitButton);
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


        











    }
}

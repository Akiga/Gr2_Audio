namespace Gr2_Audio
{
    using System;

    using System.Drawing;


    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.mainPanel = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.exitButton = new System.Windows.Forms.Button();
            this.historyButton = new System.Windows.Forms.Button();
            this.callButton = new System.Windows.Forms.Button();
            this.introductionButton = new System.Windows.Forms.Button();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.BackColor = System.Drawing.Color.White;
            this.mainPanel.Location = new System.Drawing.Point(98, 2);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(704, 451);
            this.mainPanel.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(110)))), ((int)(((byte)(255)))));
            this.panel2.Controls.Add(this.exitButton);
            this.panel2.Controls.Add(this.historyButton);
            this.panel2.Controls.Add(this.callButton);
            this.panel2.Controls.Add(this.introductionButton);
            this.panel2.Location = new System.Drawing.Point(1, -3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(100, 456);
            this.panel2.TabIndex = 0;
            // 
            // exitButton
            // 
            this.exitButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.exitButton.Image = ((System.Drawing.Image)(resources.GetObject("exitButton.Image")));
            this.exitButton.Location = new System.Drawing.Point(0, 340);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(100, 39);
            this.exitButton.TabIndex = 3;
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // historyButton
            // 
            this.historyButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.historyButton.Image = ((System.Drawing.Image)(resources.GetObject("historyButton.Image")));
            this.historyButton.Location = new System.Drawing.Point(0, 247);
            this.historyButton.Name = "historyButton";
            this.historyButton.Size = new System.Drawing.Size(100, 39);
            this.historyButton.TabIndex = 2;
            this.historyButton.UseVisualStyleBackColor = true;
            // 
            // callButton
            // 
            this.callButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.callButton.Image = ((System.Drawing.Image)(resources.GetObject("callButton.Image")));
            this.callButton.Location = new System.Drawing.Point(0, 142);
            this.callButton.Name = "callButton";
            this.callButton.Size = new System.Drawing.Size(100, 39);
            this.callButton.TabIndex = 1;
            this.callButton.UseVisualStyleBackColor = true;
            // 
            // introductionButton
            // 
            this.introductionButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.introductionButton.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.introductionButton.Location = new System.Drawing.Point(0, 61);
            this.introductionButton.Name = "introductionButton";
            this.introductionButton.Size = new System.Drawing.Size(100, 33);
            this.introductionButton.TabIndex = 0;
            this.introductionButton.Text = "GIỚI THIỆU";
            this.introductionButton.UseVisualStyleBackColor = true;
            this.introductionButton.Click += new System.EventHandler(this.introductionButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.mainPanel);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button introductionButton;
        private System.Windows.Forms.Button callButton;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Button historyButton;
        private static Bitmap ResizeIcon(string path, int width, int height)
        {
            if (!System.IO.File.Exists(path))
            {
                return null; // Trả về null nếu tệp không tồn tại
            }
            try
            {
                using (Bitmap originalIcon = new Bitmap(path))
                {
                    return new Bitmap(originalIcon, new Size(width, height));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi tải ảnh: " + ex.Message);
                return null;
            }
        }

        private void SetButtonIcons()
        {
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
        
            if (System.IO.File.Exists(System.IO.Path.Combine(iconPath, "intro.png")))
                this.introductionButton.Image = ResizeIcon(System.IO.Path.Combine(iconPath, "intro.png"), 24, 24);
        
            if (System.IO.File.Exists(System.IO.Path.Combine(iconPath, "call.png")))
                this.callButton.Image = ResizeIcon(System.IO.Path.Combine(iconPath, "call.png"), 24, 24);
        
            if (System.IO.File.Exists(System.IO.Path.Combine(iconPath, "history.png")))
                this.historyButton.Image = ResizeIcon(System.IO.Path.Combine(iconPath, "history.png"), 24, 24);
        
            if (System.IO.File.Exists(System.IO.Path.Combine(iconPath, "exit.png")))
                this.exitButton.Image = ResizeIcon(System.IO.Path.Combine(iconPath, "exit.png"), 24, 24);
        }
    }
}


namespace PartialityLauncher {
    partial class MainWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if( disposing && ( components != null ) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.RunGameButton = new System.Windows.Forms.Button();
            this.GamePanel = new System.Windows.Forms.Panel();
            this.RestoreBackupButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.EnabledModBox = new System.Windows.Forms.ListBox();
            this.SwapButton = new System.Windows.Forms.Button();
            this.DisabledModBox = new System.Windows.Forms.ListBox();
            this.GameIcon = new System.Windows.Forms.PictureBox();
            this.GameLabel = new System.Windows.Forms.Label();
            this.GamePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GameIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // RunGameButton
            // 
            this.RunGameButton.Location = new System.Drawing.Point(12, 495);
            this.RunGameButton.Name = "RunGameButton";
            this.RunGameButton.Size = new System.Drawing.Size(422, 23);
            this.RunGameButton.TabIndex = 0;
            this.RunGameButton.Text = "Run Game";
            this.RunGameButton.UseVisualStyleBackColor = true;
            this.RunGameButton.Click += new System.EventHandler(this.Run_Game_Click);
            // 
            // GamePanel
            // 
            this.GamePanel.Controls.Add(this.RestoreBackupButton);
            this.GamePanel.Controls.Add(this.label2);
            this.GamePanel.Controls.Add(this.label1);
            this.GamePanel.Controls.Add(this.RefreshButton);
            this.GamePanel.Controls.Add(this.EnabledModBox);
            this.GamePanel.Controls.Add(this.SwapButton);
            this.GamePanel.Controls.Add(this.DisabledModBox);
            this.GamePanel.Controls.Add(this.GameIcon);
            this.GamePanel.Controls.Add(this.GameLabel);
            this.GamePanel.Location = new System.Drawing.Point(12, 12);
            this.GamePanel.Name = "GamePanel";
            this.GamePanel.Size = new System.Drawing.Size(422, 477);
            this.GamePanel.TabIndex = 1;
            // 
            // RestoreBackupButton
            // 
            this.RestoreBackupButton.Location = new System.Drawing.Point(327, 3);
            this.RestoreBackupButton.Name = "RestoreBackupButton";
            this.RestoreBackupButton.Size = new System.Drawing.Size(92, 23);
            this.RestoreBackupButton.TabIndex = 8;
            this.RestoreBackupButton.Text = "Restore Backup";
            this.RestoreBackupButton.UseVisualStyleBackColor = true;
            this.RestoreBackupButton.Click += new System.EventHandler(this.RestoreBackupButton_Click);
            this.RestoreBackupButton.Paint += new System.Windows.Forms.PaintEventHandler(this.RestoreBackupButton_Paint);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(168, 93);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Enabled Patches";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 90);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Disabled Patches";
            // 
            // RefreshButton
            // 
            this.RefreshButton.Location = new System.Drawing.Point(344, 80);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(75, 23);
            this.RefreshButton.TabIndex = 5;
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // EnabledModBox
            // 
            this.EnabledModBox.FormattingEnabled = true;
            this.EnabledModBox.Location = new System.Drawing.Point(171, 109);
            this.EnabledModBox.Name = "EnabledModBox";
            this.EnabledModBox.Size = new System.Drawing.Size(248, 368);
            this.EnabledModBox.Sorted = true;
            this.EnabledModBox.TabIndex = 4;
            this.EnabledModBox.SelectedValueChanged += new System.EventHandler(this.EnabledModBox_SelectedValueChanged);
            // 
            // SwapButton
            // 
            this.SwapButton.Location = new System.Drawing.Point(131, 241);
            this.SwapButton.Name = "SwapButton";
            this.SwapButton.Size = new System.Drawing.Size(34, 45);
            this.SwapButton.TabIndex = 3;
            this.SwapButton.Text = "< >";
            this.SwapButton.UseVisualStyleBackColor = true;
            this.SwapButton.Click += new System.EventHandler(this.SwapButton_Click);
            // 
            // DisabledModBox
            // 
            this.DisabledModBox.FormattingEnabled = true;
            this.DisabledModBox.Location = new System.Drawing.Point(3, 106);
            this.DisabledModBox.Name = "DisabledModBox";
            this.DisabledModBox.Size = new System.Drawing.Size(122, 368);
            this.DisabledModBox.TabIndex = 2;
            this.DisabledModBox.SelectedValueChanged += new System.EventHandler(this.DisabledModBox_SelectedValueChanged);
            // 
            // GameIcon
            // 
            this.GameIcon.Location = new System.Drawing.Point(3, 3);
            this.GameIcon.Name = "GameIcon";
            this.GameIcon.Size = new System.Drawing.Size(70, 70);
            this.GameIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.GameIcon.TabIndex = 1;
            this.GameIcon.TabStop = false;
            // 
            // GameLabel
            // 
            this.GameLabel.AutoSize = true;
            this.GameLabel.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GameLabel.Location = new System.Drawing.Point(79, 25);
            this.GameLabel.Name = "GameLabel";
            this.GameLabel.Size = new System.Drawing.Size(117, 22);
            this.GameLabel.TabIndex = 0;
            this.GameLabel.Text = "Game Name";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 530);
            this.Controls.Add(this.GamePanel);
            this.Controls.Add(this.RunGameButton);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Partiality Launcher";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainWindow_Paint);
            this.GamePanel.ResumeLayout(false);
            this.GamePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GameIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button RunGameButton;
        private System.Windows.Forms.Panel GamePanel;
        private System.Windows.Forms.PictureBox GameIcon;
        private System.Windows.Forms.Label GameLabel;
        private System.Windows.Forms.ListBox EnabledModBox;
        private System.Windows.Forms.Button SwapButton;
        private System.Windows.Forms.ListBox DisabledModBox;
        private System.Windows.Forms.Button RefreshButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button RestoreBackupButton;
    }
}


namespace DebugGui
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.PictureMain = new System.Windows.Forms.PictureBox();
            this.DropdownMapChoice = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.LblBuildStep = new System.Windows.Forms.Label();
            this.LblFutureBuildSteps = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.PictureMain)).BeginInit();
            this.SuspendLayout();
            // 
            // PictureMain
            // 
            this.PictureMain.Location = new System.Drawing.Point(-44, -5);
            this.PictureMain.Name = "PictureMain";
            this.PictureMain.Size = new System.Drawing.Size(1240, 1044);
            this.PictureMain.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.PictureMain.TabIndex = 0;
            this.PictureMain.TabStop = false;
            // 
            // DropdownMapChoice
            // 
            this.DropdownMapChoice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DropdownMapChoice.FormattingEnabled = true;
            this.DropdownMapChoice.Location = new System.Drawing.Point(1463, 12);
            this.DropdownMapChoice.MaxDropDownItems = 32;
            this.DropdownMapChoice.Name = "DropdownMapChoice";
            this.DropdownMapChoice.Size = new System.Drawing.Size(429, 29);
            this.DropdownMapChoice.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1379, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 21);
            this.label1.TabIndex = 2;
            this.label1.Text = "Map type:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1367, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 21);
            this.label2.TabIndex = 3;
            this.label2.Text = "Build order:";
            // 
            // LblBuildStep
            // 
            this.LblBuildStep.AutoSize = true;
            this.LblBuildStep.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblBuildStep.Location = new System.Drawing.Point(1463, 57);
            this.LblBuildStep.Name = "LblBuildStep";
            this.LblBuildStep.Size = new System.Drawing.Size(28, 25);
            this.LblBuildStep.TabIndex = 4;
            this.LblBuildStep.Text = "__";
            // 
            // LblFutureBuildSteps
            // 
            this.LblFutureBuildSteps.AutoSize = true;
            this.LblFutureBuildSteps.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblFutureBuildSteps.Location = new System.Drawing.Point(1463, 82);
            this.LblFutureBuildSteps.Name = "LblFutureBuildSteps";
            this.LblFutureBuildSteps.Size = new System.Drawing.Size(24, 21);
            this.LblFutureBuildSteps.TabIndex = 5;
            this.LblFutureBuildSteps.Text = "__";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1904, 1186);
            this.Controls.Add(this.LblFutureBuildSteps);
            this.Controls.Add(this.LblBuildStep);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DropdownMapChoice);
            this.Controls.Add(this.PictureMain);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainForm";
            this.Text = "BOSSE - StarCraft 2 Bot";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.PictureMain)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox PictureMain;
        private System.Windows.Forms.ComboBox DropdownMapChoice;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label LblBuildStep;
        private System.Windows.Forms.Label LblFutureBuildSteps;
    }
}


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
            this.LabelStandardMap = new System.Windows.Forms.Label();
            this.LabelTerrainMap = new System.Windows.Forms.Label();
            this.LabelPathMap = new System.Windows.Forms.Label();
            this.LabelInfluenceMap = new System.Windows.Forms.Label();
            this.LabelTensionMap = new System.Windows.Forms.Label();
            this.LabelVulnerabilityMap = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // LabelStandardMap
            // 
            this.LabelStandardMap.AutoSize = true;
            this.LabelStandardMap.Location = new System.Drawing.Point(12, 21);
            this.LabelStandardMap.Name = "LabelStandardMap";
            this.LabelStandardMap.Size = new System.Drawing.Size(233, 21);
            this.LabelStandardMap.TabIndex = 0;
            this.LabelStandardMap.Text = "Overview Map (API coordinates)";
            // 
            // LabelTerrainMap
            // 
            this.LabelTerrainMap.AutoSize = true;
            this.LabelTerrainMap.Location = new System.Drawing.Point(1180, 21);
            this.LabelTerrainMap.Name = "LabelTerrainMap";
            this.LabelTerrainMap.Size = new System.Drawing.Size(92, 21);
            this.LabelTerrainMap.TabIndex = 1;
            this.LabelTerrainMap.Text = "Terrain Map";
            // 
            // LabelPathMap
            // 
            this.LabelPathMap.AutoSize = true;
            this.LabelPathMap.Location = new System.Drawing.Point(1180, 359);
            this.LabelPathMap.Name = "LabelPathMap";
            this.LabelPathMap.Size = new System.Drawing.Size(97, 21);
            this.LabelPathMap.TabIndex = 2;
            this.LabelPathMap.Text = "Pathing Map";
            // 
            // LabelInfluenceMap
            // 
            this.LabelInfluenceMap.AutoSize = true;
            this.LabelInfluenceMap.Location = new System.Drawing.Point(1522, 21);
            this.LabelInfluenceMap.Name = "LabelInfluenceMap";
            this.LabelInfluenceMap.Size = new System.Drawing.Size(108, 21);
            this.LabelInfluenceMap.TabIndex = 3;
            this.LabelInfluenceMap.Text = "Influence Map";
            // 
            // LabelTensionMap
            // 
            this.LabelTensionMap.AutoSize = true;
            this.LabelTensionMap.Location = new System.Drawing.Point(1522, 359);
            this.LabelTensionMap.Name = "LabelTensionMap";
            this.LabelTensionMap.Size = new System.Drawing.Size(97, 21);
            this.LabelTensionMap.TabIndex = 4;
            this.LabelTensionMap.Text = "Tension Map";
            // 
            // LabelVulnerabilityMap
            // 
            this.LabelVulnerabilityMap.AutoSize = true;
            this.LabelVulnerabilityMap.Location = new System.Drawing.Point(1522, 697);
            this.LabelVulnerabilityMap.Name = "LabelVulnerabilityMap";
            this.LabelVulnerabilityMap.Size = new System.Drawing.Size(133, 21);
            this.LabelVulnerabilityMap.TabIndex = 5;
            this.LabelVulnerabilityMap.Text = "Vulnerability Map";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2049, 1195);
            this.Controls.Add(this.LabelVulnerabilityMap);
            this.Controls.Add(this.LabelTensionMap);
            this.Controls.Add(this.LabelInfluenceMap);
            this.Controls.Add(this.LabelPathMap);
            this.Controls.Add(this.LabelTerrainMap);
            this.Controls.Add(this.LabelStandardMap);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainForm";
            this.Text = "BOSSE Maps";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LabelStandardMap;
        private System.Windows.Forms.Label LabelTerrainMap;
        private System.Windows.Forms.Label LabelPathMap;
        private System.Windows.Forms.Label LabelInfluenceMap;
        private System.Windows.Forms.Label LabelTensionMap;
        private System.Windows.Forms.Label LabelVulnerabilityMap;
    }
}


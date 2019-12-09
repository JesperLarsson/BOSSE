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
            this.LabelStandardMap = new System.Windows.Forms.Label();
            this.LabelTerrainMap = new System.Windows.Forms.Label();
            this.LabelInfluenceMap = new System.Windows.Forms.Label();
            this.LabelTensionMap = new System.Windows.Forms.Label();
            this.LabelVulnerabilityMap = new System.Windows.Forms.Label();
            this.LabelPlacementGrid = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // LabelStandardMap
            // 
            this.LabelStandardMap.AutoSize = true;
            this.LabelStandardMap.Location = new System.Drawing.Point(1180, 359);
            this.LabelStandardMap.Name = "LabelStandardMap";
            this.LabelStandardMap.Size = new System.Drawing.Size(111, 21);
            this.LabelStandardMap.TabIndex = 0;
            this.LabelStandardMap.Text = "Overview Map";
            this.LabelStandardMap.Click += new System.EventHandler(this.LabelStandardMap_Click);
            // 
            // LabelTerrainMap
            // 
            this.LabelTerrainMap.AutoSize = true;
            this.LabelTerrainMap.Location = new System.Drawing.Point(1180, 21);
            this.LabelTerrainMap.Name = "LabelTerrainMap";
            this.LabelTerrainMap.Size = new System.Drawing.Size(92, 21);
            this.LabelTerrainMap.TabIndex = 1;
            this.LabelTerrainMap.Text = "Terrain Map";
            this.LabelTerrainMap.Click += new System.EventHandler(this.LabelTerrainMap_Click);
            // 
            // LabelInfluenceMap
            // 
            this.LabelInfluenceMap.AutoSize = true;
            this.LabelInfluenceMap.Location = new System.Drawing.Point(1522, 21);
            this.LabelInfluenceMap.Name = "LabelInfluenceMap";
            this.LabelInfluenceMap.Size = new System.Drawing.Size(108, 21);
            this.LabelInfluenceMap.TabIndex = 3;
            this.LabelInfluenceMap.Text = "Influence Map";
            this.LabelInfluenceMap.Click += new System.EventHandler(this.LabelInfluenceMap_Click);
            // 
            // LabelTensionMap
            // 
            this.LabelTensionMap.AutoSize = true;
            this.LabelTensionMap.Location = new System.Drawing.Point(1522, 359);
            this.LabelTensionMap.Name = "LabelTensionMap";
            this.LabelTensionMap.Size = new System.Drawing.Size(97, 21);
            this.LabelTensionMap.TabIndex = 4;
            this.LabelTensionMap.Text = "Tension Map";
            this.LabelTensionMap.Click += new System.EventHandler(this.LabelTensionMap_Click);
            // 
            // LabelVulnerabilityMap
            // 
            this.LabelVulnerabilityMap.AutoSize = true;
            this.LabelVulnerabilityMap.Location = new System.Drawing.Point(1522, 697);
            this.LabelVulnerabilityMap.Name = "LabelVulnerabilityMap";
            this.LabelVulnerabilityMap.Size = new System.Drawing.Size(133, 21);
            this.LabelVulnerabilityMap.TabIndex = 5;
            this.LabelVulnerabilityMap.Text = "Vulnerability Map";
            this.LabelVulnerabilityMap.Click += new System.EventHandler(this.LabelVulnerabilityMap_Click);
            // 
            // LabelPlacementGrid
            // 
            this.LabelPlacementGrid.AutoSize = true;
            this.LabelPlacementGrid.Location = new System.Drawing.Point(1180, 697);
            this.LabelPlacementGrid.Name = "LabelPlacementGrid";
            this.LabelPlacementGrid.Size = new System.Drawing.Size(116, 21);
            this.LabelPlacementGrid.TabIndex = 6;
            this.LabelPlacementGrid.Text = "Placement Grid";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2049, 1195);
            this.Controls.Add(this.LabelPlacementGrid);
            this.Controls.Add(this.LabelVulnerabilityMap);
            this.Controls.Add(this.LabelTensionMap);
            this.Controls.Add(this.LabelInfluenceMap);
            this.Controls.Add(this.LabelTerrainMap);
            this.Controls.Add(this.LabelStandardMap);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainForm";
            this.Text = "BOSSE - StarCraft 2 Bot";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LabelStandardMap;
        private System.Windows.Forms.Label LabelTerrainMap;
        private System.Windows.Forms.Label LabelInfluenceMap;
        private System.Windows.Forms.Label LabelTensionMap;
        private System.Windows.Forms.Label LabelVulnerabilityMap;
        private System.Windows.Forms.Label LabelPlacementGrid;
    }
}


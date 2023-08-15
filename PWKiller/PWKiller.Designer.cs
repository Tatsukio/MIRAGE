namespace PWKiller
{
    partial class PWKillerMain
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
        public void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PWKillerMain));
            this.PWKillerButtonText = new System.Windows.Forms.Button();
            this.PWKillerTitleText = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // PWKillerButtonText
            // 
            this.PWKillerButtonText.Font = new System.Drawing.Font("Trebuchet MS", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PWKillerButtonText.Location = new System.Drawing.Point(12, 170);
            this.PWKillerButtonText.Name = "PWKillerButtonText";
            this.PWKillerButtonText.Size = new System.Drawing.Size(275, 120);
            this.PWKillerButtonText.TabIndex = 0;
            this.PWKillerButtonText.Text = "End processes";
            this.PWKillerButtonText.UseVisualStyleBackColor = true;
            this.PWKillerButtonText.Click += new System.EventHandler(this.MainButton_Click);
            // 
            // PWKillerTitleText
            // 
            this.PWKillerTitleText.Font = new System.Drawing.Font("Trebuchet MS", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PWKillerTitleText.Location = new System.Drawing.Point(12, 10);
            this.PWKillerTitleText.Name = "PWKillerTitleText";
            this.PWKillerTitleText.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.PWKillerTitleText.Size = new System.Drawing.Size(275, 150);
            this.PWKillerTitleText.TabIndex = 1;
            this.PWKillerTitleText.Text = "If ParaWorld crashes or does not terminate, press this button!";
            this.PWKillerTitleText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PWKillerMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(299, 302);
            this.Controls.Add(this.PWKillerTitleText);
            this.Controls.Add(this.PWKillerButtonText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "PWKillerMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PWKiller";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button PWKillerButtonText;
        private System.Windows.Forms.Label PWKillerTitleText;
    }
}


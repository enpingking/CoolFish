namespace CoolFish
{
    partial class MainWindow
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
            this.ProcessCB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.AttachBTN = new System.Windows.Forms.Button();
            this.OutputRTB = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // ProcessCB
            // 
            this.ProcessCB.FormattingEnabled = true;
            this.ProcessCB.Location = new System.Drawing.Point(12, 50);
            this.ProcessCB.Name = "ProcessCB";
            this.ProcessCB.Size = new System.Drawing.Size(576, 21);
            this.ProcessCB.TabIndex = 0;
            this.ProcessCB.DropDown += new System.EventHandler(this.ProcessCB_DropDown);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial Narrow", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(207, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "Pick a WoW process to attach to:";
            // 
            // AttachBTN
            // 
            this.AttachBTN.Location = new System.Drawing.Point(594, 50);
            this.AttachBTN.Name = "AttachBTN";
            this.AttachBTN.Size = new System.Drawing.Size(75, 23);
            this.AttachBTN.TabIndex = 2;
            this.AttachBTN.Text = "Attach";
            this.AttachBTN.UseVisualStyleBackColor = true;
            this.AttachBTN.Click += new System.EventHandler(this.AttachBTN_Click);
            // 
            // OutputRTB
            // 
            this.OutputRTB.Location = new System.Drawing.Point(12, 75);
            this.OutputRTB.Name = "OutputRTB";
            this.OutputRTB.Size = new System.Drawing.Size(576, 96);
            this.OutputRTB.TabIndex = 3;
            this.OutputRTB.Text = "";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(683, 183);
            this.Controls.Add(this.OutputRTB);
            this.Controls.Add(this.AttachBTN);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ProcessCB);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(699, 222);
            this.MinimumSize = new System.Drawing.Size(699, 222);
            this.Name = "MainWindow";
            this.Text = "CoolFish";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox ProcessCB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button AttachBTN;
        private System.Windows.Forms.RichTextBox OutputRTB;
    }
}
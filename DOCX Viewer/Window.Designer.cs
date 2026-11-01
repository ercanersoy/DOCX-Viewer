// Copyright (c) 2024 Ercan Ersoy
//
// Written by Ercan Ersoy.
// This file was changed with Claude Code (Anthropic): the menu-free,
// button based and resizable user interface.

namespace Viewer
{
    partial class Window_Form
    {
        /// <summary>
        ///Gerekli tasarımcı değişkeni.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///Kullanılan tüm kaynakları temizleyin.
        /// </summary>
        ///<param name="disposing">yönetilen kaynaklar dispose edilmeliyse doğru; aksi halde yanlış.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        /// <summary>
        /// Tasarımcı desteği için gerekli metot - bu metodun
        ///içeriğini kod düzenleyici ile değiştirmeyin.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.Main_TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.Toolbar_FlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.Open_File_Button = new System.Windows.Forms.Button();
            this.Copy_Button = new System.Windows.Forms.Button();
            this.Select_All_Button = new System.Windows.Forms.Button();
            this.Zoom_Out_Button = new System.Windows.Forms.Button();
            this.Zoom_In_Button = new System.Windows.Forms.Button();
            this.Zoom_Reset_Button = new System.Windows.Forms.Button();
            this.Find_TextBox = new System.Windows.Forms.TextBox();
            this.Find_Button = new System.Windows.Forms.Button();
            this.About_Button = new System.Windows.Forms.Button();
            this.Area_RichTextBox = new Viewer.ViewerRichTextBox();
            this.Status_StatusStrip = new System.Windows.Forms.StatusStrip();
            this.File_ToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.Zoom_ToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.Open_File_OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.Main_ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.Main_TableLayoutPanel.SuspendLayout();
            this.Toolbar_FlowLayoutPanel.SuspendLayout();
            this.Status_StatusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // Main_TableLayoutPanel
            //
            this.Main_TableLayoutPanel.ColumnCount = 1;
            this.Main_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Main_TableLayoutPanel.Controls.Add(this.Toolbar_FlowLayoutPanel, 0, 0);
            this.Main_TableLayoutPanel.Controls.Add(this.Area_RichTextBox, 0, 1);
            this.Main_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Main_TableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Main_TableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.Main_TableLayoutPanel.Name = "Main_TableLayoutPanel";
            this.Main_TableLayoutPanel.RowCount = 2;
            this.Main_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.Main_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Main_TableLayoutPanel.Size = new System.Drawing.Size(884, 579);
            this.Main_TableLayoutPanel.TabIndex = 0;
            //
            // Toolbar_FlowLayoutPanel
            //
            this.Toolbar_FlowLayoutPanel.AutoSize = true;
            this.Toolbar_FlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Open_File_Button);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Copy_Button);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Select_All_Button);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Zoom_Out_Button);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Zoom_In_Button);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Zoom_Reset_Button);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Find_TextBox);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.Find_Button);
            this.Toolbar_FlowLayoutPanel.Controls.Add(this.About_Button);
            this.Toolbar_FlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Toolbar_FlowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Toolbar_FlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.Toolbar_FlowLayoutPanel.Name = "Toolbar_FlowLayoutPanel";
            this.Toolbar_FlowLayoutPanel.Padding = new System.Windows.Forms.Padding(4);
            this.Toolbar_FlowLayoutPanel.Size = new System.Drawing.Size(884, 42);
            this.Toolbar_FlowLayoutPanel.TabIndex = 0;
            this.Toolbar_FlowLayoutPanel.WrapContents = true;
            //
            // Open_File_Button
            //
            this.Open_File_Button.AutoSize = true;
            this.Open_File_Button.Margin = new System.Windows.Forms.Padding(3);
            this.Open_File_Button.MinimumSize = new System.Drawing.Size(80, 28);
            this.Open_File_Button.Name = "Open_File_Button";
            this.Open_File_Button.Size = new System.Drawing.Size(80, 28);
            this.Open_File_Button.TabIndex = 0;
            this.Open_File_Button.Text = "&Open...";
            this.Main_ToolTip.SetToolTip(this.Open_File_Button, "Open a Word document (Ctrl+O)");
            this.Open_File_Button.UseVisualStyleBackColor = true;
            this.Open_File_Button.Click += new System.EventHandler(this.Open_File_Button_Click);
            //
            // Copy_Button
            //
            this.Copy_Button.AutoSize = true;
            this.Copy_Button.Margin = new System.Windows.Forms.Padding(3);
            this.Copy_Button.MinimumSize = new System.Drawing.Size(80, 28);
            this.Copy_Button.Name = "Copy_Button";
            this.Copy_Button.Size = new System.Drawing.Size(80, 28);
            this.Copy_Button.TabIndex = 1;
            this.Copy_Button.Text = "&Copy";
            this.Main_ToolTip.SetToolTip(this.Copy_Button, "Copy the selected content (Ctrl+C)");
            this.Copy_Button.UseVisualStyleBackColor = true;
            this.Copy_Button.Click += new System.EventHandler(this.Copy_Button_Click);
            //
            // Select_All_Button
            //
            this.Select_All_Button.AutoSize = true;
            this.Select_All_Button.Margin = new System.Windows.Forms.Padding(3);
            this.Select_All_Button.MinimumSize = new System.Drawing.Size(80, 28);
            this.Select_All_Button.Name = "Select_All_Button";
            this.Select_All_Button.Size = new System.Drawing.Size(80, 28);
            this.Select_All_Button.TabIndex = 2;
            this.Select_All_Button.Text = "Select &All";
            this.Main_ToolTip.SetToolTip(this.Select_All_Button, "Select the whole document (Ctrl+A)");
            this.Select_All_Button.UseVisualStyleBackColor = true;
            this.Select_All_Button.Click += new System.EventHandler(this.Select_All_Button_Click);
            //
            // Zoom_Out_Button
            //
            this.Zoom_Out_Button.AutoSize = true;
            this.Zoom_Out_Button.Margin = new System.Windows.Forms.Padding(3);
            this.Zoom_Out_Button.MinimumSize = new System.Drawing.Size(80, 28);
            this.Zoom_Out_Button.Name = "Zoom_Out_Button";
            this.Zoom_Out_Button.Size = new System.Drawing.Size(80, 28);
            this.Zoom_Out_Button.TabIndex = 3;
            this.Zoom_Out_Button.Text = "Zoom Out";
            this.Main_ToolTip.SetToolTip(this.Zoom_Out_Button, "Zoom out (Ctrl+-)");
            this.Zoom_Out_Button.UseVisualStyleBackColor = true;
            this.Zoom_Out_Button.Click += new System.EventHandler(this.Zoom_Out_Button_Click);
            //
            // Zoom_In_Button
            //
            this.Zoom_In_Button.AutoSize = true;
            this.Zoom_In_Button.Margin = new System.Windows.Forms.Padding(3);
            this.Zoom_In_Button.MinimumSize = new System.Drawing.Size(80, 28);
            this.Zoom_In_Button.Name = "Zoom_In_Button";
            this.Zoom_In_Button.Size = new System.Drawing.Size(80, 28);
            this.Zoom_In_Button.TabIndex = 4;
            this.Zoom_In_Button.Text = "Zoom In";
            this.Main_ToolTip.SetToolTip(this.Zoom_In_Button, "Zoom in (Ctrl++)");
            this.Zoom_In_Button.UseVisualStyleBackColor = true;
            this.Zoom_In_Button.Click += new System.EventHandler(this.Zoom_In_Button_Click);
            //
            // Zoom_Reset_Button
            //
            this.Zoom_Reset_Button.AutoSize = true;
            this.Zoom_Reset_Button.Margin = new System.Windows.Forms.Padding(3);
            this.Zoom_Reset_Button.MinimumSize = new System.Drawing.Size(60, 28);
            this.Zoom_Reset_Button.Name = "Zoom_Reset_Button";
            this.Zoom_Reset_Button.Size = new System.Drawing.Size(60, 28);
            this.Zoom_Reset_Button.TabIndex = 5;
            this.Zoom_Reset_Button.Text = "100%";
            this.Main_ToolTip.SetToolTip(this.Zoom_Reset_Button, "Actual size (Ctrl+0)");
            this.Zoom_Reset_Button.UseVisualStyleBackColor = true;
            this.Zoom_Reset_Button.Click += new System.EventHandler(this.Zoom_Reset_Button_Click);
            //
            // Find_TextBox
            //
            this.Find_TextBox.Margin = new System.Windows.Forms.Padding(12, 6, 3, 3);
            this.Find_TextBox.Name = "Find_TextBox";
            this.Find_TextBox.Size = new System.Drawing.Size(160, 20);
            this.Find_TextBox.TabIndex = 6;
            this.Main_ToolTip.SetToolTip(this.Find_TextBox, "Text to find (Ctrl+F)");
            this.Find_TextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Find_TextBox_KeyDown);
            //
            // Find_Button
            //
            this.Find_Button.AutoSize = true;
            this.Find_Button.Margin = new System.Windows.Forms.Padding(3);
            this.Find_Button.MinimumSize = new System.Drawing.Size(80, 28);
            this.Find_Button.Name = "Find_Button";
            this.Find_Button.Size = new System.Drawing.Size(80, 28);
            this.Find_Button.TabIndex = 7;
            this.Find_Button.Text = "&Find Next";
            this.Main_ToolTip.SetToolTip(this.Find_Button, "Find the next match (F3)");
            this.Find_Button.UseVisualStyleBackColor = true;
            this.Find_Button.Click += new System.EventHandler(this.Find_Button_Click);
            //
            // About_Button
            //
            this.About_Button.AutoSize = true;
            this.About_Button.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            this.About_Button.MinimumSize = new System.Drawing.Size(80, 28);
            this.About_Button.Name = "About_Button";
            this.About_Button.Size = new System.Drawing.Size(80, 28);
            this.About_Button.TabIndex = 8;
            this.About_Button.Text = "A&bout";
            this.Main_ToolTip.SetToolTip(this.About_Button, "About DOCX Viewer");
            this.About_Button.UseVisualStyleBackColor = true;
            this.About_Button.Click += new System.EventHandler(this.About_Button_Click);
            //
            // Area_RichTextBox
            //
            this.Area_RichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Area_RichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Area_RichTextBox.Location = new System.Drawing.Point(0, 42);
            this.Area_RichTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.Area_RichTextBox.Name = "Area_RichTextBox";
            this.Area_RichTextBox.Size = new System.Drawing.Size(884, 537);
            this.Area_RichTextBox.TabIndex = 1;
            this.Area_RichTextBox.Text = "";
            //
            // Status_StatusStrip
            //
            this.Status_StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.File_ToolStripStatusLabel,
            this.Zoom_ToolStripStatusLabel});
            this.Status_StatusStrip.Location = new System.Drawing.Point(0, 579);
            this.Status_StatusStrip.Name = "Status_StatusStrip";
            this.Status_StatusStrip.Size = new System.Drawing.Size(884, 22);
            this.Status_StatusStrip.TabIndex = 1;
            //
            // File_ToolStripStatusLabel
            //
            this.File_ToolStripStatusLabel.Name = "File_ToolStripStatusLabel";
            this.File_ToolStripStatusLabel.Size = new System.Drawing.Size(819, 17);
            this.File_ToolStripStatusLabel.Spring = true;
            this.File_ToolStripStatusLabel.Text = "Open a Word document or drop it on this window.";
            this.File_ToolStripStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // Zoom_ToolStripStatusLabel
            //
            this.Zoom_ToolStripStatusLabel.Name = "Zoom_ToolStripStatusLabel";
            this.Zoom_ToolStripStatusLabel.Size = new System.Drawing.Size(35, 17);
            this.Zoom_ToolStripStatusLabel.Text = "100%";
            //
            // Open_File_OpenFileDialog
            //
            this.Open_File_OpenFileDialog.Filter = "Word Documents (*.docx;*.docm;*.dotx;*.dotm)|*.docx;*.docm;*.dotx;*.dotm|All Files (*.*)|*.*";
            this.Open_File_OpenFileDialog.Title = "Open Word Document";
            //
            // Window_Form
            //
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 601);
            this.Controls.Add(this.Main_TableLayoutPanel);
            this.Controls.Add(this.Status_StatusStrip);
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "Window_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DOCX Viewer";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Window_Form_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Window_Form_DragEnter);
            this.Main_TableLayoutPanel.ResumeLayout(false);
            this.Main_TableLayoutPanel.PerformLayout();
            this.Toolbar_FlowLayoutPanel.ResumeLayout(false);
            this.Toolbar_FlowLayoutPanel.PerformLayout();
            this.Status_StatusStrip.ResumeLayout(false);
            this.Status_StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel Main_TableLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel Toolbar_FlowLayoutPanel;
        private System.Windows.Forms.Button Open_File_Button;
        private System.Windows.Forms.Button Copy_Button;
        private System.Windows.Forms.Button Select_All_Button;
        private System.Windows.Forms.Button Zoom_Out_Button;
        private System.Windows.Forms.Button Zoom_In_Button;
        private System.Windows.Forms.Button Zoom_Reset_Button;
        private System.Windows.Forms.TextBox Find_TextBox;
        private System.Windows.Forms.Button Find_Button;
        private System.Windows.Forms.Button About_Button;
        private Viewer.ViewerRichTextBox Area_RichTextBox;
        private System.Windows.Forms.StatusStrip Status_StatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel File_ToolStripStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel Zoom_ToolStripStatusLabel;
        private System.Windows.Forms.OpenFileDialog Open_File_OpenFileDialog;
        private System.Windows.Forms.ToolTip Main_ToolTip;
    }
}

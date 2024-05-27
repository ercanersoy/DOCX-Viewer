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
            this.Open_File_Button = new System.Windows.Forms.Button();
            this.Area_RichTextBox = new System.Windows.Forms.RichTextBox();
            this.Open_File_OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // Open_File_Button
            // 
            this.Open_File_Button.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.Open_File_Button.Location = new System.Drawing.Point(12, 12);
            this.Open_File_Button.Name = "Open_File_Button";
            this.Open_File_Button.Size = new System.Drawing.Size(90, 40);
            this.Open_File_Button.TabIndex = 0;
            this.Open_File_Button.Text = "Open File";
            this.Open_File_Button.UseVisualStyleBackColor = true;
            this.Open_File_Button.Click += new System.EventHandler(this.Open_Button_Click);
            // 
            // Area_RichTextBox
            // 
            this.Area_RichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Area_RichTextBox.Location = new System.Drawing.Point(0, 58);
            this.Area_RichTextBox.Name = "Area_RichTextBox";
            this.Area_RichTextBox.Size = new System.Drawing.Size(784, 303);
            this.Area_RichTextBox.TabIndex = 1;
            this.Area_RichTextBox.Text = "";
            // 
            // Window_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 361);
            this.Controls.Add(this.Area_RichTextBox);
            this.Controls.Add(this.Open_File_Button);
            this.Name = "Window_Form";
            this.Text = "DOCX Viewer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Open_File_Button;
        private System.Windows.Forms.RichTextBox Area_RichTextBox;
        private System.Windows.Forms.OpenFileDialog Open_File_OpenFileDialog;
    }
}


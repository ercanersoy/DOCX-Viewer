// Copyright (c) 2024 Ercan Ersoy
//
// Written by Ercan Ersoy.
// Also, take advantage of ChatGPT GPT-4o and GitHub Copilot.
// This file was changed with Claude Code (Anthropic): Word 2007-2016
// compatible document loading, button based interface, zoom, find,
// drag and drop and command line support.

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Viewer
{
    public partial class Window_Form : Form
    {
        private const string APPLICATION_NAME = "DOCX Viewer";

        private static readonly float[] zoom_levels = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 3f, 4f };

        private readonly string startup_file;
        private float zoom = 1f;

        public Window_Form() : this(null)
        {
        }

        public Window_Form(string file_path)
        {
            InitializeComponent();

            startup_file = file_path;

            // Use the font of the operating system (Segoe UI, Tahoma, ...).
            Font = SystemFonts.MessageBoxFont;

            Area_RichTextBox.AllowDrop = true;
            Area_RichTextBox.EnableAutoDragDrop = false;
            Area_RichTextBox.DragEnter += Window_Form_DragEnter;
            Area_RichTextBox.DragDrop += Window_Form_DragDrop;

            Update_Status(null);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (!string.IsNullOrEmpty(startup_file))
            {
                Open_Document(startup_file);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.O:
                    Open_File_Button.PerformClick();
                    return true;

                case Keys.Control | Keys.F:
                    Find_TextBox.Focus();
                    Find_TextBox.SelectAll();
                    return true;

                case Keys.F3:
                    Find_Next();
                    return true;

                case Keys.Control | Keys.Add:
                case Keys.Control | Keys.Oemplus:
                    Change_Zoom(1);
                    return true;

                case Keys.Control | Keys.Subtract:
                case Keys.Control | Keys.OemMinus:
                    Change_Zoom(-1);
                    return true;

                case Keys.Control | Keys.D0:
                case Keys.Control | Keys.NumPad0:
                    Set_Zoom(1f);
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #region Document

        private void Open_Document(string file_path)
        {
            Cursor old_cursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                string rtf = DocxToRtfConverter.LoadAsRtf(file_path);

                Area_RichTextBox.SuspendLayout();
                Area_RichTextBox.Rtf = rtf;
                Area_RichTextBox.Select(0, 0);
                Area_RichTextBox.ScrollToCaret();
                Area_RichTextBox.ResumeLayout();

                // The zoom factor is reset by the control when new content is loaded.
                Apply_Zoom();

                Text = Path.GetFileName(file_path) + " - " + APPLICATION_NAME;
                Update_Status(file_path);
                Open_File_OpenFileDialog.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(file_path));
            }
            catch (Exception exception)
            {
                Cursor.Current = old_cursor;
                MessageBox.Show(this, "The document could not be opened." + Environment.NewLine + Environment.NewLine + exception.Message,
                    APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = old_cursor;
            }
        }

        private void Update_Status(string file_path)
        {
            if (!string.IsNullOrEmpty(file_path))
            {
                File_ToolStripStatusLabel.Text = file_path;
            }

            Zoom_ToolStripStatusLabel.Text = ((int)Math.Round(zoom * 100)).ToString() + "%";
        }

        #endregion

        #region Buttons

        private void Open_File_Button_Click(object sender, EventArgs e)
        {
            if (Open_File_OpenFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                Open_Document(Open_File_OpenFileDialog.FileName);
            }
        }

        private void Copy_Button_Click(object sender, EventArgs e)
        {
            if (Area_RichTextBox.SelectionLength > 0)
            {
                Area_RichTextBox.Copy();
            }
            Area_RichTextBox.Focus();
        }

        private void Select_All_Button_Click(object sender, EventArgs e)
        {
            Area_RichTextBox.Focus();
            Area_RichTextBox.SelectAll();
        }

        private void Zoom_Out_Button_Click(object sender, EventArgs e)
        {
            Change_Zoom(-1);
        }

        private void Zoom_In_Button_Click(object sender, EventArgs e)
        {
            Change_Zoom(1);
        }

        private void Zoom_Reset_Button_Click(object sender, EventArgs e)
        {
            Set_Zoom(1f);
        }

        private void Find_Button_Click(object sender, EventArgs e)
        {
            Find_Next();
        }

        private void Find_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Find_Next();
            }
        }

        private void About_Button_Click(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            object[] copyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);

            MessageBox.Show(this,
                APPLICATION_NAME + " " + assembly.GetName().Version.ToString() + Environment.NewLine +
                "Viewer for Word 2007, 2010, 2013 and 2016 documents." + Environment.NewLine + Environment.NewLine +
                (copyright.Length > 0 ? ((AssemblyCopyrightAttribute)copyright[0]).Copyright : "") + Environment.NewLine +
                "Licensed under the MIT License.",
                "About " + APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Zoom

        private void Change_Zoom(int direction)
        {
            // The user may also zoom with Ctrl + mouse wheel.
            float current = Area_RichTextBox.ZoomFactor;
            float next = current;

            if (direction > 0)
            {
                foreach (float level in zoom_levels)
                {
                    if (level > current + 0.01f)
                    {
                        next = level;
                        break;
                    }
                }
            }
            else
            {
                for (int i = zoom_levels.Length - 1; i >= 0; i--)
                {
                    if (zoom_levels[i] < current - 0.01f)
                    {
                        next = zoom_levels[i];
                        break;
                    }
                }
            }

            Set_Zoom(next);
        }

        private void Set_Zoom(float value)
        {
            zoom = value;
            Apply_Zoom();
            Update_Status(null);
        }

        private void Apply_Zoom()
        {
            // Setting the same value again is ignored by the control, so reset it first.
            Area_RichTextBox.ZoomFactor = 1f;
            Area_RichTextBox.ZoomFactor = zoom;
        }

        #endregion

        #region Find

        private void Find_Next()
        {
            string text = Find_TextBox.Text;

            if (text.Length == 0)
            {
                Find_TextBox.Focus();
                return;
            }

            int start = Area_RichTextBox.SelectionStart + Area_RichTextBox.SelectionLength;
            int index = start < Area_RichTextBox.TextLength ? Area_RichTextBox.Find(text, start, RichTextBoxFinds.None) : -1;

            if (index < 0)
            {
                // Continue from the beginning of the document.
                index = Area_RichTextBox.Find(text, 0, RichTextBoxFinds.None);
            }

            if (index < 0)
            {
                MessageBox.Show(this, "\"" + text + "\" was not found.", APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Area_RichTextBox.Select(index, text.Length);
            Area_RichTextBox.ScrollToCaret();
        }

        #endregion

        #region Drag and drop

        private void Window_Form_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void Window_Form_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (files != null && files.Length > 0)
            {
                Activate();
                Open_Document(files[0]);
            }
        }

        #endregion
    }
}

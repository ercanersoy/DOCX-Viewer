// Copyright (c) 2024 Ercan Ersoy
//
// This file was created with Claude Code (Anthropic).
//
// A read-only RichTextBox that uses RichEdit 4.1 (msftedit.dll) when it is
// available. RichEdit 4.1 shows PNG/JPEG pictures and tables much better than
// the RichEdit 2.0/3.0 control that .NET Framework 4.0 uses by default.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Viewer
{
    internal class ViewerRichTextBox : RichTextBox
    {
        private const int WM_USER = 0x0400;
        private const int EM_SETTYPOGRAPHYOPTIONS = WM_USER + 202;
        private const int TO_ADVANCEDTYPOGRAPHY = 1;

        private static bool richEdit41Checked;
        private static bool richEdit41Available;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public ViewerRichTextBox()
        {
            ReadOnly = true;
            BackColor = SystemColors.Window;
            DetectUrls = false;
            HideSelection = false;
            ShortcutsEnabled = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;

                if (!richEdit41Checked)
                {
                    richEdit41Checked = true;

                    try
                    {
                        richEdit41Available = LoadLibrary("msftedit.dll") != IntPtr.Zero;
                    }
                    catch (Exception)
                    {
                        richEdit41Available = false;
                    }
                }

                if (richEdit41Available)
                {
                    parameters.ClassName = "RICHEDIT50W";
                }

                return parameters;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Needed for justified paragraphs and better table layout.
            SendMessage(Handle, EM_SETTYPOGRAPHYOPTIONS, new IntPtr(TO_ADVANCEDTYPOGRAPHY), new IntPtr(TO_ADVANCEDTYPOGRAPHY));
        }

        protected override void OnReadOnlyChanged(EventArgs e)
        {
            base.OnReadOnlyChanged(e);

            // A read-only RichTextBox becomes gray by default; keep it white like a page.
            BackColor = SystemColors.Window;
        }
    }
}

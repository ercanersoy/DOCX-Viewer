// Copyright (c) 2024-2026 Ercan Ersoy
//
// Written with Claude Code Opus 5.5.

using System;
using System.Windows.Forms;

namespace Viewer
{
    internal static class Program
    {
        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Window_Form(args.Length > 0 ? args[0] : null));
        }
    }
}

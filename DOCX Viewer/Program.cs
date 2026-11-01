// Copyright (c) 2024 Ercan Ersoy
//
// Written by Ercan Ersoy.
// This file was changed with Claude Code (Anthropic): a document can be
// given as a command line argument (for example "Open with" in Explorer).

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

// Copyright (c) 2024 Ercan Ersoy
//
// Written by Ercan Ersoy.
// Also, take advantage of ChatGPT GPT-4o and GitHub Copilot.

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Viewer
{
    public partial class Window_Form : Form
    {
        public Window_Form()
        {
            InitializeComponent();
        }

        private void Open_Button_Click(object sender, EventArgs e)
        {
            if (Open_File_OpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string file_path = Open_File_OpenFileDialog.FileName;

                    using (WordprocessingDocument word_document = WordprocessingDocument.Open(file_path, false))
                    {
                        Body body = word_document.MainDocumentPart.Document.Body;

                        Area_RichTextBox.Clear();

                        foreach (var paragraph in body.Elements<Paragraph>())
                        {
                            if (paragraph.ParagraphProperties != null && paragraph.ParagraphProperties.Justification != null)
                            {
                                if(paragraph.ParagraphProperties.Justification.Val == JustificationValues.Center)
                                {
                                    Area_RichTextBox.SelectionAlignment = HorizontalAlignment.Center;
                                }
                                else if (paragraph.ParagraphProperties.Justification.Val == JustificationValues.Center)
                                {
                                    Area_RichTextBox.SelectionAlignment = HorizontalAlignment.Right;
                                }
                                else
                                {
                                    Area_RichTextBox.SelectionAlignment = HorizontalAlignment.Left;
                                }
                            }
                            else
                            {
                                Area_RichTextBox.SelectionAlignment = HorizontalAlignment.Left;
                            }

                            foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>())
                            {
                                foreach (var text in run.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>())
                                {
                                    int start = Area_RichTextBox.TextLength;
                                    int end = Area_RichTextBox.TextLength;

                                    FontStyle style = FontStyle.Regular;
                                    float font_size = 12;
                                    System.Drawing.Color color = Area_RichTextBox.SelectionColor;

                                    Area_RichTextBox.Select(start, end - start);

                                    if (run.RunProperties != null)
                                    {
                                        if (run.RunProperties.Bold != null && run.RunProperties.Bold.Val != OnOffValue.FromBoolean(true))
                                        {
                                            style |= FontStyle.Bold;
                                        }

                                        if (run.RunProperties.Italic != null && run.RunProperties.Italic.Val != OnOffValue.FromBoolean(true))
                                        {
                                            style |= FontStyle.Italic;
                                        }

                                        if (run.RunProperties.Underline != null && run.RunProperties.Underline.Val != OnOffValue.FromBoolean(true))
                                        {
                                            style |= FontStyle.Underline;
                                        }

                                        if (run.RunProperties.Color != null)
                                        {
                                            color = ColorTranslator.FromHtml("#" + run.RunProperties.Color.Val);
                                        }

                                        if (run.RunProperties.FontSize != null)
                                        {
                                            font_size = Convert.ToInt32(run.RunProperties.FontSize.Val) / 2f;
                                        }
                                    }

                                    Area_RichTextBox.SelectionFont = new System.Drawing.Font(Area_RichTextBox.SelectionFont.FontFamily, font_size, style);
                                    Area_RichTextBox.SelectionColor = color;

                                    Area_RichTextBox.AppendText(text.Text);

                                    Area_RichTextBox.Select(end, 0);
                                }
                            }

                            Area_RichTextBox.AppendText(Environment.NewLine);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

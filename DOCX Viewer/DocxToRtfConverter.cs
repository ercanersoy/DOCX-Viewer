// Copyright (c) 2024 Ercan Ersoy
//
// This file was created with Claude Code (Anthropic).
//
// Converts Word 2007, 2010, 2013 and 2016 documents (.docx, .docm, .dotx,
// .dotm, transitional and strict Open XML) to RTF so that they can be shown
// in a RichTextBox. The converter works on the XML level so that it does not
// depend on newer element types and it is compatible with .NET Framework 4.0.

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

namespace Viewer
{
    internal sealed class DocxToRtfConverter
    {
        private const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private const string W_STRICT = "http://purl.oclc.org/ooxml/wordprocessingml/main";
        private const string R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string R_STRICT = "http://purl.oclc.org/ooxml/officeDocument/relationships";
        private const string MC = "http://schemas.openxmlformats.org/markup-compatibility/2006";
        private const string M = "http://schemas.openxmlformats.org/officeDocument/2006/math";
        private const string M_STRICT = "http://purl.oclc.org/ooxml/officeDocument/math";

        private const int UNSET = int.MinValue;
        private const int MAX_CELLS = 63;

        #region Public API

        /// <summary>
        /// Opens a Word 2007-2016 document and returns its content as RTF.
        /// </summary>
        public static string LoadAsRtf(string path)
        {
            byte[] bytes;

            // The file is read with full sharing so that documents that are
            // open in Word can be viewed too.
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                bytes = new byte[file.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = file.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0)
                    {
                        break;
                    }
                    offset += read;
                }
            }

            if (bytes.Length >= 8 && bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0 &&
                bytes[4] == 0xA1 && bytes[5] == 0xB1 && bytes[6] == 0x1A && bytes[7] == 0xE1)
            {
                throw new NotSupportedException(
                    "This file is either a Word 97-2003 (.doc) document or a password protected document." + Environment.NewLine +
                    "Only Word 2007, 2010, 2013 and 2016 documents (.docx, .docm, .dotx, .dotm) without a password are supported.");
            }

            if (bytes.Length < 4 || bytes[0] != 0x50 || bytes[1] != 0x4B)
            {
                throw new NotSupportedException(
                    "This file is not a valid Word 2007, 2010, 2013 or 2016 document.");
            }

            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                // Newer (Word 2010, 2013 and 2016) markup is normalized to the
                // Word 2007 markup by choosing the compatible alternatives.
                OpenSettings settings = new OpenSettings();
                settings.AutoSave = false;
                settings.MarkupCompatibilityProcessSettings = new MarkupCompatibilityProcessSettings(
                    MarkupCompatibilityProcessMode.ProcessAllParts, FileFormatVersions.Office2007);

                using (WordprocessingDocument document = WordprocessingDocument.Open(stream, true, settings))
                {
                    return new DocxToRtfConverter(document).ConvertToRtf();
                }
            }
        }

        #endregion

        #region Nested types

        private sealed class RunProps
        {
            public bool Bold, Italic, Underline, Strike, Caps, SmallCaps, Hidden;
            public int VertAlign; // 0 = baseline, 1 = superscript, 2 = subscript
            public string Font = "Times New Roman";
            public string FontEastAsia;
            public string FontComplex;
            public int HalfPoints = 20;
            public Color? ForeColor, Highlight, Shading;

            public RunProps Clone()
            {
                return (RunProps)MemberwiseClone();
            }
        }

        private sealed class TabStop
        {
            public int Position;
            public string Kind;
            public string Leader;
        }

        private sealed class ParaProps
        {
            public string Justification;
            public int Left, Right, FirstLine, Before, After;
            public int Line = 240;
            public string LineRule = "auto";
            public bool ContextualSpacing, Bidi;
            public string NumId;
            public int Ilvl;
            public List<TabStop> Tabs = new List<TabStop>();

            public ParaProps Clone()
            {
                ParaProps copy = (ParaProps)MemberwiseClone();
                copy.Tabs = new List<TabStop>(Tabs);
                return copy;
            }
        }

        private sealed class StyleInfo
        {
            public string Id, Type, BasedOn;
            public OpenXmlElement PPr, RPr, TblPr;
        }

        private sealed class LevelInfo
        {
            public int Start = 1;
            public string Format = "decimal";
            public string Text = "";
            public string Suffix = "tab";
            public bool IsLegal;
            public OpenXmlElement PPr, RPr;
        }

        private sealed class CellContext
        {
            public bool First = true;
        }

        private sealed class InlineState
        {
            public bool First = true;
        }

        #endregion

        #region Fields

        private readonly MainDocumentPart mainPart;
        private OpenXmlPart currentPart;

        private readonly StringBuilder body = new StringBuilder();

        private readonly List<string> fontNames = new List<string>();
        private readonly List<int> fontCharsets = new List<int>();
        private readonly Dictionary<string, int> fontIndexes = new Dictionary<string, int>();
        private readonly List<Color> colors = new List<Color>();
        private readonly Dictionary<int, int> colorIndexes = new Dictionary<int, int>();

        private readonly Dictionary<string, StyleInfo> styles = new Dictionary<string, StyleInfo>();
        private string defaultParagraphStyle, defaultCharacterStyle, defaultTableStyle;
        private RunProps defaultRunProps = new RunProps();
        private ParaProps defaultParaProps = new ParaProps();
        private readonly Dictionary<string, RunProps> baseRunCache = new Dictionary<string, RunProps>();
        private readonly List<string> tableStyleStack = new List<string>();

        private string minorFont = "Calibri", majorFont = "Cambria";
        private string minorFontEastAsia, majorFontEastAsia, minorFontComplex, majorFontComplex;
        private int defaultTab = 720;

        private readonly Dictionary<string, Dictionary<int, LevelInfo>> abstractLevels = new Dictionary<string, Dictionary<int, LevelInfo>>();
        private readonly Dictionary<string, string> abstractStyleLinks = new Dictionary<string, string>();
        private readonly Dictionary<string, string> numToAbstract = new Dictionary<string, string>();
        private readonly Dictionary<string, Dictionary<int, LevelInfo>> numLevelOverrides = new Dictionary<string, Dictionary<int, LevelInfo>>();
        private readonly Dictionary<string, Dictionary<int, int>> numStartOverrides = new Dictionary<string, Dictionary<int, int>>();
        private readonly Dictionary<string, int[]> listCounters = new Dictionary<string, int[]>();
        private readonly HashSet<string> seenNumIds = new HashSet<string>();

        private readonly List<bool> fieldStack = new List<bool>();
        private string previousParagraphStyle;

        private readonly List<string> footnoteOrder = new List<string>();
        private readonly Dictionary<string, string> footnoteMarks = new Dictionary<string, string>();
        private readonly List<string> endnoteOrder = new List<string>();
        private readonly Dictionary<string, string> endnoteMarks = new Dictionary<string, string>();
        private string currentNoteMark = "";

        private static readonly Dictionary<string, Color> highlightColors = CreateHighlightColors();

        #endregion

        private DocxToRtfConverter(WordprocessingDocument document)
        {
            mainPart = document.MainDocumentPart;
            currentPart = mainPart;
        }

        #region Conversion

        private string ConvertToRtf()
        {
            if (mainPart == null || mainPart.RootElement == null)
            {
                throw new InvalidDataException("The document does not contain a main document part.");
            }

            LoadTheme();
            LoadStyles();
            LoadNumbering();
            LoadSettings();

            // Font 0 is the default font of the document.
            GetFontIndex(defaultRunProps.Font, 0);

            OpenXmlElement bodyElement = Child(mainPart.RootElement, "body");

            if (bodyElement != null)
            {
                OpenXmlElement firstSection = FindFirstSection(bodyElement);

                RenderHeaderFooter(firstSection, "headerReference", true);
                RenderBlocks(bodyElement, body, null);
                RenderNotes(true);
                RenderNotes(false);
                RenderHeaderFooter(firstSection, "footerReference", false);
            }

            StringBuilder rtf = new StringBuilder(body.Length + 1024);
            rtf.Append("{\\rtf1\\ansi\\ansicpg1252\\deff0\\uc1\\deftab").Append(defaultTab);
            rtf.Append("{\\fonttbl");
            for (int i = 0; i < fontNames.Count; i++)
            {
                rtf.Append("{\\f").Append(i).Append("\\fnil\\fcharset").Append(fontCharsets[i]).Append(' ');
                AppendEscaped(rtf, fontNames[i], false);
                rtf.Append(";}");
            }
            rtf.Append('}');
            rtf.Append("{\\colortbl ;");
            foreach (Color color in colors)
            {
                rtf.Append("\\red").Append(color.R).Append("\\green").Append(color.G).Append("\\blue").Append(color.B).Append(';');
            }
            rtf.Append('}');
            rtf.Append("\\viewkind4\\pard\\plain ");
            rtf.Append(body.ToString());
            rtf.Append('}');

            return rtf.ToString();
        }

        private static OpenXmlElement FindFirstSection(OpenXmlElement bodyElement)
        {
            foreach (OpenXmlElement element in bodyElement.Descendants())
            {
                if (IsW(element, "sectPr"))
                {
                    return element;
                }
            }
            return null;
        }

        private void RenderHeaderFooter(OpenXmlElement section, string referenceName, bool isHeader)
        {
            if (section == null)
            {
                return;
            }

            bool titlePage = OnOff(Child(section, "titlePg"));
            string wanted = titlePage ? "first" : "default";
            string relationshipId = null;
            string fallbackId = null;

            foreach (OpenXmlElement reference in section.ChildElements)
            {
                if (!IsW(reference, referenceName))
                {
                    continue;
                }

                string type = Attr(reference, "type") ?? "default";
                string id = AttrR(reference, "id");

                if (type == wanted)
                {
                    relationshipId = id;
                }
                else if (type == "default" || fallbackId == null)
                {
                    fallbackId = id;
                }
            }

            if (relationshipId == null)
            {
                relationshipId = fallbackId;
            }

            OpenXmlPart part = GetPart(mainPart, relationshipId);

            if (part == null || part.RootElement == null || !HasVisibleContent(part.RootElement))
            {
                return;
            }

            OpenXmlPart oldPart = currentPart;
            currentPart = part;

            if (!isHeader)
            {
                AppendSeparator(body);
            }

            previousParagraphStyle = null;
            RenderBlocks(part.RootElement, body, null);

            if (isHeader)
            {
                AppendSeparator(body);
            }

            currentPart = oldPart;
        }

        private void RenderNotes(bool footnotes)
        {
            List<string> order = footnotes ? footnoteOrder : endnoteOrder;
            Dictionary<string, string> marks = footnotes ? footnoteMarks : endnoteMarks;

            if (order.Count == 0)
            {
                return;
            }

            OpenXmlPart part = footnotes ? (OpenXmlPart)mainPart.FootnotesPart : (OpenXmlPart)mainPart.EndnotesPart;

            if (part == null || part.RootElement == null)
            {
                return;
            }

            Dictionary<string, OpenXmlElement> notes = new Dictionary<string, OpenXmlElement>();

            foreach (OpenXmlElement note in part.RootElement.ChildElements)
            {
                string id = Attr(note, "id");
                if (id != null && !notes.ContainsKey(id))
                {
                    notes.Add(id, note);
                }
            }

            OpenXmlPart oldPart = currentPart;
            currentPart = part;
            AppendSeparator(body);

            // Footnotes may reference other notes, so the list can grow while rendering.
            for (int i = 0; i < order.Count; i++)
            {
                OpenXmlElement note;
                if (notes.TryGetValue(order[i], out note))
                {
                    currentNoteMark = marks[order[i]];
                    previousParagraphStyle = null;
                    RenderBlocks(note, body, null);
                }
            }

            currentNoteMark = "";
            currentPart = oldPart;
        }

        private void AppendSeparator(StringBuilder sb)
        {
            sb.Append("\\pard\\plain\\qc\\sb120\\sa120{\\f0\\fs16\\cf").Append(GetColorIndex(Color.FromArgb(160, 160, 160))).Append(' ');
            AppendEscaped(sb, new string('─', 40), false);
            sb.Append("\\par}");
        }

        private static bool HasVisibleContent(OpenXmlElement root)
        {
            if (root.InnerText.Trim().Length > 0)
            {
                return true;
            }

            foreach (OpenXmlElement element in root.Descendants())
            {
                if (element.LocalName == "drawing" || element.LocalName == "pict" || element.LocalName == "object")
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Block level

        private void RenderBlocks(OpenXmlElement container, StringBuilder sb, CellContext cell)
        {
            if (container == null)
            {
                return;
            }

            foreach (OpenXmlElement child in container.ChildElements)
            {
                if (IsAlternateContent(child))
                {
                    RenderBlocks(ChooseAlternate(child), sb, cell);
                    continue;
                }

                if (!IsWNamespace(child))
                {
                    continue;
                }

                switch (child.LocalName)
                {
                    case "p":
                        RenderParagraph(child, sb, cell);
                        break;

                    case "tbl":
                        if (cell == null)
                        {
                            RenderTable(child, sb);
                        }
                        else
                        {
                            // Nested tables are shown as tab separated text.
                            BeginCellLine(cell, sb, null);
                            RenderTableAsText(child, sb);
                        }
                        break;

                    case "sdt":
                        RenderBlocks(Child(child, "sdtContent"), sb, cell);
                        break;

                    case "customXml":
                    case "ins":
                    case "moveTo":
                        RenderBlocks(child, sb, cell);
                        break;

                    case "altChunk":
                        if (cell == null)
                        {
                            sb.Append("\\pard\\plain ");
                        }
                        else
                        {
                            BeginCellLine(cell, sb, null);
                        }
                        EmitText(sb, defaultRunProps, "[Embedded content]", true);
                        if (cell == null)
                        {
                            sb.Append("\\par ");
                        }
                        break;
                }
            }
        }

        private void RenderParagraph(OpenXmlElement p, StringBuilder sb, CellContext cell)
        {
            OpenXmlElement pPr = Child(p, "pPr");
            string styleId = GetParagraphStyleId(pPr);
            LevelInfo level;
            string numId;
            int ilvl;
            ParaProps pp = BuildParaProps(pPr, styleId, out level, out numId, out ilvl);
            RunProps baseRun = BuildBaseRunProps(styleId);
            RunProps markRun = baseRun.Clone();
            ApplyRunProps(markRun, Child(pPr, "rPr"));

            if (cell == null)
            {
                if (pp.ContextualSpacing)
                {
                    if (previousParagraphStyle == styleId)
                    {
                        pp.Before = 0;
                    }

                    OpenXmlElement next = NextParagraph(p);
                    if (next != null && GetParagraphStyleId(Child(next, "pPr")) == styleId)
                    {
                        pp.After = 0;
                    }
                }

                sb.Append("\\pard\\plain");
                AppendParagraphFormat(sb, pp, level != null && level.Suffix == "tab");
                sb.Append(' ');
            }
            else
            {
                BeginCellLine(cell, sb, pp);
            }

            previousParagraphStyle = styleId;

            if (level != null)
            {
                EmitNumber(sb, level, numId, ilvl, markRun, false);
            }

            RenderInline(p, sb, baseRun);

            if (cell == null)
            {
                sb.Append('{').Append(RunCodes(markRun, IsSymbolFont(markRun.Font) ? 2 : 0)).Append("\\par}");
            }
        }

        private void BeginCellLine(CellContext cell, StringBuilder sb, ParaProps pp)
        {
            if (cell.First)
            {
                sb.Append("\\pard\\intbl\\plain");
                if (pp != null)
                {
                    ParaProps cellProps = pp.Clone();
                    cellProps.Before = 0;
                    cellProps.After = 0;
                    AppendParagraphFormat(sb, cellProps, false);
                }
                sb.Append(' ');
                cell.First = false;
            }
            else
            {
                sb.Append("\\line ");
            }
        }

        private static OpenXmlElement NextParagraph(OpenXmlElement p)
        {
            OpenXmlElement next = p.NextSibling();
            while (next != null)
            {
                if (IsW(next, "p"))
                {
                    return next;
                }
                if (IsW(next, "tbl") || IsW(next, "sdt"))
                {
                    return null;
                }
                next = next.NextSibling();
            }
            return null;
        }

        private void AppendParagraphFormat(StringBuilder sb, ParaProps pp, bool numberTab)
        {
            switch (pp.Justification)
            {
                case "center":
                    sb.Append("\\qc");
                    break;
                case "right":
                case "end":
                    sb.Append("\\qr");
                    break;
                case "both":
                case "distribute":
                case "lowKashida":
                case "mediumKashida":
                case "highKashida":
                case "thaiDistribute":
                    sb.Append("\\qj");
                    break;
                default:
                    sb.Append("\\ql");
                    break;
            }

            if (pp.Bidi)
            {
                sb.Append("\\rtlpar");
            }

            int left = Math.Max(0, pp.Left);
            int firstLine = Math.Max(-left, pp.FirstLine);

            if (left != 0) sb.Append("\\li").Append(left);
            if (pp.Right > 0) sb.Append("\\ri").Append(pp.Right);
            if (firstLine != 0) sb.Append("\\fi").Append(firstLine);
            if (pp.Before > 0) sb.Append("\\sb").Append(pp.Before);
            if (pp.After > 0) sb.Append("\\sa").Append(pp.After);

            if (pp.Line > 0)
            {
                if (pp.LineRule == "exact")
                {
                    sb.Append("\\sl-").Append(pp.Line).Append("\\slmult0");
                }
                else if (pp.LineRule == "atLeast")
                {
                    sb.Append("\\sl").Append(pp.Line).Append("\\slmult0");
                }
                else if (pp.Line != 240)
                {
                    sb.Append("\\sl").Append(pp.Line).Append("\\slmult1");
                }
            }

            List<TabStop> tabs = new List<TabStop>(pp.Tabs);

            // The hanging indent of a numbered paragraph works as a tab stop.
            if (numberTab && firstLine < 0)
            {
                bool exists = false;
                foreach (TabStop tab in tabs)
                {
                    if (tab.Position == left)
                    {
                        exists = true;
                    }
                }
                if (!exists)
                {
                    TabStop tab = new TabStop();
                    tab.Position = left;
                    tab.Kind = "left";
                    tabs.Add(tab);
                }
            }

            tabs.Sort(delegate(TabStop a, TabStop b) { return a.Position.CompareTo(b.Position); });

            foreach (TabStop tab in tabs)
            {
                if (tab.Position <= 0 || tab.Kind == "bar")
                {
                    continue;
                }

                switch (tab.Kind)
                {
                    case "center":
                        sb.Append("\\tqc");
                        break;
                    case "right":
                    case "end":
                        sb.Append("\\tqr");
                        break;
                    case "decimal":
                        sb.Append("\\tqdec");
                        break;
                }

                switch (tab.Leader)
                {
                    case "dot":
                    case "middleDot":
                        sb.Append("\\tldot");
                        break;
                    case "hyphen":
                        sb.Append("\\tlhyph");
                        break;
                    case "underscore":
                        sb.Append("\\tlul");
                        break;
                    case "heavy":
                        sb.Append("\\tlth");
                        break;
                }

                sb.Append("\\tx").Append(tab.Position);
            }
        }

        #endregion

        #region Tables

        private void RenderTable(OpenXmlElement tbl, StringBuilder sb)
        {
            OpenXmlElement tblPr = Child(tbl, "tblPr");
            string tableStyle = ValOf(tblPr, "tblStyle") ?? defaultTableStyle;
            List<int> grid = new List<int>();

            OpenXmlElement tblGrid = Child(tbl, "tblGrid");
            if (tblGrid != null)
            {
                foreach (OpenXmlElement gridColumn in tblGrid.ChildElements)
                {
                    if (IsW(gridColumn, "gridCol"))
                    {
                        grid.Add(Math.Max(0, ParseTwips(Attr(gridColumn, "w"), 0)));
                    }
                }
            }

            List<OpenXmlElement> rows = new List<OpenXmlElement>();
            CollectChildren(tbl, "tr", rows);

            if (rows.Count == 0)
            {
                return;
            }

            bool borders = HasTableBorders(tblPr, tableStyle);
            int left = 0;
            OpenXmlElement tblInd = Child(tblPr, "tblInd");
            if (tblInd != null && (Attr(tblInd, "type") ?? "dxa") == "dxa")
            {
                left = ParseTwips(Attr(tblInd, "w"), 0);
            }
            string tableJc = ValOf(tblPr, "jc");

            tableStyleStack.Add(tableStyle);
            string savedPreviousStyle = previousParagraphStyle;

            foreach (OpenXmlElement tr in rows)
            {
                List<OpenXmlElement> cells = new List<OpenXmlElement>();
                CollectChildren(tr, "tc", cells);

                if (cells.Count == 0)
                {
                    continue;
                }

                if (cells.Count > MAX_CELLS)
                {
                    cells.RemoveRange(MAX_CELLS, cells.Count - MAX_CELLS);
                }

                OpenXmlElement trPr = Child(tr, "trPr");
                int gridBefore = ParseInt(ValOf(trPr, "gridBefore"), 0);

                sb.Append("\\trowd\\trgaph108\\trleft").Append(left);
                if (tableJc == "center")
                {
                    sb.Append("\\trqc");
                }
                else if (tableJc == "right" || tableJc == "end")
                {
                    sb.Append("\\trqr");
                }

                int x = left;
                int column = 0;
                for (; column < gridBefore && column < grid.Count; column++)
                {
                    x += grid[column];
                }

                List<bool> mergedCells = new List<bool>();

                foreach (OpenXmlElement tc in cells)
                {
                    OpenXmlElement tcPr = Child(tc, "tcPr");
                    int span = Math.Max(1, ParseInt(ValOf(tcPr, "gridSpan"), 1));
                    OpenXmlElement vMerge = Child(tcPr, "vMerge");
                    bool continued = false;

                    if (vMerge != null)
                    {
                        if (Attr(vMerge, "val") == "restart")
                        {
                            sb.Append("\\clvmgf");
                        }
                        else
                        {
                            sb.Append("\\clvmrg");
                            continued = true;
                        }
                    }
                    mergedCells.Add(continued);

                    string vAlign = ValOf(tcPr, "vAlign");
                    if (vAlign == "center")
                    {
                        sb.Append("\\clvertalc");
                    }
                    else if (vAlign == "bottom")
                    {
                        sb.Append("\\clvertalb");
                    }

                    OpenXmlElement shading = Child(tcPr, "shd");
                    Color? fill = shading != null ? ParseColor(Attr(shading, "fill")) : null;
                    if (fill.HasValue)
                    {
                        sb.Append("\\clcbpat").Append(GetColorIndex(fill.Value));
                    }

                    if (borders)
                    {
                        sb.Append("\\clbrdrt\\brdrs\\brdrw10\\clbrdrl\\brdrs\\brdrw10\\clbrdrb\\brdrs\\brdrw10\\clbrdrr\\brdrs\\brdrw10");
                    }

                    int width = 0;
                    for (int i = 0; i < span; i++)
                    {
                        if (column + i < grid.Count)
                        {
                            width += grid[column + i];
                        }
                    }

                    if (width <= 0)
                    {
                        OpenXmlElement tcW = Child(tcPr, "tcW");
                        width = (tcW != null && (Attr(tcW, "type") ?? "dxa") == "dxa") ? ParseTwips(Attr(tcW, "w"), 0) : 0;
                        if (width <= 0)
                        {
                            width = 9000 / cells.Count;
                        }
                    }

                    column += span;
                    x += width;
                    sb.Append("\\cellx").Append(x);
                }

                for (int i = 0; i < cells.Count; i++)
                {
                    CellContext context = new CellContext();
                    previousParagraphStyle = null;

                    if (!mergedCells[i])
                    {
                        RenderBlocks(cells[i], sb, context);
                    }

                    if (context.First)
                    {
                        sb.Append("\\pard\\intbl\\plain ");
                    }

                    sb.Append("\\cell ");
                }

                sb.Append("\\row ");
            }

            tableStyleStack.RemoveAt(tableStyleStack.Count - 1);
            previousParagraphStyle = savedPreviousStyle;
            sb.Append("\\pard\\plain ");
        }

        private void RenderTableAsText(OpenXmlElement tbl, StringBuilder sb)
        {
            List<OpenXmlElement> rows = new List<OpenXmlElement>();
            CollectChildren(tbl, "tr", rows);
            bool firstRow = true;

            OpenXmlElement tblPr = Child(tbl, "tblPr");
            tableStyleStack.Add(ValOf(tblPr, "tblStyle") ?? defaultTableStyle);

            foreach (OpenXmlElement tr in rows)
            {
                if (!firstRow)
                {
                    sb.Append("\\line ");
                }
                firstRow = false;

                List<OpenXmlElement> cells = new List<OpenXmlElement>();
                CollectChildren(tr, "tc", cells);
                bool firstCell = true;

                foreach (OpenXmlElement tc in cells)
                {
                    if (!firstCell)
                    {
                        sb.Append("\\tab ");
                    }
                    firstCell = false;
                    RenderBlocksInline(tc, sb, new InlineState(), " ");
                }
            }

            tableStyleStack.RemoveAt(tableStyleStack.Count - 1);
        }

        private static void CollectChildren(OpenXmlElement container, string name, List<OpenXmlElement> result)
        {
            foreach (OpenXmlElement child in container.ChildElements)
            {
                if (IsW(child, name))
                {
                    result.Add(child);
                }
                else if (IsW(child, "sdt"))
                {
                    OpenXmlElement content = Child(child, "sdtContent");
                    if (content != null)
                    {
                        CollectChildren(content, name, result);
                    }
                }
                else if (IsW(child, "customXml") || IsW(child, "ins") || IsW(child, "moveTo"))
                {
                    CollectChildren(child, name, result);
                }
                else if (IsAlternateContent(child))
                {
                    OpenXmlElement chosen = ChooseAlternate(child);
                    if (chosen != null)
                    {
                        CollectChildren(chosen, name, result);
                    }
                }
            }
        }

        private bool HasTableBorders(OpenXmlElement tblPr, string tableStyle)
        {
            OpenXmlElement tblBorders = Child(tblPr, "tblBorders");

            if (tblBorders == null)
            {
                List<StyleInfo> chain = GetStyleChain(tableStyle);
                for (int i = chain.Count - 1; i >= 0 && tblBorders == null; i--)
                {
                    tblBorders = Child(chain[i].TblPr, "tblBorders");
                }
            }

            if (tblBorders == null)
            {
                return false;
            }

            foreach (OpenXmlElement border in tblBorders.ChildElements)
            {
                string value = Attr(border, "val");
                if (value != null && value != "none" && value != "nil")
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Inline level

        private void RenderBlocksInline(OpenXmlElement container, StringBuilder sb, InlineState state, string separator)
        {
            if (container == null)
            {
                return;
            }

            foreach (OpenXmlElement child in container.ChildElements)
            {
                if (IsAlternateContent(child))
                {
                    RenderBlocksInline(ChooseAlternate(child), sb, state, separator);
                    continue;
                }

                if (IsW(child, "p"))
                {
                    if (!state.First)
                    {
                        sb.Append(separator);
                    }
                    state.First = false;

                    OpenXmlElement pPr = Child(child, "pPr");
                    string styleId = GetParagraphStyleId(pPr);
                    LevelInfo level;
                    string numId;
                    int ilvl;
                    BuildParaProps(pPr, styleId, out level, out numId, out ilvl);
                    RunProps baseRun = BuildBaseRunProps(styleId);

                    if (level != null)
                    {
                        RunProps markRun = baseRun.Clone();
                        ApplyRunProps(markRun, Child(pPr, "rPr"));
                        EmitNumber(sb, level, numId, ilvl, markRun, true);
                    }

                    RenderInline(child, sb, baseRun);
                }
                else if (IsW(child, "tbl"))
                {
                    if (!state.First)
                    {
                        sb.Append(separator);
                    }
                    state.First = false;
                    RenderTableAsText(child, sb);
                }
                else if (IsW(child, "sdt"))
                {
                    RenderBlocksInline(Child(child, "sdtContent"), sb, state, separator);
                }
                else if (IsW(child, "customXml") || IsW(child, "ins") || IsW(child, "moveTo"))
                {
                    RenderBlocksInline(child, sb, state, separator);
                }
            }
        }

        private void RenderInline(OpenXmlElement container, StringBuilder sb, RunProps paragraphRun)
        {
            if (container == null)
            {
                return;
            }

            foreach (OpenXmlElement child in container.ChildElements)
            {
                if (IsAlternateContent(child))
                {
                    RenderInline(ChooseAlternate(child), sb, paragraphRun);
                    continue;
                }

                if (child.NamespaceUri == M || child.NamespaceUri == M_STRICT)
                {
                    if (child.LocalName == "oMath" || child.LocalName == "oMathPara")
                    {
                        RenderMath(child, sb, paragraphRun);
                    }
                    continue;
                }

                if (!IsWNamespace(child))
                {
                    continue;
                }

                switch (child.LocalName)
                {
                    case "r":
                        RenderRun(child, sb, paragraphRun);
                        break;

                    case "hyperlink":
                    case "smartTag":
                    case "customXml":
                    case "fldSimple":
                    case "ins":
                    case "moveTo":
                    case "dir":
                    case "bdo":
                        RenderInline(child, sb, paragraphRun);
                        break;

                    case "sdt":
                        RenderInline(Child(child, "sdtContent"), sb, paragraphRun);
                        break;
                }
            }
        }

        private void RenderRun(OpenXmlElement r, StringBuilder sb, RunProps paragraphRun)
        {
            OpenXmlElement rPr = Child(r, "rPr");
            RunProps rp = paragraphRun.Clone();
            string characterStyle = ValOf(rPr, "rStyle") ?? defaultCharacterStyle;

            foreach (StyleInfo style in GetStyleChain(characterStyle))
            {
                ApplyRunProps(rp, style.RPr);
            }

            ApplyRunProps(rp, rPr);
            RenderRunContent(r, sb, rp, paragraphRun);
        }

        private void RenderRunContent(OpenXmlElement run, StringBuilder sb, RunProps rp, RunProps paragraphRun)
        {
            foreach (OpenXmlElement child in run.ChildElements)
            {
                if (IsAlternateContent(child))
                {
                    OpenXmlElement chosen = ChooseAlternate(child);
                    if (chosen != null)
                    {
                        RenderRunContent(chosen, sb, rp, paragraphRun);
                    }
                    continue;
                }

                if (!IsWNamespace(child))
                {
                    continue;
                }

                switch (child.LocalName)
                {
                    case "t":
                        EmitText(sb, rp, child.InnerText, false);
                        break;

                    case "tab":
                    case "ptab":
                        EmitText(sb, rp, "\t", false);
                        break;

                    case "br":
                    case "cr":
                        EmitText(sb, rp, "\n", false);
                        break;

                    case "noBreakHyphen":
                        EmitText(sb, rp, "-", false);
                        break;

                    case "sym":
                        EmitSymbol(sb, rp, child);
                        break;

                    case "fldChar":
                        UpdateField(Attr(child, "fldCharType"));
                        break;

                    case "drawing":
                        if (!IsFieldCodeHidden())
                        {
                            RenderDrawing(child, sb, rp);
                        }
                        break;

                    case "pict":
                    case "object":
                        if (!IsFieldCodeHidden())
                        {
                            RenderVml(child, sb, rp);
                        }
                        break;

                    case "footnoteReference":
                    case "endnoteReference":
                        EmitNoteReference(sb, rp, child, child.LocalName == "footnoteReference");
                        break;

                    case "footnoteRef":
                    case "endnoteRef":
                        EmitText(sb, rp, currentNoteMark, false);
                        break;

                    case "ruby":
                        OpenXmlElement rubyBase = Child(child, "rubyBase");
                        if (rubyBase != null)
                        {
                            RenderInline(rubyBase, sb, paragraphRun);
                        }
                        break;
                }
            }
        }

        private void EmitNoteReference(StringBuilder sb, RunProps rp, OpenXmlElement reference, bool footnote)
        {
            string id = Attr(reference, "id");

            if (id == null)
            {
                return;
            }

            List<string> order = footnote ? footnoteOrder : endnoteOrder;
            Dictionary<string, string> marks = footnote ? footnoteMarks : endnoteMarks;
            string mark;

            if (!marks.TryGetValue(id, out mark))
            {
                order.Add(id);
                mark = footnote ? order.Count.ToString(CultureInfo.InvariantCulture) : ToRoman(order.Count).ToLowerInvariant();
                marks.Add(id, mark);
            }

            if (!IsTrue(Attr(reference, "customMarkFollows")))
            {
                EmitText(sb, rp, mark, false);
            }
        }

        private void EmitSymbol(StringBuilder sb, RunProps rp, OpenXmlElement sym)
        {
            string font = Attr(sym, "font");
            int code;

            if (!int.TryParse(Attr(sym, "char") ?? "", NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code) || code <= 0 || code > 0xFFFF)
            {
                return;
            }

            RunProps symbolRun = rp.Clone();

            if (!string.IsNullOrEmpty(font))
            {
                symbolRun.Font = font;
            }

            if (!IsSymbolFont(symbolRun.Font) && code >= 0xF000 && code <= 0xF0FF)
            {
                code -= 0xF000;
            }

            EmitText(sb, symbolRun, ((char)code).ToString(), false);
        }

        private void RenderMath(OpenXmlElement math, StringBuilder sb, RunProps paragraphRun)
        {
            StringBuilder text = new StringBuilder();

            foreach (OpenXmlElement element in math.Descendants())
            {
                if (element.LocalName == "t" && (element.NamespaceUri == M || element.NamespaceUri == M_STRICT))
                {
                    text.Append(element.InnerText);
                }
            }

            RunProps mathRun = paragraphRun.Clone();
            mathRun.Font = "Cambria Math";
            mathRun.Italic = true;
            EmitText(sb, mathRun, text.ToString(), false);
        }

        private void UpdateField(string type)
        {
            if (type == "begin")
            {
                fieldStack.Add(false);
            }
            else if (type == "separate")
            {
                if (fieldStack.Count > 0)
                {
                    fieldStack[fieldStack.Count - 1] = true;
                }
            }
            else if (type == "end")
            {
                if (fieldStack.Count > 0)
                {
                    fieldStack.RemoveAt(fieldStack.Count - 1);
                }
            }
        }

        private bool IsFieldCodeHidden()
        {
            foreach (bool inResult in fieldStack)
            {
                if (!inResult)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Drawings and images

        private void RenderDrawing(OpenXmlElement drawing, StringBuilder sb, RunProps rp)
        {
            OpenXmlElement textBox = FindDescendant(drawing, "txbxContent");

            if (textBox != null)
            {
                RenderTextBox(textBox, sb);
                return;
            }

            long cx = 0, cy = 0;
            OpenXmlElement extent = FindDescendant(drawing, "extent");
            if (extent != null)
            {
                long.TryParse(Attr(extent, "cx"), NumberStyles.Integer, CultureInfo.InvariantCulture, out cx);
                long.TryParse(Attr(extent, "cy"), NumberStyles.Integer, CultureInfo.InvariantCulture, out cy);
            }

            List<OpenXmlElement> blips = new List<OpenXmlElement>();
            foreach (OpenXmlElement element in drawing.Descendants())
            {
                if (element.LocalName == "blip")
                {
                    blips.Add(element);
                }
            }

            if (blips.Count == 0)
            {
                if (FindDescendant(drawing, "chart") != null)
                {
                    EmitText(sb, rp, "[Chart]", true);
                }
                else if (FindDescendant(drawing, "relIds") != null)
                {
                    EmitText(sb, rp, "[SmartArt]", true);
                }
                return;
            }

            foreach (OpenXmlElement blip in blips)
            {
                string id = AttrR(blip, "embed");
                if (blips.Count == 1)
                {
                    EmitImage(sb, rp, id, (int)(cx / 635), (int)(cy / 635));
                }
                else
                {
                    EmitImage(sb, rp, id, 0, 0);
                }
            }
        }

        private void RenderVml(OpenXmlElement pict, StringBuilder sb, RunProps rp)
        {
            OpenXmlElement textBox = FindDescendant(pict, "txbxContent");

            if (textBox != null)
            {
                RenderTextBox(textBox, sb);
            }

            foreach (OpenXmlElement element in pict.Descendants())
            {
                if (element.LocalName != "imagedata")
                {
                    continue;
                }

                string id = AttrR(element, "id") ?? AttrAny(element, "relid");
                int width = 0, height = 0;

                if (element.Parent != null)
                {
                    string style = AttrAny(element.Parent, "style");
                    if (style != null)
                    {
                        foreach (string declaration in style.Split(';'))
                        {
                            int colon = declaration.IndexOf(':');
                            if (colon <= 0)
                            {
                                continue;
                            }
                            string name = declaration.Substring(0, colon).Trim().ToLowerInvariant();
                            string value = declaration.Substring(colon + 1).Trim();
                            if (name == "width")
                            {
                                width = ParseTwips(value, 0);
                            }
                            else if (name == "height")
                            {
                                height = ParseTwips(value, 0);
                            }
                        }
                    }
                }

                EmitImage(sb, rp, id, width, height);
            }
        }

        private void RenderTextBox(OpenXmlElement textBox, StringBuilder sb)
        {
            sb.Append("\\line ");
            RenderBlocksInline(textBox, sb, new InlineState(), "\\line ");
            sb.Append("\\line ");
        }

        private void EmitImage(StringBuilder sb, RunProps rp, string relationshipId, int widthTwips, int heightTwips)
        {
            ImagePart imagePart = GetPart(currentPart, relationshipId) as ImagePart;

            if (imagePart == null)
            {
                return;
            }

            byte[] data;
            string blipType;
            int pixelWidth, pixelHeight;

            try
            {
                using (Stream stream = imagePart.GetStream(FileMode.Open, FileAccess.Read))
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    data = memory.ToArray();
                }

                string contentType = (imagePart.ContentType ?? "").ToLowerInvariant();

                using (MemoryStream memory = new MemoryStream(data))
                using (Image image = Image.FromStream(memory))
                {
                    pixelWidth = Math.Max(1, image.Width);
                    pixelHeight = Math.Max(1, image.Height);

                    if (widthTwips <= 0 || heightTwips <= 0)
                    {
                        float dpiX = image.HorizontalResolution > 1 ? image.HorizontalResolution : 96f;
                        float dpiY = image.VerticalResolution > 1 ? image.VerticalResolution : 96f;
                        widthTwips = (int)(pixelWidth * 1440f / dpiX);
                        heightTwips = (int)(pixelHeight * 1440f / dpiY);
                    }

                    if (contentType == "image/png")
                    {
                        blipType = "\\pngblip";
                    }
                    else if (contentType == "image/jpeg" || contentType == "image/jpg" || contentType == "image/pjpeg")
                    {
                        blipType = "\\jpegblip";
                    }
                    else
                    {
                        // Other formats (EMF, WMF, GIF, BMP, TIFF, ...) are converted to PNG.
                        int width = Math.Max(1, Math.Min(4000, widthTwips / 15));
                        int height = Math.Max(1, Math.Min(4000, heightTwips / 15));

                        if (!(image is Metafile))
                        {
                            width = Math.Min(4000, pixelWidth);
                            height = Math.Min(4000, pixelHeight);
                        }

                        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.Clear(Color.Transparent);
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                graphics.SmoothingMode = SmoothingMode.HighQuality;
                                graphics.DrawImage(image, 0, 0, width, height);
                            }

                            using (MemoryStream png = new MemoryStream())
                            {
                                bitmap.Save(png, ImageFormat.Png);
                                data = png.ToArray();
                            }
                        }

                        pixelWidth = width;
                        pixelHeight = height;
                        blipType = "\\pngblip";
                    }
                }
            }
            catch (Exception)
            {
                EmitText(sb, rp, "[Image]", true);
                return;
            }

            sb.Append("{\\pict").Append(blipType);
            sb.Append("\\picw").Append(pixelWidth).Append("\\pich").Append(pixelHeight);
            sb.Append("\\picwgoal").Append(widthTwips).Append("\\pichgoal").Append(heightTwips);
            sb.Append("\r\n");

            const string hex = "0123456789abcdef";
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(hex[data[i] >> 4]).Append(hex[data[i] & 0x0F]);
                if (i % 64 == 63)
                {
                    sb.Append("\r\n");
                }
            }

            sb.Append('}');
        }

        #endregion

        #region Text output

        private void EmitText(StringBuilder sb, RunProps rp, string text, bool ignoreFields)
        {
            if (string.IsNullOrEmpty(text) || rp.Hidden || (!ignoreFields && IsFieldCodeHidden()))
            {
                return;
            }

            RunProps effective = rp;

            if (rp.Caps || rp.SmallCaps)
            {
                text = text.ToUpper(CultureInfo.CurrentCulture);
                if (rp.SmallCaps && !rp.Caps)
                {
                    effective = rp.Clone();
                    effective.HalfPoints = Math.Max(2, rp.HalfPoints * 4 / 5);
                }
            }

            if (IsSymbolFont(effective.Font))
            {
                sb.Append('{').Append(RunCodes(effective, 2)).Append(' ');
                AppendEscaped(sb, text, true);
                sb.Append('}');
                return;
            }

            // The text is split into parts that need different character sets.
            int start = 0;
            int currentCharset = -1;

            for (int i = 0; i <= text.Length; i++)
            {
                int charset = i < text.Length ? GetCharset(text[i]) : -2;

                if (charset == -1)
                {
                    continue;
                }

                if (currentCharset == -1 && charset != -2)
                {
                    currentCharset = charset;
                    continue;
                }

                if (charset != currentCharset)
                {
                    EmitSegment(sb, effective, text.Substring(start, i - start), currentCharset == -1 ? 0 : currentCharset);
                    start = i;
                    currentCharset = charset;
                }
            }
        }

        private void EmitSegment(StringBuilder sb, RunProps rp, string text, int charset)
        {
            if (text.Length == 0)
            {
                return;
            }

            sb.Append('{').Append(RunCodes(rp, charset)).Append(' ');
            AppendEscaped(sb, text, false);
            sb.Append('}');
        }

        private string RunCodes(RunProps rp, int charset)
        {
            string font = rp.Font;

            if (charset == 177 || charset == 178 || charset == 222)
            {
                font = rp.FontComplex ?? font;
            }
            else if (charset == 128 || charset == 129 || charset == 134)
            {
                font = rp.FontEastAsia ?? font;
            }

            StringBuilder codes = new StringBuilder();
            codes.Append("\\f").Append(GetFontIndex(font, charset));
            codes.Append("\\fs").Append(Math.Max(2, rp.HalfPoints));

            if (rp.Bold) codes.Append("\\b");
            if (rp.Italic) codes.Append("\\i");
            if (rp.Underline) codes.Append("\\ul");
            if (rp.Strike) codes.Append("\\strike");
            if (rp.VertAlign == 1) codes.Append("\\super");
            if (rp.VertAlign == 2) codes.Append("\\sub");
            if (rp.ForeColor.HasValue) codes.Append("\\cf").Append(GetColorIndex(rp.ForeColor.Value));

            Color? background = rp.Highlight ?? rp.Shading;
            if (background.HasValue) codes.Append("\\highlight").Append(GetColorIndex(background.Value));

            return codes.ToString();
        }

        private static void AppendEscaped(StringBuilder sb, string text, bool symbolFont)
        {
            foreach (char ch in text)
            {
                switch (ch)
                {
                    case '\\':
                        sb.Append("\\\\");
                        continue;
                    case '{':
                        sb.Append("\\{");
                        continue;
                    case '}':
                        sb.Append("\\}");
                        continue;
                    case '\t':
                        sb.Append("\\tab ");
                        continue;
                    case '\n':
                        sb.Append("\\line ");
                        continue;
                    case '\r':
                        continue;
                }

                if (symbolFont && ((ch >= 0xF000 && ch <= 0xF0FF) || (ch >= 0x80 && ch <= 0xFF)))
                {
                    sb.Append("\\'").Append(((int)(ch & 0xFF)).ToString("x2", CultureInfo.InvariantCulture));
                }
                else if (ch < 0x20)
                {
                    // Control characters are not shown.
                }
                else if (ch < 0x80)
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.Append("\\u").Append(((short)ch).ToString(CultureInfo.InvariantCulture)).Append('?');
                }
            }
        }

        private static int GetCharset(char ch)
        {
            if (ch < 0x100) return -1;                                  // ASCII and Latin-1
            if (ch == 0x011E || ch == 0x011F || ch == 0x0130 || ch == 0x0131 || ch == 0x015E || ch == 0x015F) return 162; // Turkish
            if (ch <= 0x024F) return 238;                               // Central European
            if (ch >= 0x0370 && ch <= 0x03FF) return 161;               // Greek
            if (ch >= 0x0400 && ch <= 0x04FF) return 204;               // Cyrillic
            if (ch >= 0x0590 && ch <= 0x05FF) return 177;               // Hebrew
            if (ch >= 0x0600 && ch <= 0x06FF) return 178;               // Arabic
            if (ch >= 0x0E00 && ch <= 0x0E7F) return 222;               // Thai
            if (ch >= 0x2000 && ch <= 0x2BFF) return -1;                // Punctuation and symbols
            if (ch >= 0x3040 && ch <= 0x30FF) return 128;               // Japanese
            if (ch >= 0xAC00 && ch <= 0xD7AF) return 129;               // Korean
            if (ch >= 0x4E00 && ch <= 0x9FFF) return 134;               // Chinese
            return 1;
        }

        private static bool IsSymbolFont(string font)
        {
            if (font == null)
            {
                return false;
            }

            switch (font.Trim().ToLowerInvariant())
            {
                case "symbol":
                case "wingdings":
                case "wingdings 2":
                case "wingdings 3":
                case "webdings":
                case "marlett":
                case "mt extra":
                case "zapfdingbats":
                    return true;
                default:
                    return false;
            }
        }

        private int GetFontIndex(string font, int charset)
        {
            if (string.IsNullOrEmpty(font))
            {
                font = defaultRunProps.Font ?? "Times New Roman";
            }

            if (IsSymbolFont(font))
            {
                charset = 2;
            }

            string key = font + "|" + charset.ToString(CultureInfo.InvariantCulture);
            int index;

            if (!fontIndexes.TryGetValue(key, out index))
            {
                index = fontNames.Count;
                fontNames.Add(font);
                fontCharsets.Add(charset);
                fontIndexes.Add(key, index);
            }

            return index;
        }

        private int GetColorIndex(Color color)
        {
            int key = color.ToArgb();
            int index;

            if (!colorIndexes.TryGetValue(key, out index))
            {
                colors.Add(color);
                index = colors.Count; // index 0 is the automatic color
                colorIndexes.Add(key, index);
            }

            return index;
        }

        #endregion

        #region Numbering

        private void EmitNumber(StringBuilder sb, LevelInfo level, string numId, int ilvl, RunProps markRun, bool inline)
        {
            string abstractId = ResolveAbstract(numToAbstract[numId]);
            int[] counters;

            if (!listCounters.TryGetValue(abstractId, out counters))
            {
                counters = new int[9];
                for (int i = 0; i < counters.Length; i++)
                {
                    counters[i] = UNSET;
                }
                listCounters.Add(abstractId, counters);
            }

            if (seenNumIds.Add(numId))
            {
                Dictionary<int, int> overrides;
                if (numStartOverrides.TryGetValue(numId, out overrides))
                {
                    foreach (int overriddenLevel in overrides.Keys)
                    {
                        for (int i = overriddenLevel; i < counters.Length; i++)
                        {
                            counters[i] = UNSET;
                        }
                    }
                }
            }

            if (counters[ilvl] == UNSET)
            {
                counters[ilvl] = GetStart(numId, ilvl);
            }
            else
            {
                counters[ilvl]++;
            }

            for (int i = ilvl + 1; i < counters.Length; i++)
            {
                counters[i] = UNSET;
            }

            StringBuilder text = new StringBuilder();
            string pattern = level.Text ?? "";

            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == '%' && i + 1 < pattern.Length && pattern[i + 1] >= '1' && pattern[i + 1] <= '9')
                {
                    int k = pattern[i + 1] - '1';
                    int value = counters[k] == UNSET ? GetStart(numId, k) : counters[k];
                    LevelInfo other = GetLevel(numId, k);
                    string format = other != null ? other.Format : "decimal";

                    if (level.IsLegal && k != ilvl)
                    {
                        format = "decimal";
                    }

                    text.Append(FormatNumber(value, format));
                    i++;
                }
                else
                {
                    text.Append(pattern[i]);
                }
            }

            if (text.Length == 0)
            {
                return;
            }

            RunProps numberRun = markRun.Clone();
            ApplyRunProps(numberRun, level.RPr);
            numberRun.Underline = numberRun.Underline && level.RPr != null && Child(level.RPr, "u") != null;
            EmitText(sb, numberRun, text.ToString(), true);

            if (level.Suffix == "space" || (inline && level.Suffix != "nothing"))
            {
                EmitText(sb, numberRun, " ", true);
            }
            else if (level.Suffix != "nothing")
            {
                EmitText(sb, numberRun, "\t", true);
            }
        }

        private int GetStart(string numId, int ilvl)
        {
            Dictionary<int, int> overrides;
            int start;

            if (numStartOverrides.TryGetValue(numId, out overrides) && overrides.TryGetValue(ilvl, out start))
            {
                return start;
            }

            LevelInfo level = GetLevel(numId, ilvl);
            return level != null ? level.Start : 1;
        }

        private LevelInfo GetLevel(string numId, int ilvl)
        {
            string abstractId;

            if (numId == null || !numToAbstract.TryGetValue(numId, out abstractId))
            {
                return null;
            }

            Dictionary<int, LevelInfo> levels;
            LevelInfo level;

            if (numLevelOverrides.TryGetValue(numId, out levels) && levels.TryGetValue(ilvl, out level))
            {
                return level;
            }

            if (abstractLevels.TryGetValue(ResolveAbstract(abstractId), out levels) && levels.TryGetValue(ilvl, out level))
            {
                return level;
            }

            if (abstractLevels.TryGetValue(abstractId, out levels) && levels.TryGetValue(ilvl, out level))
            {
                return level;
            }

            return null;
        }

        private string ResolveAbstract(string abstractId)
        {
            for (int guard = 0; guard < 5; guard++)
            {
                string styleId;
                StyleInfo style;

                if (!abstractStyleLinks.TryGetValue(abstractId, out styleId) || !styles.TryGetValue(styleId, out style))
                {
                    break;
                }

                OpenXmlElement numPr = Child(style.PPr, "numPr");
                string linkedNumId = ValOf(numPr, "numId");
                string linkedAbstract;

                if (linkedNumId == null || !numToAbstract.TryGetValue(linkedNumId, out linkedAbstract) || linkedAbstract == abstractId)
                {
                    break;
                }

                abstractId = linkedAbstract;
            }

            return abstractId;
        }

        private static string FormatNumber(int value, string format)
        {
            switch (format)
            {
                case "bullet":
                case "none":
                    return "";
                case "decimalZero":
                    return value >= 0 && value < 10 ? "0" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
                case "upperRoman":
                    return ToRoman(value);
                case "lowerRoman":
                    return ToRoman(value).ToLowerInvariant();
                case "upperLetter":
                    return ToLetters(value);
                case "lowerLetter":
                    return ToLetters(value).ToLowerInvariant();
                case "ordinal":
                    return value.ToString(CultureInfo.InvariantCulture) + OrdinalSuffix(value);
                default:
                    return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string ToRoman(int value)
        {
            if (value <= 0 || value >= 4000)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            int[] numbers = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] symbols = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < numbers.Length; i++)
            {
                while (value >= numbers[i])
                {
                    result.Append(symbols[i]);
                    value -= numbers[i];
                }
            }

            return result.ToString();
        }

        private static string ToLetters(int value)
        {
            if (value <= 0)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            char letter = (char)('A' + (value - 1) % 26);
            return new string(letter, (value - 1) / 26 + 1);
        }

        private static string OrdinalSuffix(int value)
        {
            int lastTwo = Math.Abs(value) % 100;

            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return "th";
            }

            switch (Math.Abs(value) % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        #endregion

        #region Properties

        private string GetParagraphStyleId(OpenXmlElement pPr)
        {
            return ValOf(pPr, "pStyle") ?? defaultParagraphStyle;
        }

        private ParaProps BuildParaProps(OpenXmlElement pPr, string styleId, out LevelInfo level, out string numId, out int ilvl)
        {
            ParaProps styleProps = defaultParaProps.Clone();

            foreach (StyleInfo style in GetStyleChain(styleId))
            {
                ApplyParaProps(styleProps, style.PPr);
            }

            ParaProps full = styleProps.Clone();
            ApplyParaProps(full, pPr);

            level = null;
            numId = null;
            ilvl = 0;

            if (full.NumId != null && full.NumId != "0")
            {
                int levelIndex = Math.Max(0, Math.Min(8, full.Ilvl));
                LevelInfo found = GetLevel(full.NumId, levelIndex);

                if (found != null)
                {
                    level = found;
                    numId = full.NumId;
                    ilvl = levelIndex;

                    // Order: style, numbering level and then direct formatting.
                    ApplyParaProps(styleProps, found.PPr);
                    ApplyParaProps(styleProps, pPr);
                    return styleProps;
                }
            }

            return full;
        }

        private RunProps BuildBaseRunProps(string styleId)
        {
            string tableStyle = tableStyleStack.Count > 0 ? tableStyleStack[tableStyleStack.Count - 1] : null;
            string key = (tableStyle ?? "") + "|" + (styleId ?? "");
            RunProps result;

            if (!baseRunCache.TryGetValue(key, out result))
            {
                result = defaultRunProps.Clone();

                foreach (StyleInfo style in GetStyleChain(tableStyle))
                {
                    ApplyRunProps(result, style.RPr);
                }

                foreach (StyleInfo style in GetStyleChain(styleId))
                {
                    ApplyRunProps(result, style.RPr);
                }

                baseRunCache.Add(key, result);
            }

            return result.Clone();
        }

        private void ApplyParaProps(ParaProps pp, OpenXmlElement pPr)
        {
            if (pPr == null)
            {
                return;
            }

            foreach (OpenXmlElement child in pPr.ChildElements)
            {
                if (!IsWNamespace(child))
                {
                    continue;
                }

                switch (child.LocalName)
                {
                    case "jc":
                        pp.Justification = Attr(child, "val") ?? pp.Justification;
                        break;

                    case "ind":
                        string left = Attr(child, "left") ?? Attr(child, "start");
                        string right = Attr(child, "right") ?? Attr(child, "end");
                        string firstLine = Attr(child, "firstLine");
                        string hanging = Attr(child, "hanging");
                        if (left != null) pp.Left = ParseTwips(left, pp.Left);
                        if (right != null) pp.Right = ParseTwips(right, pp.Right);
                        if (firstLine != null) pp.FirstLine = ParseTwips(firstLine, 0);
                        if (hanging != null) pp.FirstLine = -ParseTwips(hanging, 0);
                        break;

                    case "spacing":
                        string before = Attr(child, "before");
                        string after = Attr(child, "after");
                        string line = Attr(child, "line");
                        string lineRule = Attr(child, "lineRule");
                        if (before != null) pp.Before = ParseTwips(before, pp.Before);
                        if (after != null) pp.After = ParseTwips(after, pp.After);
                        if (IsTrue(Attr(child, "beforeAutospacing"))) pp.Before = 280;
                        if (IsTrue(Attr(child, "afterAutospacing"))) pp.After = 280;
                        if (line != null)
                        {
                            pp.Line = ParseTwips(line, pp.Line);
                            pp.LineRule = lineRule ?? "auto";
                        }
                        else if (lineRule != null)
                        {
                            pp.LineRule = lineRule;
                        }
                        break;

                    case "contextualSpacing":
                        pp.ContextualSpacing = OnOff(child);
                        break;

                    case "bidi":
                        pp.Bidi = OnOff(child);
                        break;

                    case "numPr":
                        string numId = ValOf(child, "numId");
                        string ilvl = ValOf(child, "ilvl");
                        if (numId != null) pp.NumId = numId;
                        if (ilvl != null) pp.Ilvl = ParseInt(ilvl, 0);
                        break;

                    case "tabs":
                        foreach (OpenXmlElement tab in child.ChildElements)
                        {
                            if (!IsW(tab, "tab"))
                            {
                                continue;
                            }

                            int position = ParseTwips(Attr(tab, "pos"), int.MinValue);
                            if (position == int.MinValue)
                            {
                                continue;
                            }

                            pp.Tabs.RemoveAll(delegate(TabStop t) { return t.Position == position; });

                            string kind = Attr(tab, "val") ?? "left";
                            if (kind != "clear")
                            {
                                TabStop stop = new TabStop();
                                stop.Position = position;
                                stop.Kind = kind;
                                stop.Leader = Attr(tab, "leader");
                                pp.Tabs.Add(stop);
                            }
                        }
                        break;
                }
            }
        }

        private void ApplyRunProps(RunProps rp, OpenXmlElement rPr)
        {
            if (rPr == null)
            {
                return;
            }

            foreach (OpenXmlElement child in rPr.ChildElements)
            {
                if (!IsWNamespace(child))
                {
                    continue;
                }

                switch (child.LocalName)
                {
                    case "b":
                        rp.Bold = OnOff(child);
                        break;
                    case "i":
                        rp.Italic = OnOff(child);
                        break;
                    case "u":
                        string underline = Attr(child, "val");
                        rp.Underline = underline != "none";
                        break;
                    case "strike":
                    case "dstrike":
                        rp.Strike = OnOff(child);
                        break;
                    case "caps":
                        rp.Caps = OnOff(child);
                        break;
                    case "smallCaps":
                        rp.SmallCaps = OnOff(child);
                        break;
                    case "vanish":
                        rp.Hidden = OnOff(child);
                        break;
                    case "vertAlign":
                        string align = Attr(child, "val");
                        rp.VertAlign = align == "superscript" ? 1 : align == "subscript" ? 2 : 0;
                        break;
                    case "sz":
                        rp.HalfPoints = ParseHalfPoints(Attr(child, "val"), rp.HalfPoints);
                        break;
                    case "rFonts":
                        ApplyFonts(rp, child);
                        break;
                    case "color":
                        string color = Attr(child, "val");
                        if (color != null)
                        {
                            rp.ForeColor = ParseColor(color);
                        }
                        break;
                    case "highlight":
                        string highlight = Attr(child, "val") ?? "none";
                        Color named;
                        rp.Highlight = highlightColors.TryGetValue(highlight, out named) ? (Color?)named : null;
                        break;
                    case "shd":
                        rp.Shading = ParseColor(Attr(child, "fill"));
                        break;
                }
            }
        }

        private void ApplyFonts(RunProps rp, OpenXmlElement rFonts)
        {
            string ascii = ThemeFont(Attr(rFonts, "asciiTheme")) ?? Attr(rFonts, "ascii") ??
                           ThemeFont(Attr(rFonts, "hAnsiTheme")) ?? Attr(rFonts, "hAnsi");
            string eastAsia = ThemeFont(Attr(rFonts, "eastAsiaTheme")) ?? Attr(rFonts, "eastAsia");
            string complex = ThemeFont(Attr(rFonts, "cstheme")) ?? Attr(rFonts, "cs");

            if (!string.IsNullOrEmpty(ascii)) rp.Font = ascii;
            if (!string.IsNullOrEmpty(eastAsia)) rp.FontEastAsia = eastAsia;
            if (!string.IsNullOrEmpty(complex)) rp.FontComplex = complex;
        }

        private string ThemeFont(string theme)
        {
            if (string.IsNullOrEmpty(theme))
            {
                return null;
            }

            bool major = theme.StartsWith("major", StringComparison.Ordinal);

            if (theme.EndsWith("EastAsia", StringComparison.Ordinal))
            {
                return (major ? majorFontEastAsia : minorFontEastAsia) ?? (major ? majorFont : minorFont);
            }

            if (theme.EndsWith("Bidi", StringComparison.Ordinal))
            {
                return (major ? majorFontComplex : minorFontComplex) ?? (major ? majorFont : minorFont);
            }

            return major ? majorFont : minorFont;
        }

        #endregion

        #region Loading of document parts

        private void LoadTheme()
        {
            ThemePart theme = mainPart.ThemePart;

            if (theme == null || theme.RootElement == null)
            {
                return;
            }

            foreach (OpenXmlElement element in theme.RootElement.Descendants())
            {
                bool major = element.LocalName == "majorFont";

                if (!major && element.LocalName != "minorFont")
                {
                    continue;
                }

                foreach (OpenXmlElement font in element.ChildElements)
                {
                    string typeface = AttrAny(font, "typeface");

                    if (string.IsNullOrEmpty(typeface))
                    {
                        continue;
                    }

                    switch (font.LocalName)
                    {
                        case "latin":
                            if (major) majorFont = typeface; else minorFont = typeface;
                            break;
                        case "ea":
                            if (major) majorFontEastAsia = typeface; else minorFontEastAsia = typeface;
                            break;
                        case "cs":
                            if (major) majorFontComplex = typeface; else minorFontComplex = typeface;
                            break;
                    }
                }
            }
        }

        private void LoadStyles()
        {
            OpenXmlPart stylesPart = mainPart.StyleDefinitionsPart;

            if (stylesPart == null || stylesPart.RootElement == null)
            {
                stylesPart = mainPart.StylesWithEffectsPart;
            }

            if (stylesPart == null || stylesPart.RootElement == null)
            {
                return;
            }

            foreach (OpenXmlElement child in stylesPart.RootElement.ChildElements)
            {
                if (IsW(child, "docDefaults"))
                {
                    ApplyRunProps(defaultRunProps, Child(Child(child, "rPrDefault"), "rPr"));
                    ApplyParaProps(defaultParaProps, Child(Child(child, "pPrDefault"), "pPr"));
                }
                else if (IsW(child, "style"))
                {
                    StyleInfo style = new StyleInfo();
                    style.Id = Attr(child, "styleId");
                    style.Type = Attr(child, "type") ?? "paragraph";
                    style.BasedOn = ValOf(child, "basedOn");
                    style.PPr = Child(child, "pPr");
                    style.RPr = Child(child, "rPr");
                    style.TblPr = Child(child, "tblPr");

                    if (style.Id == null || styles.ContainsKey(style.Id))
                    {
                        continue;
                    }

                    styles.Add(style.Id, style);

                    if (IsTrue(Attr(child, "default")))
                    {
                        if (style.Type == "paragraph" && defaultParagraphStyle == null) defaultParagraphStyle = style.Id;
                        else if (style.Type == "character" && defaultCharacterStyle == null) defaultCharacterStyle = style.Id;
                        else if (style.Type == "table" && defaultTableStyle == null) defaultTableStyle = style.Id;
                    }
                }
            }
        }

        private List<StyleInfo> GetStyleChain(string styleId)
        {
            List<StyleInfo> chain = new List<StyleInfo>();
            StyleInfo style;

            while (styleId != null && chain.Count < 20 && styles.TryGetValue(styleId, out style))
            {
                if (chain.Contains(style))
                {
                    break;
                }
                chain.Insert(0, style);
                styleId = style.BasedOn;
            }

            return chain;
        }

        private void LoadNumbering()
        {
            NumberingDefinitionsPart numberingPart = mainPart.NumberingDefinitionsPart;

            if (numberingPart == null || numberingPart.RootElement == null)
            {
                return;
            }

            foreach (OpenXmlElement child in numberingPart.RootElement.ChildElements)
            {
                if (IsW(child, "abstractNum"))
                {
                    string id = Attr(child, "abstractNumId");
                    if (id == null || abstractLevels.ContainsKey(id))
                    {
                        continue;
                    }

                    Dictionary<int, LevelInfo> levels = new Dictionary<int, LevelInfo>();

                    foreach (OpenXmlElement lvl in child.ChildElements)
                    {
                        if (IsW(lvl, "lvl"))
                        {
                            int index = ParseInt(Attr(lvl, "ilvl"), 0);
                            if (!levels.ContainsKey(index))
                            {
                                levels.Add(index, ParseLevel(lvl, null));
                            }
                        }
                    }

                    abstractLevels.Add(id, levels);

                    string styleLink = ValOf(child, "numStyleLink");
                    if (styleLink != null)
                    {
                        abstractStyleLinks[id] = styleLink;
                    }
                }
                else if (IsW(child, "num"))
                {
                    string numId = Attr(child, "numId");
                    string abstractId = ValOf(child, "abstractNumId");

                    if (numId == null || abstractId == null || numToAbstract.ContainsKey(numId))
                    {
                        continue;
                    }

                    numToAbstract.Add(numId, abstractId);

                    foreach (OpenXmlElement levelOverride in child.ChildElements)
                    {
                        if (!IsW(levelOverride, "lvlOverride"))
                        {
                            continue;
                        }

                        int index = ParseInt(Attr(levelOverride, "ilvl"), 0);
                        string start = ValOf(levelOverride, "startOverride");
                        OpenXmlElement lvl = Child(levelOverride, "lvl");

                        if (start != null)
                        {
                            if (!numStartOverrides.ContainsKey(numId))
                            {
                                numStartOverrides.Add(numId, new Dictionary<int, int>());
                            }
                            numStartOverrides[numId][index] = ParseInt(start, 1);
                        }

                        if (lvl != null)
                        {
                            if (!numLevelOverrides.ContainsKey(numId))
                            {
                                numLevelOverrides.Add(numId, new Dictionary<int, LevelInfo>());
                            }
                            numLevelOverrides[numId][index] = ParseLevel(lvl, null);
                        }
                    }
                }
            }
        }

        private static LevelInfo ParseLevel(OpenXmlElement lvl, LevelInfo baseLevel)
        {
            LevelInfo level = new LevelInfo();

            if (baseLevel != null)
            {
                level.Start = baseLevel.Start;
                level.Format = baseLevel.Format;
                level.Text = baseLevel.Text;
                level.Suffix = baseLevel.Suffix;
            }

            string start = ValOf(lvl, "start");
            string format = ValOf(lvl, "numFmt");
            OpenXmlElement text = Child(lvl, "lvlText");
            string suffix = ValOf(lvl, "suff");

            if (start != null) level.Start = ParseInt(start, 1);
            if (format != null) level.Format = format;
            if (text != null) level.Text = Attr(text, "val") ?? "";
            if (suffix != null) level.Suffix = suffix;

            level.IsLegal = OnOff(Child(lvl, "isLgl"));
            level.PPr = Child(lvl, "pPr");
            level.RPr = Child(lvl, "rPr");
            return level;
        }

        private void LoadSettings()
        {
            DocumentSettingsPart settings = mainPart.DocumentSettingsPart;

            if (settings == null || settings.RootElement == null)
            {
                return;
            }

            int tab = ParseTwips(ValOf(settings.RootElement, "defaultTabStop"), 0);
            if (tab > 0)
            {
                defaultTab = tab;
            }
        }

        #endregion

        #region XML helpers

        private static bool IsWNamespace(OpenXmlElement element)
        {
            return element.NamespaceUri == W || element.NamespaceUri == W_STRICT;
        }

        private static bool IsW(OpenXmlElement element, string localName)
        {
            return element != null && element.LocalName == localName && IsWNamespace(element);
        }

        private static bool IsAlternateContent(OpenXmlElement element)
        {
            return element.LocalName == "AlternateContent" && element.NamespaceUri == MC;
        }

        private static OpenXmlElement ChooseAlternate(OpenXmlElement alternateContent)
        {
            OpenXmlElement firstChoice = null;

            foreach (OpenXmlElement child in alternateContent.ChildElements)
            {
                if (child.NamespaceUri != MC)
                {
                    continue;
                }

                if (child.LocalName == "Fallback")
                {
                    return child;
                }

                if (child.LocalName == "Choice" && firstChoice == null)
                {
                    firstChoice = child;
                }
            }

            return firstChoice;
        }

        private static OpenXmlElement Child(OpenXmlElement element, string localName)
        {
            if (element == null)
            {
                return null;
            }

            foreach (OpenXmlElement child in element.ChildElements)
            {
                if (IsW(child, localName))
                {
                    return child;
                }
            }

            return null;
        }

        private static OpenXmlElement FindDescendant(OpenXmlElement element, string localName)
        {
            foreach (OpenXmlElement descendant in element.Descendants())
            {
                if (descendant.LocalName == localName)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static string ValOf(OpenXmlElement element, string childName)
        {
            return Attr(Child(element, childName), "val");
        }

        private static string Attr(OpenXmlElement element, string localName)
        {
            if (element == null)
            {
                return null;
            }

            foreach (OpenXmlAttribute attribute in element.GetAttributes())
            {
                if (attribute.LocalName == localName &&
                    (string.IsNullOrEmpty(attribute.NamespaceUri) || attribute.NamespaceUri == W || attribute.NamespaceUri == W_STRICT))
                {
                    return attribute.Value;
                }
            }

            return null;
        }

        private static string AttrR(OpenXmlElement element, string localName)
        {
            if (element == null)
            {
                return null;
            }

            foreach (OpenXmlAttribute attribute in element.GetAttributes())
            {
                if (attribute.LocalName == localName && (attribute.NamespaceUri == R || attribute.NamespaceUri == R_STRICT))
                {
                    return attribute.Value;
                }
            }

            return null;
        }

        private static string AttrAny(OpenXmlElement element, string localName)
        {
            if (element == null)
            {
                return null;
            }

            foreach (OpenXmlAttribute attribute in element.GetAttributes())
            {
                if (attribute.LocalName == localName)
                {
                    return attribute.Value;
                }
            }

            return null;
        }

        private static OpenXmlPart GetPart(OpenXmlPart owner, string relationshipId)
        {
            if (owner == null || string.IsNullOrEmpty(relationshipId))
            {
                return null;
            }

            try
            {
                return owner.GetPartById(relationshipId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool OnOff(OpenXmlElement element)
        {
            if (element == null)
            {
                return false;
            }

            string value = Attr(element, "val");
            return value == null || IsTrue(value);
        }

        private static bool IsTrue(string value)
        {
            return value == "1" || value == "true" || value == "on";
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : fallback;
        }

        /// <summary>
        /// Parses a measure in twentieths of a point. Strict documents can
        /// also contain universal measures such as "12pt" or "2.5cm".
        /// </summary>
        private static int ParseTwips(string value, int fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            value = value.Trim();
            double factor = 1;
            string[] units = { "pt", "in", "cm", "mm", "pc", "pi" };
            double[] factors = { 20, 1440, 1440 / 2.54, 144 / 2.54, 240, 240 };

            for (int i = 0; i < units.Length; i++)
            {
                if (value.EndsWith(units[i], StringComparison.OrdinalIgnoreCase))
                {
                    factor = factors[i];
                    value = value.Substring(0, value.Length - units[i].Length);
                    break;
                }
            }

            double number;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                return fallback;
            }

            return (int)Math.Round(number * factor);
        }

        private static int ParseHalfPoints(string value, int fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            if (char.IsLetter(value[value.Length - 1]))
            {
                // Universal measure: twips / 10 = half points.
                int twips = ParseTwips(value, int.MinValue);
                return twips == int.MinValue ? fallback : Math.Max(2, twips / 10);
            }

            double number;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                ? Math.Max(2, (int)Math.Round(number)) : fallback;
        }

        private static Color? ParseColor(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "auto" || value.Length != 6)
            {
                return null;
            }

            int rgb;
            if (!int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
            {
                return null;
            }

            return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        private static Dictionary<string, Color> CreateHighlightColors()
        {
            Dictionary<string, Color> result = new Dictionary<string, Color>();
            result.Add("yellow", Color.FromArgb(255, 255, 0));
            result.Add("green", Color.FromArgb(0, 255, 0));
            result.Add("cyan", Color.FromArgb(0, 255, 255));
            result.Add("magenta", Color.FromArgb(255, 0, 255));
            result.Add("blue", Color.FromArgb(0, 0, 255));
            result.Add("red", Color.FromArgb(255, 0, 0));
            result.Add("darkBlue", Color.FromArgb(0, 0, 128));
            result.Add("darkCyan", Color.FromArgb(0, 128, 128));
            result.Add("darkGreen", Color.FromArgb(0, 128, 0));
            result.Add("darkMagenta", Color.FromArgb(128, 0, 128));
            result.Add("darkRed", Color.FromArgb(128, 0, 0));
            result.Add("darkYellow", Color.FromArgb(128, 128, 0));
            result.Add("darkGray", Color.FromArgb(128, 128, 128));
            result.Add("lightGray", Color.FromArgb(192, 192, 192));
            result.Add("black", Color.FromArgb(0, 0, 0));
            result.Add("white", Color.FromArgb(255, 255, 255));
            return result;
        }

        #endregion
    }
}

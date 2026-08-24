using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CreateEnv.Ecosystem.Memory
{
    // A small PDF writer.
    //
    // The design document describes composing the report as HTML and rendering it to
    // PDF on the device. Unity has no HTML renderer, and the platform ones live
    // behind a native plugin that can only be exercised on a phone. Since almost
    // every real bug in this feature was caught by running things headlessly, the
    // report is built here instead: no plugin, no dependency, it runs in the Editor,
    // and a test can generate a real file and check it.
    //
    // Only what a lab report needs — text, wrapped paragraphs, rules, filled
    // rectangles and lines. Type is Helvetica, one of the fourteen fonts every PDF
    // reader is required to have, so nothing is embedded and the file stays tiny.
    public class PdfWriter
    {
        // A4 in points.
        public const float PageWidth = 595f;
        public const float PageHeight = 842f;
        public const float Margin = 48f;
        public const float ContentWidth = PageWidth - Margin * 2f;

        // One line of the baseline grid.
        //
        // Every line of text and every gap is a whole number of these, whatever the
        // font size. Mixed leading is what makes a document look assembled out of
        // fragments — the eye reads uneven spacing as unrelated blocks even when the
        // words say otherwise.
        public const float Line = 13f;

        readonly List<StringBuilder> _pages = new List<StringBuilder>();
        StringBuilder _page;

        // The pen, in PDF coordinates: y counts up from the bottom of the page.
        public float Cursor { get; private set; }

        public int PageCount => _pages.Count;

        public PdfWriter()
        {
            NewPage();
        }

        public void NewPage()
        {
            _page = new StringBuilder(4096);
            _pages.Add(_page);
            Cursor = PageHeight - Margin;
        }

        // Starts a new page when the next block would not fit on this one.
        public void EnsureRoom(float height)
        {
            if (Cursor - height < Margin) NewPage();
        }

        public void Space(float points) => Cursor -= points;

        // ── Drawing ──────────────────────────────────────────────────────────

        public void SetFill(Color c) =>
            _page.Append(F(c.r)).Append(' ').Append(F(c.g)).Append(' ').Append(F(c.b)).Append(" rg\n");

        public void SetStroke(Color c) =>
            _page.Append(F(c.r)).Append(' ').Append(F(c.g)).Append(' ').Append(F(c.b)).Append(" RG\n");

        public void Rect(float x, float y, float w, float h, Color fill)
        {
            SetFill(fill);
            _page.Append(F(x)).Append(' ').Append(F(y)).Append(' ')
                 .Append(F(w)).Append(' ').Append(F(h)).Append(" re f\n");
        }

        public void Stroke(float x1, float y1, float x2, float y2, Color colour, float width = 0.8f)
        {
            SetStroke(colour);
            _page.Append(F(width)).Append(" w\n")
                 .Append(F(x1)).Append(' ').Append(F(y1)).Append(" m ")
                 .Append(F(x2)).Append(' ').Append(F(y2)).Append(" l S\n");
        }

        // One line of text at an exact position. Does not move the cursor.
        public void TextAt(float x, float y, string text, float size, bool bold, Color colour)
        {
            if (string.IsNullOrEmpty(text)) return;
            SetFill(colour);
            _page.Append("BT /").Append(bold ? "F2" : "F1").Append(' ').Append(F(size))
                 .Append(" Tf ").Append(F(x)).Append(' ').Append(F(y)).Append(" Td (")
                 .Append(Escape(text)).Append(") Tj ET\n");
        }

        // A heading, with the space above and below it that makes a document
        // readable rather than a wall.
        //
        // `keepWith` is room reserved for whatever follows. A heading stranded at the
        // foot of a page, with its section starting overleaf, reads as though the
        // section is missing — so a heading only lands here if something can land
        // under it.
        public void Heading(string text, float size = 12.5f, float keepWith = 3f * Line)
        {
            EnsureRoom(Line * 2.5f + keepWith);
            Cursor -= Line * 1.5f;
            TextAt(Margin, Cursor, text, size, true, Ink);
            Cursor -= 6f;
            Stroke(Margin, Cursor, PageWidth - Margin, Cursor, Rule, 0.6f);
            Cursor -= Line - 6f;
        }

        // A wrapped paragraph, one grid line per line of text. Returns the height used.
        public float Paragraph(string text, float size = 9f, bool bold = false,
                               Color? colour = null, float indent = 0f)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            var lines = Wrap(text, size, bold, ContentWidth - indent);
            float used = 0f;

            foreach (var line in lines)
            {
                EnsureRoom(Line);
                Cursor -= Line;
                TextAt(Margin + indent, Cursor, line, size, bold, colour ?? Ink);
                used += Line;
            }
            return used;
        }

        // A label on the left and a value beside it, as a setup sheet reads.
        public void Field(string label, string value, float size = 9f, float valueAt = 132f)
        {
            EnsureRoom(Line);
            Cursor -= Line;
            TextAt(Margin, Cursor, label, size, false, Faint);
            TextAt(Margin + valueAt, Cursor, value, size, false, Ink);
        }

        // One row of a list: a colour chip, a name, and a figure further along.
        public void Row(Color chip, string name, string value, float size = 9f,
                        float valueAt = 190f)
        {
            EnsureRoom(Line);
            Cursor -= Line;
            Rect(Margin, Cursor + 1f, 6f, 6f, chip);
            TextAt(Margin + 13f, Cursor, name, size, false, Ink);
            if (!string.IsNullOrEmpty(value))
                TextAt(Margin + valueAt, Cursor, value, size, false, Faint);
        }

        // ── Text measurement ─────────────────────────────────────────────────

        // Helvetica advance widths, thousandths of an em, for printable ASCII.
        // Wrapping only needs to be close, but close is much better than assuming
        // every glyph is the same width, which makes ragged text look broken.
        static readonly short[] Widths =
        {
            278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,          // 32-47
            556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556,          // 48-63
            1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,         // 64-79
            667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,          // 80-95
            333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556,          // 96-111
            556,556,333,500,278,556,500,722,500,500,500,334,260,334,584,              // 112-126
        };

        public static float Measure(string text, float size, bool bold)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            int units = 0;
            foreach (char c in text)
            {
                int i = c - 32;
                units += (i >= 0 && i < Widths.Length) ? Widths[i] : 556;
            }
            // Bold Helvetica is a little wider; near enough for line breaking.
            return units / 1000f * size * (bold ? 1.06f : 1f);
        }

        public static List<string> Wrap(string text, float size, bool bold, float width)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Split('\n'))
            {
                if (paragraph.Length == 0) { lines.Add(""); continue; }

                var line = new StringBuilder();
                foreach (var word in paragraph.Split(' '))
                {
                    if (word.Length == 0) continue;
                    string candidate = line.Length == 0 ? word : line + " " + word;
                    if (Measure(candidate, size, bold) <= width || line.Length == 0)
                    {
                        if (line.Length > 0) line.Append(' ');
                        line.Append(word);
                    }
                    else
                    {
                        lines.Add(line.ToString());
                        line.Clear();
                        line.Append(word);
                    }
                }
                lines.Add(line.ToString());
            }
            return lines;
        }

        // ── Palette ──────────────────────────────────────────────────────────
        public static readonly Color Ink = new Color(0.11f, 0.14f, 0.17f);
        public static readonly Color Faint = new Color(0.42f, 0.48f, 0.54f);
        public static readonly Color Rule = new Color(0.80f, 0.84f, 0.87f);
        public static readonly Color Accent = new Color(0.17f, 0.66f, 0.29f);

        // ── Output ───────────────────────────────────────────────────────────

        public byte[] ToBytes()
        {
            var pdf = new StringBuilder(16384);
            var offsets = new List<int>();

            void Obj(int id, string body)
            {
                offsets.Add(pdf.Length);
                pdf.Append(id).Append(" 0 obj\n").Append(body).Append("\nendobj\n");
            }

            pdf.Append("%PDF-1.4\n");

            int pageCount = _pages.Count;
            int firstPageId = 4;
            int firstContentId = firstPageId + pageCount;
            int fontRegularId = firstContentId + pageCount;
            int fontBoldId = fontRegularId + 1;

            // 1 catalogue, 2 page tree, 3 unused placeholder kept so ids line up
            Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");

            var kids = new StringBuilder();
            for (int i = 0; i < pageCount; i++) kids.Append(firstPageId + i).Append(" 0 R ");
            Obj(2, $"<< /Type /Pages /Kids [{kids.ToString().Trim()}] /Count {pageCount} >>");
            Obj(3, "<< >>");

            for (int i = 0; i < pageCount; i++)
            {
                Obj(firstPageId + i,
                    $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(PageWidth)} {F(PageHeight)}] " +
                    $"/Resources << /Font << /F1 {fontRegularId} 0 R /F2 {fontBoldId} 0 R >> >> " +
                    $"/Contents {firstContentId + i} 0 R >>");
            }

            for (int i = 0; i < pageCount; i++)
            {
                string content = _pages[i].ToString();
                Obj(firstContentId + i,
                    $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream");
            }

            Obj(fontRegularId, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            Obj(fontBoldId, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

            int xrefAt = pdf.Length;
            int total = offsets.Count + 1;
            pdf.Append("xref\n0 ").Append(total).Append('\n');
            pdf.Append("0000000000 65535 f \n");
            foreach (int offset in offsets)
                pdf.Append(offset.ToString("D10")).Append(" 00000 n \n");

            pdf.Append("trailer\n<< /Size ").Append(total).Append(" /Root 1 0 R >>\nstartxref\n")
               .Append(xrefAt).Append("\n%%EOF\n");

            // Latin-1 keeps byte offsets equal to character offsets, which is what the
            // cross-reference table above assumes.
            return Encoding.GetEncoding(28591).GetBytes(pdf.ToString());
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        // Text is a PDF literal string; brackets and backslashes end it early.
        // Anything outside Latin-1 is transliterated rather than dropped, so a stray
        // dash or quotation mark cannot corrupt the file.
        static string Escape(string text)
        {
            var sb = new StringBuilder(text.Length + 8);
            foreach (char raw in text)
            {
                // Characters with no Latin-1 equivalent are spelled out rather than
                // dropped. An arrow becoming a question mark is not a missing glyph a
                // reader shrugs off — it turns "13 → 6" into "13 ? 6".
                switch (raw)
                {
                    case '→': sb.Append("->");  continue;
                    case '←': sb.Append("<-");  continue;
                    case '…': sb.Append("..."); continue;
                    case '≈': sb.Append("~");   continue;
                    case '≤': sb.Append("<=");  continue;
                    case '≥': sb.Append(">=");  continue;
                }

                char c = raw switch
                {
                    '—' or '–' => '-',
                    '‘' or '’' => '\'',
                    '“' or '”' => '"',
                    '•' => '·',
                    ' ' => ' ',
                    _ => raw,
                };

                if (c == '(' || c == ')' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 32 || c > 255) sb.Append('?');
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}

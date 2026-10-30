using InternshipManagement.Application.Interfaces;
using System;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;

namespace InternshipManagement.Infrastructure.Documents
{
    /// <summary>
    /// Reads the text layer of a PDF with PdfPig (pure .NET, nothing is written to disk).
    /// Page.Text has no line breaks for many PDFs, which would break everything that works per line
    /// (the contact-detail remover, name masking), so lines are rebuilt from the word positions:
    /// words on the same baseline form one line, lines run top to bottom.
    /// Image-only (scanned) PDFs have no text layer and come back (almost) empty; the caller handles that.
    /// </summary>
    public sealed class PdfPigTextExtractor : IPdfTextExtractor
    {
        // Words whose baselines are within this many points belong to the same line.
        private const double LineTolerance = 3.0;

        public string ExtractText(byte[] pdf, int maxPages)
        {
            try
            {
                using var document = PdfDocument.Open(pdf);

                var sb = new StringBuilder();
                var pages = 0;
                foreach (var page in document.GetPages())
                {
                    if (pages++ >= maxPages) break;

                    var lines = page.GetWords()
                        .GroupBy(w => Math.Round(w.BoundingBox.Bottom / LineTolerance))
                        .OrderByDescending(g => g.Key)   // PDF y runs bottom to top
                        .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));

                    sb.AppendLine(string.Join("\n", lines));
                }

                return sb.ToString();
            }
            catch (Exception ex)   // corrupt, encrypted, truncated or not a PDF at all
            {
                throw new PdfUnreadableException("The PDF could not be read.", ex);
            }
        }
    }
}

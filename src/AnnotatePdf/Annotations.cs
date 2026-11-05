using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GroupDocs.Annotation;
using GroupDocs.Annotation.Models;
using GroupDocs.Annotation.Models.AnnotationModels;
using GroupDocs.Annotation.Options;

namespace AnnotatePdf
{
    /// <summary>
    /// Every GroupDocs.Annotation call this sample makes, in one file.
    /// The same code runs on PDF, DOCX, XLSX, PPTX and images - only the file path changes.
    /// </summary>
    public static class Annotations
    {
        // ------------------------------------------------------------------ info

        public static void PrintInfo(string path)
        {
            using Annotator annotator = new Annotator(path);
            IDocumentInfo info = annotator.Document.GetDocumentInfo();

            Console.WriteLine($"{Path.GetFileName(path)}");
            Console.WriteLine($"  type   : {info.FileType}");
            Console.WriteLine($"  pages  : {info.PageCount}");
            Console.WriteLine($"  bytes  : {info.Size}");

            foreach (PageInfo page in info.PagesInfo)
            {
                int lines = page.TextLines?.Count ?? 0;

                // PageInfo.PageNumber is 1-based. Annotations are placed with a 0-based index.
                Console.WriteLine($"  page {page.PageNumber}: {page.Width} x {page.Height} pt, {lines} text line(s)");
            }
        }

        // ------------------------------------------------------------------ add

        /// <summary>
        /// The hello-world case: a yellow highlight with a comment and one reply.
        ///
        /// Points are given with the origin at the BOTTOM-left corner of the page, the same
        /// convention PDF itself uses. Four points describe one rectangle, in this order:
        /// top-left, top-right, bottom-left, bottom-right. More rectangles can follow in the
        /// same list when the highlighted text wraps across lines.
        /// </summary>
        public static void AddHighlight(string path, string output, int pageIndex, float x, float y, float width, float height, string message)
        {
            using Annotator annotator = new Annotator(path);

            HighlightAnnotation highlight = new HighlightAnnotation
            {
                PageNumber = pageIndex,            // 0 = first page
                BackgroundColor = 0xFFE699,        // 0xRRGGBB
                Opacity = 0.5,
                Message = message,
                CreatedOn = DateTime.Now,
                User = new User { Id = 1, Name = "Reviewer", Role = Role.Editor },
                Points = new List<Point>
                {
                    new Point(x, y + height),
                    new Point(x + width, y + height),
                    new Point(x, y),
                    new Point(x + width, y)
                },
                Replies = new List<Reply>
                {
                    new Reply { Comment = "Confirmed with finance.", RepliedOn = DateTime.Now }
                }
            };

            annotator.Add(highlight);
            annotator.Save(output);
        }

        /// <summary>
        /// Finds a phrase in the document text and highlights it - no hand-measured coordinates.
        ///
        /// GetDocumentInfo() returns the text lines of every page with their boxes. Those boxes
        /// are measured from the TOP of the page, so the y value has to be flipped before it goes
        /// into a text-markup annotation.
        /// </summary>
        public static int HighlightText(string path, string output, string phrase, string message)
        {
            using Annotator annotator = new Annotator(path);
            IDocumentInfo info = annotator.Document.GetDocumentInfo();
            int added = 0;

            foreach (PageInfo page in info.PagesInfo)
            {
                if (page.TextLines == null)
                {
                    continue;
                }

                foreach (TextLineInfo line in page.TextLines)
                {
                    if (line.Text == null || line.Text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    float left = (float)line.LeftIndent;
                    float right = (float)(line.LeftIndent + line.Width);
                    float top = (float)(page.Height - line.TopIndent);
                    float bottom = (float)(page.Height - line.TopIndent - line.Height);

                    annotator.Add(new HighlightAnnotation
                    {
                        PageNumber = page.PageNumber - 1,   // 1-based in, 0-based out
                        BackgroundColor = 0xFFE699,
                        Opacity = 0.5,
                        Message = message,
                        CreatedOn = DateTime.Now,
                        User = new User { Id = 1, Name = "Reviewer", Role = Role.Editor },
                        Points = new List<Point>
                        {
                            new Point(left, top),
                            new Point(right, top),
                            new Point(left, bottom),
                            new Point(right, bottom)
                        }
                    });

                    added++;
                }
            }

            annotator.Save(output);
            return added;
        }

        /// <summary>A box around a region, for figures and tables. Box is measured from the TOP-left.</summary>
        public static void AddArea(string path, string output, int pageIndex, float x, float y, float width, float height, string message)
        {
            using Annotator annotator = new Annotator(path);

            annotator.Add(new AreaAnnotation
            {
                PageNumber = pageIndex,
                Box = new Rectangle(x, y, width, height),
                BackgroundColor = 0xCCE5FF,
                PenColor = 0x0000FF,
                PenWidth = 1,
                Opacity = 0.3,
                Message = message,
                CreatedOn = DateTime.Now
            });

            annotator.Save(output);
        }

        // ------------------------------------------------------------------ read

        public static List<AnnotationBase> Read(string path)
        {
            using Annotator annotator = new Annotator(path);
            return annotator.Get();
        }

        public static void Print(IEnumerable<AnnotationBase> annotations)
        {
            bool any = false;

            foreach (AnnotationBase annotation in annotations)
            {
                any = true;
                Console.WriteLine($"#{annotation.Id} {annotation.Type} on page {annotation.PageNumber + 1} by {annotation.User?.Name ?? "(no author)"}");

                if (!string.IsNullOrWhiteSpace(annotation.Message))
                {
                    Console.WriteLine($"    {annotation.Message}");
                }

                if (annotation.Replies == null)
                {
                    continue;
                }

                foreach (Reply reply in annotation.Replies)
                {
                    Console.WriteLine($"    reply: {reply.Comment}");
                }
            }

            if (!any)
            {
                Console.WriteLine("No annotations.");
            }
        }

        // ------------------------------------------------------------------ remove

        public static int Remove(string path, string output, int? id)
        {
            using Annotator annotator = new Annotator(path);
            List<AnnotationBase> annotations = annotator.Get();

            List<AnnotationBase> doomed = id.HasValue
                ? annotations.Where(a => a.Id == id.Value).ToList()
                : annotations;

            if (doomed.Count > 0)
            {
                annotator.Remove(doomed);
            }

            annotator.Save(output);
            return doomed.Count;
        }

        // ------------------------------------------------------------------ preview

        /// <summary>
        /// Renders page images. PreviewOptions.PageNumbers is 1-BASED, unlike the page index
        /// used when adding an annotation.
        /// </summary>
        public static List<string> Preview(string path, string outputDirectory, bool withAnnotations)
        {
            Directory.CreateDirectory(outputDirectory);
            List<string> written = new List<string>();

            using Annotator annotator = new Annotator(path);

            PreviewOptions previewOptions = new PreviewOptions(pageNumber =>
            {
                string file = Path.Combine(outputDirectory, $"page_{pageNumber}.png");
                written.Add(file);
                return File.Create(file);
            });

            previewOptions.PreviewFormat = PreviewFormats.PNG;
            previewOptions.RenderAnnotations = withAnnotations;

            annotator.Document.GeneratePreview(previewOptions);
            return written;
        }
    }
}

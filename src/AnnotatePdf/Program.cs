using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GroupDocs.Annotation;

namespace AnnotatePdf
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            ApplyLicense();

            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                Usage();
                return args.Length == 0 ? 1 : 0;
            }

            try
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "info":
                        Annotations.PrintInfo(Document(args));
                        return 0;

                    case "highlight":
                        return Highlight(args);

                    case "area":
                        return Area(args);

                    case "list":
                        Annotations.Print(Annotations.Read(Document(args)));
                        return 0;

                    case "remove":
                        return Remove(args);

                    case "preview":
                        return Preview(args);

                    default:
                        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
                        Usage();
                        return 1;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Error: " + exception.Message);
                return 1;
            }
        }

        // ----------------------------------------------------------------- commands

        private static int Highlight(string[] args)
        {
            string document = Document(args);
            string output = Option(args, "--out") ?? Output(document, "annotated");
            string message = Option(args, "--message") ?? "Please check this.";
            string? phrase = Option(args, "--text");

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);

            if (phrase != null)
            {
                int added = Annotations.HighlightText(document, output, phrase, message);
                Console.WriteLine($"{added} line(s) matched \"{phrase}\".");
            }
            else
            {
                int page = Int(args, "--page", 0);
                float x = Float(args, "--x", 72);
                float y = Float(args, "--y", 650);
                float width = Float(args, "--width", 240);
                float height = Float(args, "--height", 14);
                Annotations.AddHighlight(document, output, page, x, y, width, height, message);
                Console.WriteLine($"Highlight added on page index {page} at ({x}, {y}).");
            }

            Console.WriteLine("Saved " + output);
            return 0;
        }

        private static int Area(string[] args)
        {
            string document = Document(args);
            string output = Option(args, "--out") ?? Output(document, "annotated");
            string message = Option(args, "--message") ?? "Check this block.";

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);

            Annotations.AddArea(
                document,
                output,
                Int(args, "--page", 0),
                Float(args, "--x", 72),
                Float(args, "--y", 160),
                Float(args, "--width", 300),
                Float(args, "--height", 60),
                message);

            Console.WriteLine("Saved " + output);
            return 0;
        }

        private static int Remove(string[] args)
        {
            string document = Document(args);
            string output = Option(args, "--out") ?? Output(document, "cleaned");
            string? id = Option(args, "--id");

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);

            int removed = Annotations.Remove(
                document,
                output,
                id == null ? (int?)null : int.Parse(id, CultureInfo.InvariantCulture));

            Console.WriteLine($"Removed {removed} annotation(s).");
            Console.WriteLine("Saved " + output);
            return 0;
        }

        private static int Preview(string[] args)
        {
            string document = Document(args);
            string directory = Option(args, "--out") ?? Path.Combine("output", "preview");
            bool withAnnotations = !args.Contains("--no-annotations");

            List<string> files = Annotations.Preview(document, directory, withAnnotations);
            foreach (string file in files)
            {
                Console.WriteLine("  " + file);
            }

            Console.WriteLine($"{files.Count} page image(s) written to {directory}.");
            return 0;
        }

        // ----------------------------------------------------------------- helpers

        private static string Document(string[] args)
        {
            string? path = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A document path is required.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Document not found: " + path);
            }

            return path;
        }

        private static string Output(string document, string suffix)
        {
            return Path.Combine(
                "output",
                Path.GetFileNameWithoutExtension(document) + "." + suffix + Path.GetExtension(document));
        }

        private static string? Option(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static int Int(string[] args, string name, int fallback)
        {
            string? value = Option(args, name);
            return value == null ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static float Float(string[] args, string name, float fallback)
        {
            string? value = Option(args, name);
            return value == null ? fallback : float.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>Optional: without a licence the output carries evaluation marks.</summary>
        private static void ApplyLicense()
        {
            string? path = Environment.GetEnvironmentVariable("GROUPDOCS_ANNOTATION_LICENSE");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            path = path.Trim();
            if (File.Exists(path))
            {
                new License().SetLicense(path);
            }
            else
            {
                Console.Error.WriteLine("GROUPDOCS_ANNOTATION_LICENSE points at a missing file: " + path);
            }
        }

        private static void Usage()
        {
            Console.WriteLine("Annotate PDF and Office documents in C# - GroupDocs.Annotation quickstart");
            Console.WriteLine();
            Console.WriteLine("  info      <document>");
            Console.WriteLine("  highlight <document> [--text \"phrase\"] [--message text] [--out file]");
            Console.WriteLine("            [--page 0] [--x 72] [--y 650] [--width 240] [--height 14]");
            Console.WriteLine("  area      <document> [--page 0] [--x --y --width --height] [--message text] [--out file]");
            Console.WriteLine("  list      <document>");
            Console.WriteLine("  remove    <document> [--id N] [--out file]");
            Console.WriteLine("  preview   <document> [--out directory] [--no-annotations]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run --project src/AnnotatePdf -- info samples/quarterly-review.pdf");
            Console.WriteLine("  dotnet run --project src/AnnotatePdf -- highlight samples/quarterly-review.pdf --text \"Hosting costs\"");
            Console.WriteLine("  dotnet run --project src/AnnotatePdf -- list output/quarterly-review.annotated.pdf");
        }
    }
}

using Markdig;
using Markdig.Wpf;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;

namespace SystemMetricsApp.Services
{
    public class MarkdownRenderer
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        public static FlowDocument RenderMarkdown(string markdown)
        {
            var document = Markdig.Wpf.Markdown.ToFlowDocument(markdown, Pipeline);
            
            // Process each block in the document
            foreach (var block in document.Blocks.ToList())
            {
                if (block is Paragraph paragraph)
                {
                    // Set default text color for paragraphs
                    paragraph.Foreground = System.Windows.Media.Brushes.White;
                    paragraph.TextAlignment = TextAlignment.Left;

                    // Process all inline elements
                    foreach (var inline in paragraph.Inlines)
                    {
                        inline.Foreground = System.Windows.Media.Brushes.White;
                    }

                    // Check for code blocks (text starting with ```)
                    var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
                    if (text.StartsWith("```") || text.StartsWith("`"))
                    {
                        // Create a code block container
                        var codeBlock = new Section
                        {
                            Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF1A1A1A")),
                            Padding = new Thickness(10),
                            BorderThickness = new Thickness(0),
                            Margin = new Thickness(5)
                        };

                        // Create code content
                        var codeParagraph = new Paragraph
                        {
                            Foreground = System.Windows.Media.Brushes.White,
                            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                            LineHeight = 20
                        };

                        // Remove backticks and language identifier if present
                        var code = text.Replace("```", "").Trim();
                        if (code.Contains("\n"))
                        {
                            code = code.Substring(code.IndexOf("\n")).Trim();
                        }

                        codeParagraph.Inlines.Add(new Run(code));
                        codeBlock.Blocks.Add(codeParagraph);

                        // Replace the original paragraph with our styled code block
                        document.Blocks.InsertBefore(paragraph, codeBlock);
                        document.Blocks.Remove(paragraph);
                    }
                }
            }

            // Set document-level properties
            document.Foreground = System.Windows.Media.Brushes.White;
            document.Background = System.Windows.Media.Brushes.Transparent;
            document.PagePadding = new Thickness(0);

            return document;
        }
    }
} 
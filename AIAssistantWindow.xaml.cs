using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using SystemMetricsApp.ViewModels;
using SystemMetricsApp.Services;
using System.Windows.Threading;

namespace SystemMetricsApp
{
    public partial class AIAssistantWindow : Window
    {
        private readonly SystemMetricsViewModel _viewModel;
        private readonly PerplexityService? _perplexityService;

        public AIAssistantWindow(SystemMetricsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            
            string? apiKey = Environment.GetEnvironmentVariable("PERPLEXITY_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                // Show input dialog for API key
                var dialog = new Window
                {
                    Title = "API Key Required",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF1E1E1E"))
                };

                var panel = new StackPanel { Margin = new Thickness(10) };
                var textBox = new System.Windows.Controls.TextBox 
                { 
                    Margin = new Thickness(0, 10, 0, 10),
                    Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF2D2D2D")),
                    Foreground = System.Windows.Media.Brushes.White
                };
                var button = new System.Windows.Controls.Button 
                { 
                    Content = "Set API Key",
                    Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF2D2D2D")),
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                panel.Children.Add(new TextBlock 
                { 
                    Text = "Please enter your Perplexity API Key:",
                    Foreground = System.Windows.Media.Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                });
                panel.Children.Add(textBox);
                panel.Children.Add(button);

                dialog.Content = panel;

                button.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        apiKey = textBox.Text;
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                };

                var result = dialog.ShowDialog();
                if (result != true)
                {
                    Close();
                    return;
                }
            }
            
            try
            {
                _perplexityService = new PerplexityService(apiKey!);
                // Automatically generate initial analysis
                GenerateInitialAnalysis();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error initializing API: {ex.Message}");
                Close();
            }
        }

        private async void GenerateInitialAnalysis()
        {
            if (_perplexityService == null) return;

            try
            {
                UserInputTextBox.IsEnabled = false;
                ShowThinking(true);
                
                string initialPrompt = "Analyze these system metrics and provide a comprehensive report about:" +
                                       "\n1. Overall system health" +
                                       "\n2. Current performance state" +
                                       "\n3. Any potential bottlenecks" +
                                       "\n4. Recommendations if needed" +
                                       "\nBe specific about THIS system's current state.";

                var messagePanel = CreateMessagePanel("Assistant");
                var richTextBox = new System.Windows.Controls.RichTextBox
                {
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = System.Windows.Media.Brushes.White,
                    Padding = new Thickness(0)
                };
                messagePanel.Child = richTextBox;
                ChatStackPanel.Children.Add(messagePanel);

                string accumulatedText = "";
                var channel = _perplexityService.GetStreamingResponseAsync(initialPrompt, _viewModel);
                
                while (await channel.WaitToReadAsync())
                {
                    while (channel.TryRead(out var token))
                    {
                        accumulatedText += token;
                        richTextBox.Document = MarkdownRenderer.RenderMarkdown(accumulatedText);
                        
                        if (ChatStackPanel.Parent is ScrollViewer scrollViewer)
                        {
                            scrollViewer.ScrollToBottom();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayMessage("System", $"Error generating initial analysis: {ex.Message}");
            }
            finally
            {
                ShowThinking(false);
                UserInputTextBox.IsEnabled = true;
                UserInputTextBox.Focus();
            }
        }

        private void ShowThinking(bool show)
        {
            ThinkingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void UserInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private async void SendMessage()
        {
            if (_perplexityService == null) return;
            
            string userMessage = UserInputTextBox.Text;
            if (string.IsNullOrWhiteSpace(userMessage))
                return;

            // Display user's message
            DisplayMessage("User", userMessage);
            UserInputTextBox.Clear();

            try
            {
                UserInputTextBox.IsEnabled = false;
                ShowThinking(true);

                var messagePanel = CreateMessagePanel("Assistant");
                var richTextBox = new System.Windows.Controls.RichTextBox
                {
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = System.Windows.Media.Brushes.White,
                    Padding = new Thickness(0)
                };
                messagePanel.Child = richTextBox;
                ChatStackPanel.Children.Add(messagePanel);

                string accumulatedText = "";
                var channel = _perplexityService.GetStreamingResponseAsync(userMessage, _viewModel);
                
                while (await channel.WaitToReadAsync())
                {
                    while (channel.TryRead(out var token))
                    {
                        accumulatedText += token;
                        richTextBox.Document = MarkdownRenderer.RenderMarkdown(accumulatedText);
                        
                        if (ChatStackPanel.Parent is ScrollViewer scrollViewer)
                        {
                            scrollViewer.ScrollToBottom();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayMessage("System", $"Error: {ex.Message}");
            }
            finally
            {
                ShowThinking(false);
                UserInputTextBox.IsEnabled = true;
                UserInputTextBox.Focus();
            }
        }

        private void DisplayMessage(string sender, string message, bool useMarkdown = false)
        {
            var messagePanel = new Border
            {
                Background = sender == "User" 
                    ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF2D2D2D"))
                    : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF3E3E3E")),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                HorizontalAlignment = sender == "User" ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left,
                MaxWidth = 800
            };

            if (sender == "Assistant" && useMarkdown)
            {
                var viewer = new System.Windows.Controls.RichTextBox
                {
                    Document = MarkdownRenderer.RenderMarkdown(message),
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    FontSize = 14
                };

                // Set all text to white
                viewer.Document.Foreground = System.Windows.Media.Brushes.White;
                foreach (var block in viewer.Document.Blocks)
                {
                    if (block is Paragraph paragraph)
                    {
                        paragraph.Foreground = System.Windows.Media.Brushes.White;
                        foreach (var inline in paragraph.Inlines)
                        {
                            inline.Foreground = System.Windows.Media.Brushes.White;
                        }
                    }
                }

                messagePanel.Child = viewer;
            }
            else
            {
                messagePanel.Child = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 14
                };
            }

            ChatStackPanel.Children.Add(messagePanel);

            // Show thinking indicator after user messages
            if (sender == "User")
            {
                ShowThinking(true);
            }

            // Scroll to the bottom
            if (ChatStackPanel.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        private Border CreateMessagePanel(string sender)
        {
            return new Border
            {
                Background = sender == "User" 
                    ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF2D2D2D"))
                    : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF3E3E3E")),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5),
                Padding = new Thickness(10),
            };
        }
    }
} 
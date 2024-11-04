using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Channels;
using SystemMetricsApp.ViewModels;

namespace SystemMetricsApp.Services
{
    public class PerplexityService
    {
        private readonly HttpClient _httpClient;
        private readonly List<Dictionary<string, string>> _messages;
        private const string API_URL = "https://api.perplexity.ai/chat/completions";
        private string _lastRole = "system";

        public PerplexityService(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            _messages = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", 
                        "You are an AI assistant integrated into SystemMetricViewer, a real-time PC monitoring application. " +
                        "You have access to live system metrics that are being collected and updated every second. " +
                        "Your role is to:\n" +
                        "1. Analyze the real-time metrics from THIS specific computer\n" +
                        "2. Explain what these numbers mean for the user's system performance\n" +
                        "3. Identify any potential bottlenecks or issues\n" +
                        "4. Provide context about how these values compare to typical ranges\n" +
                        "5. Suggest optimizations when relevant\n\n" +
                        "Format your responses with '##' headers for clarity and use technical terms but explain them. " +
                        "Remember these are LIVE metrics, not theoretical values." 
                    }
                }
            };
        }

        public async Task<string> GetResponseAsync(string userMessage, SystemMetricsViewModel metrics)
        {
            try
            {
                if (_lastRole == "user")
                {
                    return "Error: Waiting for assistant response. Please try again.";
                }

                string prompt = GeneratePrompt(userMessage, metrics);
                _messages.Add(new Dictionary<string, string>
                {
                    { "role", "user" },
                    { "content", prompt }
                });
                _lastRole = "user";

                var requestBody = new
                {
                    model = "llama-3.1-sonar-large-128k-online",
                    messages = _messages,
                    temperature = 0.1,
                    max_tokens = 2000,
                    top_p = 0.9,
                    frequency_penalty = 0.9
                };

                try
                {
                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync(API_URL, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return "Error: Cannot connect to AI service. Please check your internet connection and try again.";
                        }
                        return $"API Error: {responseContent}";
                    }

                    var result = JsonSerializer.Deserialize<PerplexityResponse>(responseContent);
                    string reply = result?.choices?[0]?.message?.content ?? "No response received";
                    
                    _messages.Add(new Dictionary<string, string>
                    {
                        { "role", "assistant" },
                        { "content", reply }
                    });
                    _lastRole = "assistant";

                    return reply;
                }
                catch (HttpRequestException ex)
                {
                    return $"Network Error: Cannot connect to AI service. Details: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"System Error: {ex.Message}";
            }
        }

        public ChannelReader<string> GetStreamingResponseAsync(string userMessage, SystemMetricsViewModel metrics)
        {
            var channel = Channel.CreateUnbounded<string>();
            
            _ = ProcessStreamAsync(channel.Writer, userMessage, metrics);
            
            return channel.Reader;
        }

        private async Task ProcessStreamAsync(ChannelWriter<string> writer, string userMessage, SystemMetricsViewModel metrics)
        {
            try
            {
                if (_lastRole == "user")
                {
                    await writer.WriteAsync("Error: Waiting for assistant response. Please try again.");
                    return;
                }

                string prompt = GeneratePrompt(userMessage, metrics);
                _messages.Add(new Dictionary<string, string>
                {
                    { "role", "user" },
                    { "content", prompt }
                });
                _lastRole = "user";

                var requestBody = new
                {
                    model = "llama-3.1-sonar-large-128k-online",
                    messages = _messages,
                    temperature = 0.1,
                    max_tokens = 2000,
                    top_p = 0.9,
                    frequency_penalty = 0.9,
                    stream = true
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(API_URL, content);
                if (!response.IsSuccessStatusCode)
                {
                    await writer.WriteAsync($"API Error: {await response.Content.ReadAsStringAsync()}");
                    return;
                }

                string fullResponse = "";
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;
                    
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<StreamResponse>(data);
                        if (chunk?.choices?[0]?.delta?.content is string token)
                        {
                            if (token.Contains("[") || token.Contains("]") || 
                                token.Length > 100 || 
                                token.Contains("or \"v\""))
                            {
                                continue;  // Skip this token
                            }

                            token = token.Replace("[2[5", "")
                                        .Replace("[1[4[5", "")
                                        .Replace("[2[5[4", "");

                            fullResponse += token;
                            await writer.WriteAsync(token);
                        }

                        if (chunk?.choices?[0]?.finish_reason != null)
                        {
                            break;  // End streaming if we get a finish signal
                        }
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine($"Failed to parse: {data}");
                        continue;
                    }
                }

                if (!fullResponse.EndsWith("\n"))
                {
                    await writer.WriteAsync("\n");
                }

                if (!string.IsNullOrEmpty(fullResponse))
                {
                    _messages.Add(new Dictionary<string, string>
                    {
                        { "role", "assistant" },
                        { "content", fullResponse }
                    });
                    _lastRole = "assistant";
                }
            }
            catch (Exception ex)
            {
                await writer.WriteAsync($"Error: {ex.Message}");
            }
            finally
            {
                writer.Complete();
            }
        }

        private string GeneratePrompt(string userMessage, SystemMetricsViewModel metrics)
        {
            return $"Real-time system metrics from SystemMetricViewer:\n" +
                   // System Information
                   $"System: {metrics.ProcessorInfo}\n" +
                   // Current Usage
                   $"## Current Usage\n" +
                   $"Total CPU Usage: {metrics.CpuUsageText}\n" +
                   $"Memory Usage: {metrics.MemoryUsageText}\n" +
                   $"Most CPU-Intensive Process: {metrics.TopCpuProcess}\n" +
                   $"Most Memory-Intensive Process: {metrics.TopMemoryProcess}\n\n" +
                   // Cache Performance
                   $"## Cache Performance\n" +
                   $"L1 Instruction Cache Miss Rate: {metrics.L1ICacheMissRate}%\n" +
                   $"L1 Data Cache Miss Rate: {metrics.L1DCacheMissRate}%\n" +
                   $"L2 Cache Hit Rate: {metrics.L2HitRate}%\n\n" +
                   // User Question
                   $"User asks: {userMessage}";
        }
    }

    // Response classes for JSON deserialization
    public class PerplexityResponse
    {
        public required string id { get; set; }
        public required string model { get; set; }
        public required string @object { get; set; }
        public required long created { get; set; }
        public required List<Choice> choices { get; set; }
        public required Usage usage { get; set; }
    }

    public class Choice
    {
        public required int index { get; set; }
        public required string finish_reason { get; set; }
        public required Message message { get; set; }
    }

    public class Message
    {
        public required string role { get; set; }
        public required string content { get; set; }
    }

    public class Usage
    {
        public required int prompt_tokens { get; set; }
        public required int completion_tokens { get; set; }
        public required int total_tokens { get; set; }
    }

    // Add streaming response classes
    public class StreamResponse
    {
        public string? id { get; set; }
        public string? @object { get; set; }
        public long created { get; set; }
        public string? model { get; set; }
        public List<StreamChoice>? choices { get; set; }
    }

    public class StreamChoice
    {
        public int index { get; set; }
        public Delta? delta { get; set; }
        public string? finish_reason { get; set; }
    }

    public class Delta
    {
        public string? role { get; set; }
        public string? content { get; set; }
    }
} 
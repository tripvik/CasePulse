using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Helpers;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Services;

public class OpenAITranscriptionService : ITranscriptionService, IAsyncDisposable
{
    private readonly IConfiguration _config;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private readonly byte[] _recvBuffer = new byte[8192];

    public event EventHandler<ChatMessage>? RecognizingTranscriptReceived;
    public event EventHandler<ChatMessage>? TranscriptReceived;

    public OpenAITranscriptionService(IConfiguration config)
    {
        _config = config;
    }

    public async Task InitializeAsync(WaveFormat format)
    {
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();

        string? endpoint = _config["Azure:4o-Speech:Endpoint"];
        string? apiKey = _config["Azure:4o-Speech:Key"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Azure OpenAI endpoint or key not configured.");

        var url = endpoint.Replace("https", "wss") + "/openai/realtime?api-version=2025-04-01-preview&intent=transcription";
        _ws.Options.SetRequestHeader("api-key", apiKey);

        await _ws.ConnectAsync(new Uri(url), _cts.Token);
        Debug.WriteLine($"Connected to {url}");

        //https://platform.openai.com/docs/api-reference/realtime-sessions/create-transcription

        var sessionConfig = new
        {
            type = "transcription_session.update",
            session = new
            {
                input_audio_format = "pcm16",
                input_audio_transcription = new
                {
                    model = "gpt-4o-mini-transcribe",
                    prompt = "Respond in English."
                },
                input_audio_noise_reduction = new { type = "near_field" },
                turn_detection = new
                {
                    type = "server_vad",
                    threshold = 0.5,
                    prefix_padding_ms = 300,
                    silence_duration_ms = 200
                }
            }
        };

        var configPayload = JsonSerializer.Serialize(sessionConfig);
        await _ws.SendAsync(
            Encoding.UTF8.GetBytes(configPayload),
            WebSocketMessageType.Text, true, _cts.Token
        );

        _ = Task.Run(ReceiveLoop, _cts.Token);
    }

    public async Task ProcessChunkAsync(byte[] audioData)
    {
        if (_ws?.State != WebSocketState.Open) return;
        //var signedaudioData = AudioHelper.ConvertUnsigned8BitToSigned16Bit(audioData);
        var b64 = Convert.ToBase64String(audioData);
        var msg = JsonSerializer.Serialize(new { type = "input_audio_buffer.append", audio = b64 });

        try
        {
            await _ws.SendAsync(
                Encoding.UTF8.GetBytes(msg),
                WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Failed to send audio chunk: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_ws != null && _ws.State == WebSocketState.Open)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
        }

        _cts?.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _ws?.Dispose();
        _cts?.Dispose();

        RecognizingTranscriptReceived = null;
        TranscriptReceived = null;
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (_ws?.State == WebSocketState.Open && !_cts!.IsCancellationRequested)
            {
                var result = await _ws.ReceiveAsync(_recvBuffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var json = Encoding.UTF8.GetString(_recvBuffer, 0, result.Count);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                if (type == "conversation.item.input_audio_transcription.delta")
                {
                    var text = root.GetProperty("delta").GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        RecognizingTranscriptReceived?.Invoke(this, new ChatMessage
                        {
                            Message = text,
                            Timestamp = DateTime.Now,
                            User = "You",
                            Initials = "Y"
                        });
                    }
                }
                else if (type == "conversation.item.input_audio_transcription.completed")
                {
                    var text = root.GetProperty("transcript").GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        TranscriptReceived?.Invoke(this, new ChatMessage
                        {
                            Message = text,
                            Timestamp = DateTime.Now,
                            User = "You",
                            Initials = "Y"
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ ReceiveLoop failed: {ex.Message}");
        }
    }
}

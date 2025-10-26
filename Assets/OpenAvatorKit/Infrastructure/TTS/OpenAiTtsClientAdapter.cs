using Newtonsoft.Json;
using OpenAvatarKit.Infrastructure.Interface;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAvatarKit.Infrastructure.TTS
{
    /// <summary>
    /// OpenAI TTS クライアント (Newtonsoft.Json版)
    /// </summary>
    public sealed class OpenAiTtsClientAdapter : ITtsClientAdapter
    {
        [Header("OpenAI Settings")]
        public string apiKey;
        public string model = "gpt-4o-mini-tts";
        public string voice = "alloy";
        public string responseFormat = "wav";
        public int requestTimeoutSec = 20;
        public int maxRetries = 2;
        public float retryBackoffSec = 0.8f;

        private const string Endpoint = "https://api.openai.com/v1/audio/speech";

        // ---- AudioClip生成メインI/F ----
        public async Task<AudioClip> SynthesizeToClipAsync(
            string text,
            TtsSynthesisOptions options = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("text is empty");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OpenAI apiKey not set.");

            var payload = new OpenAiTtsRequest
            {
                Model = options?.Model ?? model,
                Voice = options?.Voice ?? voice,
                Input = text,
                ResponseFormat = options?.ResponseFormat ?? responseFormat
            };

            var wavBytes = await PostJsonForBytesAsync(Endpoint, payload, ct);
            var clip = SimpleWav.ToAudioClip(wavBytes, "openai_tts");
            if (clip == null) throw new Exception("Failed to decode WAV to AudioClip.");
            return clip;
        }

        // ---- バイト配列版 ----
        public async Task<byte[]> SynthesizeToBytesAsync(
            string text,
            TtsSynthesisOptions options = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("text is empty");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OpenAI apiKey not set.");

            var payload = new OpenAiTtsRequest
            {
                Model = options?.Model ?? model,
                Voice = options?.Voice ?? voice,
                Input = text,
                ResponseFormat = options?.ResponseFormat ?? responseFormat
            };

            return await PostJsonForBytesAsync(Endpoint, payload, ct);
        }

        // ---- HTTP本体 ----
        private async Task<byte[]> PostJsonForBytesAsync(string url, OpenAiTtsRequest body, CancellationToken ct)
        {
            var json = JsonConvert.SerializeObject(body);
            var bytes = Encoding.UTF8.GetBytes(json);

            int attempt = 0;
            while (true)
            {
                attempt++;
                using var req = new UnityWebRequest(url, "POST");
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                req.timeout = requestTimeoutSec;

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    if (ct.IsCancellationRequested)
                    {
                        req.Abort();
                        ct.ThrowIfCancellationRequested();
                    }
                    await Task.Yield();
                }

                bool isNetworkError =
#if UNITY_2020_2_OR_NEWER
                    req.result == UnityWebRequest.Result.ConnectionError ||
                    req.result == UnityWebRequest.Result.ProtocolError;
#else
                    req.isNetworkError || req.isHttpError;
#endif

                if (isNetworkError)
                {
                    int status = (int)req.responseCode;
                    if ((status == 429 || status >= 500) && attempt <= maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryBackoffSec * attempt), ct);
                        continue;
                    }
                    throw new Exception($"OpenAI TTS HTTP Error {status}: {req.error}\n{req.downloadHandler?.text}");
                }

                return req.downloadHandler.data;
            }
        }

        // ---- DTO ----
        [Serializable]
        private sealed class OpenAiTtsRequest
        {
            [JsonProperty("model")] public string Model;
            [JsonProperty("voice")] public string Voice;
            [JsonProperty("input")] public string Input;
            [JsonProperty("response_format")] public string ResponseFormat;
        }

        // ---- オプション ----
        public sealed class TtsSynthesisOptions
        {
            public string Model;
            public string Voice;
            public string ResponseFormat;
        }
    }

    // ============================================================
    // 最小 WAV デコーダ（PCM 16bit / 1or2ch）
    // ============================================================
    internal static class SimpleWav
    {
        public static AudioClip ToAudioClip(byte[] wavBytes, string clipName)
        {
            if (wavBytes == null || wavBytes.Length < 44) return null;

            int channels, sampleRate, bitsPerSample, dataStart, dataLength;
            if (!ParseHeader(wavBytes, out channels, out sampleRate, out bitsPerSample, out dataStart, out dataLength))
                return null;

            if (bitsPerSample != 16)
            {
                Debug.LogError($"SimpleWav: only 16bit PCM supported (got {bitsPerSample})");
                return null;
            }

            int sampleCount = dataLength / (bitsPerSample / 8);
            int frames = sampleCount / channels;

            float[] samples = new float[sampleCount];
            int sampleIndex = 0;
            for (int i = dataStart; i < dataStart + dataLength; i += 2)
            {
                short s = (short)(wavBytes[i] | (wavBytes[i + 1] << 8));
                samples[sampleIndex++] = Mathf.Clamp(s / 32768f, -1f, 1f);
            }

            var clip = AudioClip.Create(clipName, frames, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static bool ParseHeader(byte[] b, out int channels, out int sampleRate, out int bitsPerSample, out int dataStart, out int dataLength)
        {
            channels = sampleRate = bitsPerSample = dataStart = dataLength = 0;
            try
            {
                if (Encoding.ASCII.GetString(b, 0, 4) != "RIFF") return false;
                if (Encoding.ASCII.GetString(b, 8, 4) != "WAVE") return false;

                int pos = 12;
                while (pos + 8 <= b.Length)
                {
                    string chunkId = Encoding.ASCII.GetString(b, pos, 4);
                    int chunkSize = BitConverter.ToInt32(b, pos + 4);
                    pos += 8;

                    if (chunkId == "fmt ")
                    {
                        ushort audioFormat = BitConverter.ToUInt16(b, pos + 0);
                        channels = BitConverter.ToUInt16(b, pos + 2);
                        sampleRate = BitConverter.ToInt32(b, pos + 4);
                        bitsPerSample = BitConverter.ToUInt16(b, pos + 14);
                        if (audioFormat != 1)
                        {
                            Debug.LogError($"SimpleWav: non-PCM({audioFormat}) not supported");
                            return false;
                        }
                    }
                    else if (chunkId == "data")
                    {
                        dataStart = pos;
                        dataLength = chunkSize;
                        return true;
                    }

                    pos += chunkSize;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SimpleWav header parse error: {e}");
                return false;
            }
            return false;
        }
    }
}

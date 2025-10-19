using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using OpenAvatarKid.Domain.Conversation;
using OpenAvatarKid.UseCases.Ports;
using OpenAvatarKid.InterfaceAdapters.Llm;

namespace OpenAvatarKid.Infrastructure.LLM
{
    public sealed class OpenAIChatClientAdapter : IChatPort
    {
        private readonly string apiKey;
        private readonly string endpoint;
        private readonly string model;
        private readonly string systemPrompt;
        private readonly float temperature;

        public OpenAIChatClientAdapter(
            string apiKey,
            string endpoint,
            string model,
            float temperature,
            string systemPrompt)
        {
            this.apiKey = apiKey;
            this.endpoint = string.IsNullOrEmpty(endpoint) ? "https://api.openai.com/v1/chat/completions" : endpoint;
            this.model = model;
            this.temperature = temperature;
            this.systemPrompt = systemPrompt;
        }

        public async Task<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct)
        {
            var payload = new
            {
                model = model,
                temperature = temperature,
                // JSON限定をより強くする場合は response_format を使う（対応モデルのみ）
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = this.systemPrompt },
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            using var req = new UnityWebRequest(endpoint, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

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

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"OpenAI Error: {req.error} {req.downloadHandler?.text}");
                // フォールバック：最小の Script を返す
                return new ConversationScript(lang, new[] { new Utterance("(LLMエラー: 応答取得に失敗)") });
            }

            var content = req.downloadHandler.text;
            var choice = JsonConvert.DeserializeObject<OpenAIResponse>(content);
            var rawJson = choice?.choices?[0]?.message?.content ?? "{}";

            var dto = LLMScriptParser.ParseStrictJson(rawJson);
            var script = LLMJsonMapper.ToDomain(dto);
            return script;
        }

        // レスポンス用 DTO（最小）
        private sealed class OpenAIResponse
        {
            public Choice[] choices;
        }

        private sealed class Choice
        {
            public Message message;
        }

        private sealed class Message
        {
            public string role;
            public string content;
        }
    }
}

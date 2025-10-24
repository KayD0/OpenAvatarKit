using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using OpenAvatarKid.Domain.Conversation;
using OpenAvatarKid.UseCases.Ports;
using OpenAvatarKid.InterfaceAdapters.Llm;
using Cysharp.Threading.Tasks;
using System.Net;

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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public async UniTask<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct)
        {
            var payload = new
            {
                model = model,
                temperature = temperature,
                // JSON限定をより強くする場合は response_format を使う（対応モデルのみ）
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = this.systemPrompt },
                    new { role = "user", content = userText },
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            using var req = new UnityWebRequest(endpoint, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // ★ 重要: 2回目以降のSSL再利用を避ける
            // req.SetRequestHeader("Connection", "close");

            // ★ 送信のawaitを正しく（自前ループ禁止）
            await req.SendWebRequest().ToUniTask(cancellationToken: ct);

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

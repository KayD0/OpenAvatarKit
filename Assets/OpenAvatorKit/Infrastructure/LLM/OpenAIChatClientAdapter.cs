using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using OpenAvatarKit.Domain.Conversation;
using OpenAvatarKit.Infrastructure.Interface;
using OpenAvatarKit.InterfaceAdapters.LLM;
using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAvatarKit.Infrastructure.LLM
{
    /// <summary>
    /// 🔹 OpenAI Chat Completions API 呼び出しアダプタ
    /// - JSON出力専用モードで LLM 応答を取得
    /// - SSL再利用バグ対策 / リトライ / タイムアウト付き
    /// - 応答は LlmScriptParser → LlmJsonMapper を経て ConversationScript に変換
    /// </summary>
    public sealed class OpenAIChatClientAdapter : IChatClientAdapter
    {
        // ==== 基本設定 ====
        private readonly string apiKey;
        private readonly string endpoint;
        private readonly string model;
        private readonly string systemPrompt;
        private readonly float temperature;

        // ==== 安定性パラメータ ====
        private readonly int requestTimeoutSec = 60;   // リクエストタイムアウト
        private readonly int maxRetry = 2;             // リトライ最大回数（計3回）
        private readonly float retryBackoffBaseSec = 1.2f; // リトライ待機(指数的増加)

        public OpenAIChatClientAdapter(
            string apiKey,
            string endpoint,
            string model,
            float temperature,
            string systemPrompt)
        {
            this.apiKey = apiKey;
            this.endpoint = string.IsNullOrEmpty(endpoint)
                ? "https://api.openai.com/v1/chat/completions"
                : endpoint;
            this.model = model;
            this.temperature = temperature;
            this.systemPrompt = systemPrompt;

            // TLSバージョンとハンドシェイク動作の安定化
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
            ServicePointManager.Expect100Continue = false;
        }

        /// <summary>
        /// 🎯 LLMにユーザー入力を送信し、ConversationScript（発話スクリプト）を生成する
        /// </summary>
        /// <param name="userText">ユーザー発話テキスト</param>
        /// <param name="lang">言語種別（日本語/英語）</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>LLM応答を反映した ConversationScript</returns>
        public async UniTask<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct)
        {
            // === ① リクエストPayload構築 ===
            var payload = new
            {
                model = model,
                temperature = temperature,
                // JSON限定応答を指示（対応モデルのみ有効）
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = this.systemPrompt },
                    new { role = "user", content = userText },
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            // === ② リトライ付き送信処理 ===
            for (int attempt = 0; attempt <= maxRetry; attempt++)
            {
                using var req = new UnityWebRequest(endpoint, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(bodyRaw),
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = requestTimeoutSec
                };

                // --- HTTPヘッダー設定 ---
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                // 2回目以降のSSL接続再利用によるエラーを回避
                req.SetRequestHeader("Connection", "close");

                try
                {
                    // === ③ 実リクエスト送信 ===
                    await req.SendWebRequest().ToUniTask(cancellationToken: ct);

#if UNITY_2020_1_OR_NEWER
                    bool isOk = (req.result == UnityWebRequest.Result.Success);
#else
                    bool isOk = !(req.isNetworkError || req.isHttpError);
#endif
                    if (!isOk)
                    {
                        // --- 通信/HTTPエラー処理 ---
                        long status = req.responseCode;
                        string errText = req.downloadHandler?.text;
                        Debug.LogError($"[OpenAI] HTTP Error (attempt {attempt + 1}): {req.error}, Status={status}, Body={errText}");

                        // リトライ条件チェック
                        if (attempt < maxRetry)
                        {
                            var wait = Mathf.Pow(retryBackoffBaseSec, attempt + 1);
                            Debug.LogWarning($"[OpenAI] Retry in {wait:F1} sec...");
                            await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: ct);
                            continue;
                        }

                        // リトライ尽きたらフォールバック返却
                        return FallbackScript(lang, $"(LLMエラー: {req.error ?? "HTTP Error"})");
                    }

                    // === ④ 正常応答の解析 ===
                    var content = req.downloadHandler.text;
                    var choice = JsonConvert.DeserializeObject<OpenAIResponse>(content);

                    // LLM応答本文（JSON想定）
                    string rawJson = choice?.choices != null && choice.choices.Length > 0
                        ? choice.choices[0]?.message?.content ?? "{}"
                        : "{}";

                    // === ⑤ JSON抽出・マッピング ===
                    // - ParseStrictJson(): テキストから厳密にJSONだけを抽出
                    // - ToDomain(): DTO → Domainモデル(ConversationScript)に変換
                    var dto = LLMScriptParser.ParseStrictJson(rawJson);
                    var script = LLMJsonMapper.ToDomain(dto, lang);

                    return script;
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning("[OpenAI] リクエストがキャンセルされました。");
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OpenAI] 例外発生 (attempt {attempt + 1}): {ex}");

                    if (attempt < maxRetry)
                    {
                        var wait = Mathf.Pow(retryBackoffBaseSec, attempt + 1);
                        Debug.LogWarning($"[OpenAI] 再試行まで {wait:F1} 秒待機します...");
                        await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: ct);
                        continue;
                    }

                    return FallbackScript(lang, "(LLM例外: 応答取得に失敗)");
                }
            }

            // 通常ここには到達しないが安全策
            return FallbackScript(lang, "(LLM不明エラー)");
        }

        /// <summary>
        /// ⚙️ フォールバック：LLMが応答しなかった場合の仮スクリプト
        /// </summary>
        private static ConversationScript FallbackScript(Lang lang, string message)
        {
            // betweenPauseSec は仕様で常に 1.2秒
            return new ConversationScript(
                lang,
                betweenPauseSec: 1.2f,
                utterances: new[]
                {
                    new Utterance(
                        text: message,
                        faceExpression: "neutral",
                        bodyExpression: "idle",
                        emotionLevel: 0.0f)
                }
            );
        }

        // ==== 内部DTO: OpenAI APIレスポンス簡略構造 ====
        [Serializable]
        private sealed class OpenAIResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private sealed class Choice
        {
            public Message message;
        }

        [Serializable]
        private sealed class Message
        {
            public string role;
            public string content;
        }
    }
}

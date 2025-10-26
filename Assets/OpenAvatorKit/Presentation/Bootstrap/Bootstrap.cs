using OpenAvatarKid.Infrastructure.LLM;
using OpenAvatarKid.Presentation.Controllers;
using OpenAvatarKid.UseCases.Interactors;
using UnityEngine;

namespace OpenAvatarKid.Presentation.Bootstrap
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("OpenAI")]
        [SerializeField] private string openAiApiKey; // ※安全運用は補足参照
        [SerializeField] private string openAiEndpoint = "https://api.openai.com/v1/chat/completions";
        [SerializeField] private string model = "gpt-4o-mini";
        [SerializeField] private string systemPrompt = @"
あなたは台本JSONのみを返す出力器です。絶対にJSON以外を出力しません。
スキーマ:
{
  ""utterances"": [
    {
      ""text"": ""こんにちは。元気そうですね！"",
      ""faceExpression"": ""joy"",
      ""bodyExpression"": ""wave_right"",
      ""emotionLevel"": 0.8
    },
    {
      ""text"": ""今日はどんなお手伝いをしましょうか？"",
      ""faceExpression"": ""neutral"",
      ""bodyExpression"": ""idle"",
      ""emotionLevel"": 0.3
    }
}

スキーマのfaceExpression、bodyExpressionには
次の中から最も自然な感情表現を選ぶこと。

| faceExpression | 意味 |
|----------------|------|
| neutral        | 無表情・通常状態 |
| joy            | 喜び・笑顔 |
| sad            | 悲しみ・落ち込み |
| angry          | 怒り・不満 |
| surprised      | 驚き・予想外の反応 |
| fear           | 恐れ・不安 |
| disgust        | 嫌悪・拒否 |
| shy            | 恥ずかしそう |
| confident      | 自信に満ちた表情 |
| thinking       | 思考・考え中 |

制約:
- JSON以外の文字を出力しない（説明文・注釈・コードフェンス禁止）
- textは200字以内、最低でも1要素

応答方針:
- ユーザの入力に一度だけ端的に応答する台本を返す
- 日本語入力なら ""lang"":""ja""
- 敬体で、丁寧だが冗長にしない";
        [Range(0f, 2f)][SerializeField] private float temperature = 0.3f;


        [Header("Scene Refs")]
        [SerializeField] private InteractionOrchestrator orchestrator;

        private void Awake()
        {
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                // 環境変数から拾う運用
                openAiApiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }

            var chat = new OpenAIChatClientAdapter(
                apiKey: openAiApiKey,
                endpoint: openAiEndpoint,
                model: model,
                temperature: temperature,
                systemPrompt
            );

            var runUseCase = new RunInteractionUseCase(chat);

            orchestrator.Inject(runUseCase);
        }
    }
}

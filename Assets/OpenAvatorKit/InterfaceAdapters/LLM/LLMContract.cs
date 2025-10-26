using System.Collections.Generic;

namespace OpenAvatarKid.InterfaceAdapters.LLM
{
    /// <summary>
    /// LLMが返すトップレベルJSONのDTO。
    /// 設計: 
    /// {
    ///   "timing": { "betweenPauseSec": number },    // 例: 1.2
    ///   "utterances": [                              // 発話配列（必須）
    ///     { "text": "...", "faceExpression": "...", "bodyExpression": "...", "emotionLevel": 0.5 },
    ///     ...
    ///   ],
    ///   "lang": "ja" | "en"                          // （任意）未設定なら外部判定を使用
    /// }
    /// </summary>
    public sealed class LLMScriptDto
    {
        public LLMTimingDto timing;               // 無音秒数（betweenPauseSec）を内包
        public List<LLMUtteranceDto> utterances;  // 発話配列
        public string lang;                       // "ja" / "en"（任意）
    }

    /// <summary>
    /// タイミング情報（Utterance間の無音秒数）。
    /// </summary>
    public sealed class LLMTimingDto
    {
        public float betweenPauseSec;             // 仕様固定: 1.2 を推奨（未設定時はMapperで1.2に補完）
    }

    /// <summary>
    /// UtteranceのDTO（LLMが出力する1発話）
    /// </summary>
    public sealed class LLMUtteranceDto
    {
        /// <summary>発話テキスト</summary>
        public string text;

        /// <summary>表情タグ（例: neutral/joy/sad/angry/surprised/fear/disgust/shy/confident/thinking）</summary>
        public string faceExpression;

        /// <summary>体モーション（例: idle/wave_right/nod など。存在しない場合は idle を採用）</summary>
        public string bodyExpression;

        /// <summary>感情強度 0.0〜1.0（範囲外はMapper側でClamp）</summary>
        public float? emotionLevel;
    }
}

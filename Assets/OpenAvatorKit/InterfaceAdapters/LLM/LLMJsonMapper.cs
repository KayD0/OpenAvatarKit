using System;
using System.Collections.Generic;
using OpenAvatarKid.Domain.Conversation;

namespace OpenAvatarKid.InterfaceAdapters.LLM
{
    /// <summary>
    /// LLMのDTOをDomainモデル（ConversationScript/Utterance）へマッピング。
    /// ・null安全
    /// ・デフォルト補完（betweenPauseSec=1.2f、表情/モーション/強度）
    /// ・文字の正規化（小文字化など最小限）
    /// </summary>
    public static class LLMJsonMapper
    {
        // 設計上の固定既定値
        private const float DefaultBetweenPauseSec = 1.2f;
        private const string DefaultFace = "neutral";
        private const string DefaultBody = "idle";
        private const float DefaultEmotion = 0.3f;

        /// <summary>
        /// 既存の外部判定 Lang を優先（STTやUI設定）。DTO.lang は補助的にのみ使用したい場合はこちらを推奨。
        /// </summary>
        public static ConversationScript ToDomain(LLMScriptDto dto, Lang lang)
        {
            if (dto == null)
            {
                return Fallback(lang, "(LLMエラー: 空のレスポンス)");
            }

            // timing.betweenPauseSec を補完（未指定/0以下は 1.2f を採用）
            var pause = (dto.timing != null && dto.timing.betweenPauseSec > 0f)
                ? dto.timing.betweenPauseSec
                : DefaultBetweenPauseSec;

            var utterances = new List<Utterance>();

            if (dto.utterances != null && dto.utterances.Count > 0)
            {
                foreach (var u in dto.utterances)
                {
                    if (u == null) continue;

                    // 値の補完と正規化
                    var text = u.text ?? string.Empty;
                    var face = string.IsNullOrWhiteSpace(u.faceExpression) ? DefaultFace : u.faceExpression.Trim();
                    var body = string.IsNullOrWhiteSpace(u.bodyExpression) ? DefaultBody : u.bodyExpression.Trim();
                    var emo = Clamp01(u.emotionLevel ?? DefaultEmotion);

                    // Domain Utterance へ
                    utterances.Add(new Utterance(
                        text: text,
                        faceExpression: face,
                        bodyExpression: body,
                        emotionLevel: emo));
                }
            }

            // 発話が0件ならフォールバックを1件差し込む
            if (utterances.Count == 0)
            {
                utterances.Add(new Utterance(
                    text: "(応答なし)",
                    faceExpression: DefaultFace,
                    bodyExpression: DefaultBody,
                    emotionLevel: 0.0f));
            }

            return new ConversationScript(lang, pause, utterances.AsReadOnly());
        }

        /// <summary>
        /// DTO内のlangを使って Lang を決めたい場合のオーバーロード。
        /// （既存コード互換のため残置。通常は外部判定版を使うことを推奨）
        /// </summary>
        public static ConversationScript ToDomain(LLMScriptDto dto)
        {
            var lang = ToLang(dto?.lang);
            return ToDomain(dto, lang);
        }

        private static Lang ToLang(string code)
        {
            switch (code?.Trim().ToLowerInvariant())
            {
                case "en": return Lang.En;
                case "ja":
                default: return Lang.Ja;
            }
        }

        private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);

        private static ConversationScript Fallback(Lang lang, string message)
        {
            return new ConversationScript(
                lang,
                DefaultBetweenPauseSec,
                new[]
                {
                    new Utterance(message, DefaultFace, DefaultBody, 0.0f)
                });
        }
    }
}

using OpenAvatarKid.Domain.Conversation;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenAvatarKid.Domain.Services
{
    /// <summary>
    /// ConversationScript の最終安全化（クランプ/正規化/既定値補完）を行うユーティリティ。
    /// - Text: 文字数上限、制御文字除去、前後空白トリム
    /// - EmotionLevel: 0.0〜1.0 にClamp
    /// - Face/Body: null/空→既定値、余分空白除去、小文字化
    /// - BetweenPauseSec: 0以下なら 1.2 に補正
    /// </summary>
    public static class ScriptValidator
    {
        // 仕様上の推奨既定値
        private const float DefaultBetweenPauseSec = 1.2f;
        private const string DefaultFace = "neutral";
        private const string DefaultBody = "idle";
        private const float DefaultEmotion = 0.3f;

        /// <summary>
        /// ConversationScript を安全化して返す。
        /// </summary>
        /// <param name="src">元スクリプト</param>
        /// <param name="maxCharsPerUtterance">1発話あたりの最大文字数（超過分は切り捨て）</param>
        public static ConversationScript Clamp(ConversationScript src, int maxCharsPerUtterance = 200)
        {
            if (src == null)
            {
                return new ConversationScript(
                    language: Lang.Ja,
                    betweenPauseSec: DefaultBetweenPauseSec,
                    utterances: new[]
                    {
                        new Utterance("(空のスクリプト)", DefaultFace, DefaultBody, 0.0f)
                    }
                );
            }

            // betweenPauseSec の補正（0以下は既定値）
            var pause = (src.BetweenPauseSec > 0f) ? src.BetweenPauseSec : DefaultBetweenPauseSec;

            // Utterances が null/空でも最低1件返す
            var inputUtts = src.Utterances ?? Array.Empty<Utterance>();
            var fixedList = new List<Utterance>(Math.Max(1, inputUtts.Count));

            foreach (var u in inputUtts)
            {
                // null ガード
                var text = u?.Text ?? string.Empty;
                var face = u?.FaceExpression ?? DefaultFace;
                var body = u?.BodyExpression ?? DefaultBody;
                var emo = (u != null) ? u.EmotionLevel : DefaultEmotion;

                // 文字列の正規化
                text = NormalizeText(text, maxCharsPerUtterance);
                face = NormalizeTag(face, DefaultFace);
                body = NormalizeTag(body, DefaultBody);

                // 感情強度 Clamp（0〜1）
                emo = Math.Clamp(emo, 0f, 1f);

                fixedList.Add(new Utterance(text, face, body, emo));
            }

            if (fixedList.Count == 0)
            {
                fixedList.Add(new Utterance("(応答なし)", DefaultFace, DefaultBody, 0.0f));
            }

            return new ConversationScript(src.Language, pause, fixedList.AsReadOnly());
        }

        /// <summary>
        /// テキストをトリムし、制御文字を除去し、最大長で切り詰める。
        /// </summary>
        private static string NormalizeText(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // 制御文字（改行は許可する場合は \r?\n を残しても良い）
            // ここでは、タブ/復帰含め制御文字を除去し、改行は「1つのスペース」に正規化。
            s = s.Replace("\r\n", "\n").Replace('\r', '\n');
            s = Regex.Replace(s, @"\p{C}", "");           // 制御文字を除去
            s = Regex.Replace(s, @"\s*\n\s*", " ");       // 改行をスペースに
            s = s.Trim();

            if (maxLen > 0 && s.Length > maxLen)
            {
                s = s.Substring(0, maxLen);
            }
            return s;
        }

        /// <summary>
        /// タグ文字列（face/body）をトリムし小文字化。空の場合は既定値に置換。
        /// </summary>
        private static string NormalizeTag(string s, string fallback)
        {
            var t = (s ?? string.Empty).Trim();
            if (t.Length == 0) return fallback;
            return t.ToLowerInvariant();
        }
    }
}

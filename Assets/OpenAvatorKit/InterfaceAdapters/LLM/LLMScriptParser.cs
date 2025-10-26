using Newtonsoft.Json;

namespace OpenAvatarKid.InterfaceAdapters.LLM
{
    /// <summary>
    /// LLM応答テキストから「純粋なJSONオブジェクト」を厳密抽出してDTO化する。
    /// - まずは素直にDeserializeを試行
    /// - 失敗したら { と } のネストを見ながら最初のJSONブロックを抽出
    /// - 抽出に失敗したら空DTO（Mapper側でフォールバック）
    /// </summary>
    public static class LLMScriptParser
    {
        public static LLMScriptDto ParseStrictJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new LLMScriptDto
                {
                    timing = new LLMTimingDto { betweenPauseSec = 0f },
                    utterances = null,
                    lang = null
                };
            }

            // ① 素直に逆シリアライズ
            try
            {
                return JsonConvert.DeserializeObject<LLMScriptDto>(raw);
            }
            catch
            {
                // 失敗時は次の段階へ
            }

            // ② コードフェンスや前後の説明文が混じる場合にJSON部分を抽出
            var json = ExtractFirstJsonObject(raw);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<LLMScriptDto>(json);
                }
                catch
                {
                    // 最後の砦：空DTO
                }
            }

            return new LLMScriptDto
            {
                timing = new LLMTimingDto { betweenPauseSec = 0f },
                utterances = null,
                lang = null
            };
        }

        /// <summary>
        /// テキスト中の最初の { ... } の完全ブロックをネスト追跡で抜き出す。
        /// 例: "説明文```{...}```余白" → {...}
        /// </summary>
        private static string ExtractFirstJsonObject(string s)
        {
            int start = s.IndexOf('{');
            if (start < 0) return null;

            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        int end = i;
                        return s.Substring(start, end - start + 1);
                    }
                }
            }
            return null;
        }
    }
}

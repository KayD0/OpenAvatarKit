using Newtonsoft.Json;
using OpenAvatarKid.InterfaceAdapters.LLM;

namespace OpenAvatarKid.InterfaceAdapters.Llm
{
    public static class LLMScriptParser
    {
        public static LLMScriptDto ParseStrictJson(string rawJson)
        {
            // 受信テキストから JSON 部分だけを抜く処理は後で強化
            // ここでは「LLMは必ず JSON だけ返す」前提（SystemPrompt で強制）
            return JsonConvert.DeserializeObject<LLMScriptDto>(rawJson);
        }
    }
}

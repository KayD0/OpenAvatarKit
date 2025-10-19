using Newtonsoft.Json;
using OpenAvatarKid.InterfaceAdapters.LLM;

namespace OpenAvatarKid.InterfaceAdapters.Llm
{
    public static class LLMScriptParser
    {
        public static LLMScriptDto ParseStrictJson(string rawJson)
        {
            // ��M�e�L�X�g���� JSON ���������𔲂������͌�ŋ���
            // �����ł́uLLM�͕K�� JSON �����Ԃ��v�O��iSystemPrompt �ŋ����j
            return JsonConvert.DeserializeObject<LLMScriptDto>(rawJson);
        }
    }
}

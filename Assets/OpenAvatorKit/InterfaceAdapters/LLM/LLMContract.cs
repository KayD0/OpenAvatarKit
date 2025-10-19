using System.Collections.Generic;

namespace OpenAvatarKid.InterfaceAdapters.LLM
{
    // LLM ����Ԃ��Ă��� "��" JSON �̑z�� DTO
    public sealed class LLMScriptDto
    {
        public string lang;              // "ja" / "en"
        public List<LlmUtteranceDto> utterances;
    }

    public sealed class LlmUtteranceDto
    {
        public string text;
    }
}

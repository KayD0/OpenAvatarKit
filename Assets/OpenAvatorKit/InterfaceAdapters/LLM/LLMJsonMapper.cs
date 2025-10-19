using System.Collections.Generic;
using OpenAvatarKid.Domain.Conversation;
using OpenAvatarKid.InterfaceAdapters.LLM;

namespace OpenAvatarKid.InterfaceAdapters.Llm
{
    public static class LLMJsonMapper
    {
        public static ConversationScript ToDomain(LLMScriptDto dto)
        {
            var lang = (dto.lang?.ToLower()) switch
            {
                "ja" => Lang.Ja,
                "en" => Lang.En,
                _ => Lang.Ja
            };

            var list = new List<Utterance>();
            if (dto.utterances != null)
            {
                foreach (var u in dto.utterances)
                {
                    list.Add(new Utterance(u?.text ?? string.Empty));
                }
            }
            return new ConversationScript(lang, list.AsReadOnly());
        }
    }
}

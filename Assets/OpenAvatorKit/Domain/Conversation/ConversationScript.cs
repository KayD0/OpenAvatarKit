using System.Collections.Generic;

namespace OpenAvatarKid.Domain.Conversation
{
    public enum Lang { Ja, En }

    public sealed class ConversationScript
    {
        public Lang Language { get; }
        public IReadOnlyList<Utterance> Utterances { get; }

        public ConversationScript(Lang language, IReadOnlyList<Utterance> utterances)
        {
            Language = language;
            Utterances = utterances;
        }
    }
}

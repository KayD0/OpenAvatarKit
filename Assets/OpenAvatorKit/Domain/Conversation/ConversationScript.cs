using System;
using System.Collections.Generic;

namespace OpenAvatarKid.Domain.Conversation
{
    public enum Lang { Ja, En }

    /// <summary>
    /// LLM�����̑�{�S�́i�������b�{���ʃ^�C�~���O�j
    /// </summary>
    [Serializable]
    public sealed class ConversationScript
    {
        public Lang Language { get; }
        public float BetweenPauseSec { get; }   // �eUtterance�Ԃ̖����b��
        public IReadOnlyList<Utterance> Utterances { get; }

        public ConversationScript(Lang language, float betweenPauseSec, IReadOnlyList<Utterance> utterances)
        {
            Language = language;
            BetweenPauseSec = betweenPauseSec;
            Utterances = utterances ?? Array.Empty<Utterance>();
        }

        public override string ToString()
            => $"[ConversationScript] Lang={Language}, Pause={BetweenPauseSec}s, Utterances={Utterances.Count}";
    }
}

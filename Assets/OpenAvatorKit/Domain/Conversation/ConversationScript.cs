using System;
using System.Collections.Generic;

namespace OpenAvatarKid.Domain.Conversation
{
    public enum Lang { Ja, En }

    /// <summary>
    /// LLM応答の台本全体（複数発話＋共通タイミング）
    /// </summary>
    [Serializable]
    public sealed class ConversationScript
    {
        public Lang Language { get; }
        public float BetweenPauseSec { get; }   // 各Utterance間の無音秒数
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

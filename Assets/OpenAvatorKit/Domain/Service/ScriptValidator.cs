using System.Linq;
using OpenAvatarKid.Domain.Conversation;

namespace OpenAvatarKid.Domain.Services
{
    public static class ScriptValidator
    {
        public static ConversationScript Clamp(ConversationScript src, int maxCharsPerUtterance = 200)
        {
            var fixedUtts = src.Utterances
                .Select(u => new Utterance(
                    (u.Text ?? string.Empty).Trim().Substring(0, System.Math.Min(u.Text?.Length ?? 0, maxCharsPerUtterance))
                ))
                .ToList()
                .AsReadOnly();
            return new ConversationScript(src.Language, fixedUtts);
        }
    }
}

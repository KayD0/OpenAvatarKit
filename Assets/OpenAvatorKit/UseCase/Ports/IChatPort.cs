using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;

namespace OpenAvatarKid.UseCases.Ports
{
    public interface IChatPort
    {
        /// <summary>
        /// ƒ†[ƒU“ü—Í‚ğ‚à‚Æ‚É LLM ‚©‚ç‘ä–{ Script ‚ğæ“¾
        /// </summary>
        UniTask<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct);
    }
}

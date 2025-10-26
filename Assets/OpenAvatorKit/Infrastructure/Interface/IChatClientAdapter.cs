using Cysharp.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;
using System.Threading;

namespace OpenAvatarKid.Infrastructure.Interface
{
    public interface IChatClientAdapter
    {
        /// <summary>
        /// ƒ†[ƒU“ü—Í‚ğ‚à‚Æ‚É LLM ‚©‚ç‘ä–{ Script ‚ğæ“¾
        /// </summary>
        UniTask<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct);
    }
}

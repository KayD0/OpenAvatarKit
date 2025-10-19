using System.Threading;
using System.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;

namespace OpenAvatarKid.UseCases.Ports
{
    public interface IChatPort
    {
        /// <summary>
        /// ƒ†[ƒU“ü—Í‚ğ‚à‚Æ‚É LLM ‚©‚ç‘ä–{ Script ‚ğæ“¾
        /// </summary>
        Task<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct);
    }
}

using Cysharp.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;
using System.Threading;

namespace OpenAvatarKid.UseCases.Ports
{
    public interface IChatPort
    {
        /// <summary>
        /// ユーザ入力をもとに LLM から台本 Script を取得
        /// </summary>
        UniTask<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct);
    }
}

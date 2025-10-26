using System.Threading;
using System.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;
using OpenAvatarKid.Domain.Services;
using OpenAvatarKid.Infrastructure.Interface;

namespace OpenAvatarKid.UseCases.Interactors
{
    public sealed class RunInteractionUseCase
    {
        private readonly IChatClientAdapter chat;

        public RunInteractionUseCase(IChatClientAdapter chat)
        {
            this.chat = chat;
        }

        public async Task<ConversationScript> ExecuteAsync(string userText, Lang lang, CancellationToken ct)
        {
            var script = await chat.GetScriptAsync(userText, lang, ct);
            return ScriptValidator.Clamp(script, maxCharsPerUtterance: 200);
        }
    }
}

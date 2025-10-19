using System.Threading;
using System.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;
using OpenAvatarKid.Domain.Services;
using OpenAvatarKid.UseCases.Ports;

namespace OpenAvatarKid.UseCases.Interactors
{
    public sealed class RunInteractionUseCase
    {
        private readonly IChatPort chat;

        public RunInteractionUseCase(IChatPort chat)
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

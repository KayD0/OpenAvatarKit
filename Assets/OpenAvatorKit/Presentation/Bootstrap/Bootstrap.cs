// Assets/AIKo/Presentation/Bootstrap/AikoBootstrap.cs
using UnityEngine;
using OpenAvatarKid.Infrastructure.LLM;
using OpenAvatarKid.UseCases.Interactors;
using OpenAvatarKid.Presentation.Controllers;

namespace OpenAvatarKid.Presentation.Bootstrap
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("OpenAI")]
        [SerializeField] private string openAiApiKey; // �����S�^�p�͕⑫�Q��
        [SerializeField] private string openAiEndpoint = "https://api.openai.com/v1/chat/completions";
        [SerializeField] private string model = "gpt-4o-mini";
        [SerializeField] private string systemPrompt = @"
���Ȃ��͑�{JSON�݂̂�Ԃ��o�͊�ł��B��΂�JSON�ȊO���o�͂��܂���B
�X�L�[�}:
{
  ""lang"": ""ja"" | ""en"",
  ""utterances"": [
    { ""text"": ""..."" },
    { ""text"": ""..."" }
  ]
}
����:
- JSON�ȊO�̕������o�͂��Ȃ��i�������E���߁E�R�[�h�t�F���X�֎~�j
- text��200���ȓ��A�Œ�ł�1�v�f

�������j:
- ���[�U�̓��͂Ɉ�x�����[�I�ɉ��������{��Ԃ�
- ���{����͂Ȃ� ""lang"":""ja""
- �h�̂ŁA���J�����璷�ɂ��Ȃ�";
        [Range(0f, 2f)][SerializeField] private float temperature = 0.3f;


        [Header("Scene Refs")]
        [SerializeField] private InteractionOrchestrator orchestrator;

        private void Awake()
        {
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                // ���ϐ�����E���^�p
                openAiApiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }

            var chat = new OpenAIChatClientAdapter(
                apiKey: openAiApiKey,
                endpoint: openAiEndpoint,
                model: model,
                temperature: temperature,
                systemPrompt
            );

            var runUseCase = new RunInteractionUseCase(chat);

            orchestrator.Inject(runUseCase);
        }
    }
}

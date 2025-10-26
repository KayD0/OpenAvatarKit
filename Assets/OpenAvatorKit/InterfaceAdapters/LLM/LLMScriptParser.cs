using Newtonsoft.Json;

namespace OpenAvatarKid.InterfaceAdapters.LLM
{
    /// <summary>
    /// LLM�����e�L�X�g����u������JSON�I�u�W�F�N�g�v���������o����DTO������B
    /// - �܂��͑f����Deserialize�����s
    /// - ���s������ { �� } �̃l�X�g�����Ȃ���ŏ���JSON�u���b�N�𒊏o
    /// - ���o�Ɏ��s�������DTO�iMapper���Ńt�H�[���o�b�N�j
    /// </summary>
    public static class LLMScriptParser
    {
        public static LLMScriptDto ParseStrictJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new LLMScriptDto
                {
                    timing = new LLMTimingDto { betweenPauseSec = 0f },
                    utterances = null,
                    lang = null
                };
            }

            // �@ �f���ɋt�V���A���C�Y
            try
            {
                return JsonConvert.DeserializeObject<LLMScriptDto>(raw);
            }
            catch
            {
                // ���s���͎��̒i�K��
            }

            // �A �R�[�h�t�F���X��O��̐�������������ꍇ��JSON�����𒊏o
            var json = ExtractFirstJsonObject(raw);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<LLMScriptDto>(json);
                }
                catch
                {
                    // �Ō�̍ԁF��DTO
                }
            }

            return new LLMScriptDto
            {
                timing = new LLMTimingDto { betweenPauseSec = 0f },
                utterances = null,
                lang = null
            };
        }

        /// <summary>
        /// �e�L�X�g���̍ŏ��� { ... } �̊��S�u���b�N���l�X�g�ǐՂŔ����o���B
        /// ��: "������```{...}```�]��" �� {...}
        /// </summary>
        private static string ExtractFirstJsonObject(string s)
        {
            int start = s.IndexOf('{');
            if (start < 0) return null;

            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        int end = i;
                        return s.Substring(start, end - start + 1);
                    }
                }
            }
            return null;
        }
    }
}

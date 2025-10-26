## フォルダ構成
```
Assets/
├─ ThirdParty/                              # 外部SDK・パッケージ類
│  ├─ UniVRM/
│  ├─ ChatdollKit/
│  ├─ OpenAI/
│  └─ (VOICEVOX / Azure / Whisper / etc.)
│
└─ AvatarChat/
   ├─ Domain/                               # Unity非依存モデル・ルール
   │  ├─ Conversation/
   │  │  ├─ ConversationScript.cs           # 台本モデル（Utterance配列）
   │  │  ├─ Utterance.cs                    # Text / Emotion / Animation
   │  │  ├─ EmotionTag.cs / VoiceSpec.cs
   │  │  └─ Constraints.cs
   │  ├─ LipSync/
   │  │  ├─ Phoneme.cs / Viseme.cs
   │  │  └─ LipSyncRule.cs
   │  └─ Shared/
   │     └─ ResultCode.cs / AppConstants.cs
   │
   ├─ UseCases/                             # アプリケーションロジック層
   │  ├─ Interactors/
   │  │  ├─ HandleUserInputUseCase.cs       # 入力→LLM呼び出し→Script化
   │  │  ├─ GenerateResponseUseCase.cs
   │  │  ├─ PlaySpeechUseCase.cs
   │  │  └─ UpdateLipSyncUseCase.cs
   │  ├─ Services/                          # ロジック補助
   │  │  ├─ EmotionMapper.cs
   │  │  ├─ LipSyncService.cs
   │  │  └─ ValidationService.cs
   │  └─ Extensions/                        # 内部共通拡張など
   │
   ├─ InterfaceAdapters/                    # DTO変換・検証・Presenter
   │  ├─ LLM/
   │  │  ├─ LlmContract.cs
   │  │  ├─ LlmScriptParser.cs
   │  │  └─ LlmJsonMapper.cs
   │  ├─ TTS/
   │  │  ├─ TtsContract.cs / TtsMapper.cs / TtsValidator.cs
   │  ├─ STT/
   │  │  ├─ SttContract.cs / SttMapper.cs / SttValidator.cs
   │  ├─ Presenters/
   │  │  ├─ ChatPresenter.cs
   │  │  └─ AvatarPresenter.cs
   │  └─ Telemetry/
   │     └─ InteractionMetrics.cs
   │
   ├─ Infrastructure/                       # 具体実装＋契約
   │  ├─ Interface/                         # ← ここに全ての抽象I/Fを集約
   │  │  ├─ IChatClientAdapter.cs           # LLM呼び出し契約
   │  │  ├─ ITtsClientAdapter.cs            # 音声合成契約
   │  │  ├─ ISttClientAdapter.cs            # 音声認識契約
   │  │  ├─ IEmotionAdapter.cs              # VRM表情制御
   │  │  ├─ IAnimationAdapter.cs            # アニメ制御
   │  │  ├─ IAudioInputAdapter.cs           # 音声入力（RMS/音素）
   │  │  └─ IMemoryRepository.cs            # 長期メモリ管理
   │  │
   │  ├─ LLM/
   │  │  ├─ OpenAIChatClientAdapter.cs      # IChatClientAdapter実装
   │  │  └─ ClaudeChatClientAdapter.cs
   │  ├─ TTS/
   │  │  ├─ VoiceVoxTtsClientAdapter.cs     # ITtsClientAdapter実装
   │  │  └─ OpenAiTtsClientAdapter.cs
   │  ├─ STT/
   │  │  ├─ WhisperSttClientAdapter.cs      # ISttClientAdapter実装
   │  │  └─ AzureSttClientAdapter.cs
   │  ├─ Avatar/
   │  │  ├─ VrmEmotionAdapter.cs  # IEmotionAdapter
   │  │  ├─ AnimatorAdapter.cs    # IAnimationAdapter
   │  │  └─ VrmLipSyncAdapter.cs  # IAudioInputAdapter
   │  ├─ Memory/
   │  │  └─ LocalMemoryRepository.cs
   │  └─ Audio/
   │     ├─ UnityAudioInputAdapter.cs
   │     └─ UnityAudioPlaybackAdapter.cs
   │
   ├─ Presentation/                         # Unity側のMonoBehaviour/UI
   │  ├─ Orchestration/                     # 全体指揮
   │  │  ├─ DialogOrchestrator.cs
   │  │  └─ InteractionStateMachine.cs
   │  ├─ IO/                                # 入出力専用（片方向）
   │  │  ├─ SpeechInputListener.cs
   │  │  ├─ SpeechOutputDriver.cs
   │  │  └─ WakeWordListener.cs
   │  ├─ Synchronizers/                     # 双方向同期
   │  │  ├─ LipSyncSynchronizer.cs
   │  │  ├─ ExpressionSynchronizer.cs
   │  │  └─ AvatarStateSynchronizer.cs
   │  ├─ Bridges/                           # レイヤ間の橋渡し
   │  │  ├─ ChatBridge.cs
   │  │  ├─ ToolBridge.cs
   │  │  └─ MemoryBridge.cs
   │  ├─ UI/                                # Unity UI
   │  │  ├─ ChatPanelUI.cs / LogPanel.cs / EmotionHUD.cs
   │  │  └─ Prefabs/ (App.prefab, HUD.prefab)
   │  └─ Bootstrap/
   │     └─ AvatarChatBootstrap.cs
   │
   ├─ Resources/
   │  ├─ Prompts/
   │  │  └─ SystemPrompt.txt
   │  ├─ Profiles/
   │  │  ├─ ExpressionProfile.asset
   │  │  ├─ LipSyncProfile.asset
   │  │  └─ AnimationProfile.asset
   │  └─ Config/
   │     └─ InteractionConfig.asset
   │
   ├─ Addressables/
   │  ├─ Animations/ (WaveRight.anim, Nod.anim ...)
   │  └─ Audio/PreGenerated/                # 定型TTSキャッシュ
   │
   ├─ Scenes/
   │  ├─ Main.unity
   │  └─ Demo.unity
   │
   └─ Editor/
      ├─ Validator.cs
      └─ AutoBindTool.cs
```

## 概念的フロー
```
[User Input]
  ↓
(ChatPanelUI / SpeechInputListener)
  ↓
ChatBridge → DialogOrchestrator
  ↓
HandleUserInputUseCase
  ↓
IChatClientAdapter（OpenAIChatClientAdapter）→ JSON → LlmParser → Script
  ↓
PlaySpeechUseCase
  ↓
ITtsClientAdapter（VoiceVox / OpenAI TTS）
  ↓
SpeechOutputDriver（再生 & OnRms発火）
  ↓
LipSyncSynchronizer（口パク） + ExpressionSynchronizer（感情表情）
  ↓
[Avatar 発話完了]
```

## フロー日本語解説
```
───────────────────────────────────────────────
① ユーザー入力（話す or 打ち込む）
───────────────────────────────────────────────
🎙 マイク入力
　→ SpeechInputListener（音声入力監視）
　　　└─ 音声を録音 → STTエンジンに送信
　　　　　↓
　　　　文字起こし結果（テキスト）

⌨️ テキスト入力
　→ SimpleChatUI（チャット入力欄）
　　　└─ 入力された文字列を送信
　　　　　↓
　　　　テキストを取得

　　　※どちらの入力も最終的に同じ処理へ流れる
───────────────────────────────────────────────
② 対話の指揮・状態管理
───────────────────────────────────────────────
🧩 InteractionOrchestrator（会話の司令塔）
　・「聞く→考える→話す」の流れを制御  
　・話している途中で割込みがあれば中断処理  
　・状態を管理（Idle / Thinking / Speaking）

　↓
　テキスト入力をもとに「AIへの質問」として処理開始
───────────────────────────────────────────────
③ AI応答生成（LLM呼び出し）
───────────────────────────────────────────────
🤖 HandleUserInputUseCase（AI応答を作る役割）
　└─ OpenAIChatClientAdapter に問い合わせ
　　　↓
　　LLM（例: GPTやClaude）が返す応答JSONを取得

🧾 LlmScriptParser（検証）
　・不正なJSONを補正、必要な情報だけ抽出  
　（発話文・感情・モーションなど）

🧭 LlmJsonMapper（変換）
　・DTO（外部データ）→ Script（内部形式）に変換  
　　＝ AIの返答をアバターが理解できる形に整理

🗂 PlanFactory（実行計画の作成）
　・発話内容・感情・動作を並べた台本を生成  
　　例）「こんにちは！（笑顔で手を振る）」
───────────────────────────────────────────────
④ 出力処理（話す・表情・口パク）
───────────────────────────────────────────────
💬 PlaySpeechUseCase（発話処理）
　└─ OpenAiTtsClientAdapter / VoiceVoxTtsClientAdapter  
　　　→ 文字列を音声データ（WAV/PCM）に変換  
　　　→ SpeechOutputDriver が再生（AudioSource）

🎧 SpeechOutputDriver（音声再生）
　・再生中の音量（RMS）をリアルタイム取得  
　・音の強弱に合わせて「口パクイベント」を発火

😃 ExpressionSynchronizer（表情制御）
　・感情タグ（joy, sad など）を VRM BlendShape に反映  
　・笑う・怒るなどの表情を切り替え

💋 LipSyncSynchronizer（口パク同期）
　・SpeechOutputDriver から送られる音量/音素を受け取り  
　・VRM の口形状（A/I/U/E/O）をリアルタイム更新
───────────────────────────────────────────────
⑤ アバター出力（見た目と音声）
───────────────────────────────────────────────
🗣 アバターが感情を込めて話す
　＋口パク、表情、モーションが同時に再生

💫 InteractionOrchestrator が再生完了を検知
　→ 状態を Idle に戻し、次の入力を待機
───────────────────────────────────────────────
```

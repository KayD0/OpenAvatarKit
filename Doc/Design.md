## フォルダ構成
```
Assets/
├─ ThirdParty/                              # 外部（UniVRM/ChatdollKit/SDK類）
│  ├─ UniVRM/
│  └─ (OpenAI SDK など)
│
└─ AIKo/                                    # 自前コード
   ├─ Domain/                               # Unity非依存の純粋モデル・規則
   │  ├─ Conversation/
   │  │  ├─ Script.cs                      # LLM台本のドメインモデル（Lang, Utterance…）
   │  │  ├─ Utterance.cs                   # text, VoiceSpec, Emotion, AnimCues
   │  │  ├─ VoiceSpec.cs / EmotionTag.cs / AnimCue.cs
   │  │  └─ Constraints.cs                 # 文字数/範囲などの規約値
   │  ├─ LipSync/
   │  │  ├─ Viseme.cs / Phoneme.cs         # 口形状と音素モデル
   │  │  └─ LipSyncRule.cs
   │  └─ Services/
   │     └─ ScriptValidator.cs             # スキーマ検証の純粋ロジック
   │
   ├─ UseCases/                             # アプリケーション（台本実行/口パク）
   │  ├─ Interactors/
   │  │  ├─ RunInteractionUseCase.cs       # 台本→TTS/Emotion/Animの順次実行
   │  │  ├─ DriveLipSyncByAudioUseCase.cs  # 音量駆動リップシンク
   │  │  └─ DriveLipSyncByPhonemeUseCase.cs# 音素駆動（対応TTS用）
   │  ├─ Plan/
   │  │  ├─ ExecutionPlan.cs               # UtterancePlan/全体Plan
   │  │  └─ PlanFactory.cs                 # LLM正規化結果→Plan生成
   │  └─ Ports/                            # 抽象インターフェース（外部非依存）
   │     ├─ IChatPort.cs                   # userText→LLM JSON
   │     ├─ ITtsPort.cs                    # text→wav / 再生
   │     ├─ IEmotionPort.cs                # 表情適用
   │     ├─ IAnimationPort.cs              # Animator制御
   │     ├─ IAudioLevelPort.cs             # RMS取得
   │     └─ IVisemeApplyPort.cs            # 口形状適用
   │
   ├─ InterfaceAdapters/                    # 翻訳層（DTO/Mapper/Presenter）
   │  ├─ LLM/
   │  │  ├─ LLMContract.cs                 # LLMが返す“生”JSONのDTO
   │  │  ├─ LLMScriptParser.cs             # 受信→検証→正規化（ゴミ除去/クランプ）
   │  │  └─ LLMJsonMapper.cs               # 正規化DTO→Domain Script 変換
   │  ├─ Presenters/
   │  │  └─ InteractionPresenter.cs        # UseCase出力→ViewModel（任意）
   │  └─ Telemetry/
   │     └─ InteractionMetrics.cs          # 解析/失敗率/遅延記録（任意）
   │
   ├─ Infrastructure/                       # 具体実装（Unity/SDK/VRM）
   │  ├─ LLM/
   │  │  └─ OpenAIChatClientAdapter.cs     # IChatPort 実装（REST/SDK）
   │  ├─ TTS/
   │  │  ├─ VoiceVoxTtsAdapter.cs          # ITtsPort（合成＋再生）
   │  │  └─ OpenAiTtsAdapter.cs
   │  ├─ Avatar/                            # VRM接点（ここだけVRM APIに依存）
   │  │  ├─ VrmEmotionAdapter.cs           # IEmotionPort（VRM0/1自動）
   │  │  ├─ VrmVisemeAdapter.cs            # IVisemeApplyPort（A/I/U/E/O）
   │  │  └─ AnimatorAnimationAdapter.cs    # IAnimationPort（Animator）
   │  ├─ Audio/
   │  │  └─ UnityAudioLevelAdapter.cs      # IAudioLevelPort（RMS）
   │  └─ Loading/
   │     └─ VrmRuntimeLoader.cs            # VRM0/1判別ロード
   │
   ├─ Presentation/                         # Unityの入口（Mono/UI/配線）
   │  ├─ Controllers/
   │  │  ├─ InteractionOrchestrator.cs     # 監督：入力→UseCase起動/割込/LipSync Tick
   │  │  ├─ SpeechToTextListener.cs        # 音声→文字イベント
   │  │  └─ SimpleChatUI.cs                # 文字入力→Orchestrator
   │  ├─ Bootstrap/
   │  │  └─ AikoBootstrap.cs               # 依存組立（手動DI）
   │  └─ UI/
   │     ├─ LogPanel.cs / EmotionHUD.cs
   │     └─ Prefabs/ (App.prefab, HUD.prefab)
   │
   ├─ Resources/
   │  ├─ Prompts/
   │  │  └─ SystemPrompt.txt               # JSON固定出力の指示文
   │  ├─ Profiles/
   │  │  ├─ ExpressionProfile.asset        # 感情→表情マッピング
   │  │  ├─ LipSyncProfile.asset           # Viseme重み/スムージング
   │  │  └─ AnimationProfile.asset         # クリップ名/レイヤ整合
   │  └─ Config/
   │     └─ InteractionConfig.asset        # 遅延/閾値/制限値
   │
   ├─ Addressables/
   │  ├─ Animations/ (WaveRight.anim, Nod.anim...)
   │  └─ Audio/PreGenerated/               # 定型TTSキャッシュ
   │
   ├─ Scenes/ (Main.unity, Demo.unity)
   └─ Editor/（検証ツール/バリデータなど）


## 依存の方向性
OpenAvatarKid.Domain            # 参照なし
OpenAvatarKid.UseCases          # 参照: Domain
OpenAvatarKid.InterfaceAdapters # 参照: UseCases, Domain
OpenAvatarKid.Infrastructure    # 参照: InterfaceAdapters, UseCases, Domain, UnityEngine, VRM
OpenAvatarKid.Presentation      # 参照: InterfaceAdapters, UseCases, Domain, Unity UI
```

## データフロー
```
[User]
  ├─🎙 Mic ──> SpeechToTextListener ──┐
  └─⌨️ UI.Input ─> SimpleChatUI ──────┘
                                      ▼
                            InteractionOrchestrator
                                      │  （割込・状態管理）
                                      ▼
                           RunInteractionUseCase (アプリ中核)
                                      │
                                      ▼
                           ┌──────── LLM 台本パイプライン ────────┐
                           │                                      │
                           │   (1) IChatPort.GetScriptJsonAsync() │  ← LLM呼び出し
                           │          │                           │
                           │          ▼                           │
                           │   ┌─────────────────────────────┐    │
                           │   │  LlmScriptParser            │    │
                           │   │  受信JSONの検証/正規化        │    │
                           │   │  ・JSON限定抽出              │    │
                           │   │  ・型/範囲クランプ           │    │
                           │   │  ・既定値補完                │    │
                           │   └─────────────────────────────┘    │
                           │          │ 正規化DTO                  │
                           │          ▼                           │
                           │   ┌─────────────────────────────┐    │
                           │   │  LlmJsonMapper              │    │
                           │   │  DTO → Domain Script に変換 │    │
                           │   └─────────────────────────────┘    │
                           │          │ Script                     │
                           │          ▼                           │
                           │   ┌─────────────────────────────┐    │
                           │   │  PlanFactory                │    │
                           │   │  Script → ExecutionPlan     │    │
                           │   │  （UtterancePlan[] 作成）   │    │
                           │   └─────────────────────────────┘    │
                           │          │ Plan                      │
                           └──────────▼───────────────────────────┘
                                      │ （以降、Planを順次実行）
         ┌───────────────┬───────────────┬───────────────┬───────────────┐
         ▼               ▼               ▼               ▼
     IChatPort        ITtsPort      IEmotionPort     IAnimationPort
       (LLM)           (TTS)          (表情)            (体の動き)
         │               │                │                 │
         │               │                │                 │
         │         AudioSource.Play        │                 │
         │               │                │                 │
         ▼               ▼                ▼                 ▼
 [OpenAI/Claude]   [VOICEVOX/AI TTS]  [VRM表情Adapter]   [AnimatorAdapter]

 （並列：口パク）
 AudioSource → IAudioLevelPort → DriveLipSyncUseCase → IVisemeApplyPort → [VRM 口形状]

## データフローmini
OpenAIChatClientAdapter.cs
  └─ (string json)
      ▼
LlmScriptParser.cs
  └─ (Normalized DTO / fail→error)
      ▼
LlmJsonMapper.cs
  └─ (Domain Script)
      ▼
PlanFactory.cs
  └─ (ExecutionPlan: UtterancePlan[])
      ▼
RunInteractionUseCase.cs
  ├─ ITtsPort (合成/再生)
  ├─ IEmotionPort (表情)
  ├─ IAnimationPort (体の動き)
  └─ [並列] IAudioLevelPort→DriveLipSyncUseCase→IVisemeApplyPort（口形状）
```

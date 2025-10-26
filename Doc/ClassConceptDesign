# 🧩 OpenAvatarKid 各クラスの概念設計（構造的まとめ）

## 🏗️ Domain 層（Unity非依存・純粋モデル）
> **目的:** システム中核データモデルとルールを定義。外部依存なし。

### 📘 Conversation/
- **ConversationScript.cs**  
  - 台本モデル（複数Utteranceを保持）  
  - LLM出力を正規化・安全化して内部モデル化  
  - 不変リスト構造で防御的コピー  

- **Utterance.cs**  
  - 1発話単位（text / faceExpression / bodyExpression / emotionLevel）  
  - LLM出力スキーマ(4フィールド仕様)に準拠  

- **EmotionTag.cs**  
  - 感情タグ列挙 (joy / sad / angry / neutral / thinking...)  
  - VRM BlendShapeProfileと1対1対応  

- **VoiceSpec.cs**  
  - 話者ID・話速・スタイル・感情トーンを保持  
  - PlaySpeechUseCase経由でTTSへ渡される  

- **Constraints.cs**  
  - 発話文字数上限や件数制約  
  - ValidationServiceが参照  

### 🎤 LipSync/
- **Phoneme.cs / Viseme.cs**  
  - 音素と口形状(A/I/U/E/O)の抽象化モデル  

- **LipSyncRule.cs**  
  - RMS/音素 → 口形状変換ルール  
  - Resources/Profiles/LipSyncProfile.asset で調整  

### ⚙️ Shared/
- **ResultCode.cs / AppConstants.cs**  
  - 共通コード・定数（タイムアウト値、音量閾値など）

## 🎯 UseCases 層（アプリケーションロジック）
> **目的:** 「入力→処理→出力」の流れを制御し、アダプタを組み合わせる。

### 🎬 Interactors/
- **HandleUserInputUseCase.cs**  
  - 入力(テキスト/STT結果) → LLM呼び出し → ConversationScript生成  
  - IChatClientAdapter → LlmScriptParser → LlmJsonMapper → ValidationService  

- **GenerateResponseUseCase.cs**  
  - Function Calling後の再応答生成  

- **PlaySpeechUseCase.cs**  
  - ConversationScriptを順送りで音声再生  
  - ITtsClientAdapterを介して音声合成 → SpeechOutputDriver再生  
  - `betweenPauseSec=1.2` のポーズ挿入  

- **UpdateLipSyncUseCase.cs**  
  - RMS/音素 → Viseme重み更新（リアルタイム口パク制御）

### 🧠 Services/
- **EmotionMapper.cs**  
  - EmotionTag → VRM BlendShapeKey / voiceStyle対応表  

- **LipSyncService.cs**  
  - RMS→口形状重み変換・スムージング  

- **ValidationService.cs**  
  - 不正JSONや未知タグ補正・丸め処理  

## 🔄 InterfaceAdapters 層（DTO変換・検証・Presenter）
> **目的:** 外部形式(LLM/TTS/STT)と内部モデルを安全に橋渡し。

### 🤖 LLM/
- **LlmContract.cs**  
  - LLM出力DTO（utterances + timing.betweenPauseSec）  
- **LlmScriptParser.cs**  
  - JSONパース・不正修復・欠損補完（betweenPauseSec=1.2既定）  
- **LlmJsonMapper.cs**  
  - DTO→Domain(ConversationScript/Utterance)変換  

### 🔊 TTS/
- **TtsContract.cs / TtsMapper.cs / TtsValidator.cs**  
  - 各TTSベンダーの独自パラメータを統一VoiceSpecへ変換  

### 🗣 STT/
- **SttContract.cs / SttMapper.cs / SttValidator.cs**  
  - 音声認識結果を共通フォーマット化  

### 💬 Presenters/
- **ChatPresenter.cs**  
  - 会話ログや出力メッセージの整形  
- **AvatarPresenter.cs**  
  - 表情/感情をHUD表示用に変換  

### 📈 Telemetry/
- **InteractionMetrics.cs**  
  - STT→LLM→TTS各段階のレイテンシや成功率を収集  

## 🧩 Infrastructure 層（具体的実装・I/F定義）
> **目的:** 具体的API実装と抽象I/F（Adapterパターン）

### 🧾 Interface/
- **IChatClientAdapter.cs** → LLM呼び出し（OpenAI/Claude）  
- **ITtsClientAdapter.cs** → テキスト→音声変換  
- **ISttClientAdapter.cs** → 音声→テキスト変換  
- **IEmotionAdapter.cs** → 表情制御（VRM BlendShape操作）  
- **IAnimationAdapter.cs** → モーション制御（Animator抽象化）  
- **IAudioInputAdapter.cs** → RMS/音素検出入力  
- **IMemoryRepository.cs** → 会話履歴・設定永続化  

### 🤖 LLM 実装/
- **OpenAIChatClientAdapter.cs**  
  - OpenAI Chat API 呼び出し・JSON準拠強制・リトライ制御  
- **ClaudeChatClientAdapter.cs**  
  - Claude API 実装  

### 🔊 TTS 実装/
- **VoiceVoxTtsClientAdapter.cs**  
  - VOICEVOX音声合成  
- **OpenAiTtsClientAdapter.cs**  
  - OpenAI TTS実装（JSON+PCMストリーム出力、Newtonsoft.Json利用）  

### 🗣 STT 実装/
- **WhisperSttClientAdapter.cs**  
  - Whisper STT実装（逐次/最終イベント）  
- **AzureSttClientAdapter.cs**  
  - Azure Speech to Text 実装  

### 🧍 Avatar/
- **VrmEmotionAdapter.cs** → VRM表情制御  
- **AnimatorAdapter.cs** → アニメーション再生管理  
- **VrmLipSyncAdapter.cs** → 音声出力→音素解析→Viseme変換  

### 💾 Memory/
- **LocalMemoryRepository.cs** → 設定/履歴をローカル保存  

### 🎧 Audio/
- **UnityAudioInputAdapter.cs** → マイク入力→RMS算出  
- **UnityAudioPlaybackAdapter.cs** → AudioSource再生+OnRms発火  

## 🎨 Presentation 層（Unity実装/UI）
> **目的:** ユーザー入力と出力、リアルタイム同期制御。

### 🧭 Orchestration/
- **DialogOrchestrator.cs**  
  - 「聞く→考える→話す」状態制御と割込管理  
- **InteractionStateMachine.cs**  
  - Idle/Listening/Thinking/Speaking状態を厳格管理  

### 🎙 IO/
- **SpeechInputListener.cs** → マイク→STT呼び出し  
- **SpeechOutputDriver.cs** → 音声再生・RMS発火  
- **WakeWordListener.cs** → ウェイクワード検知→状態遷移  

### 🔁 Synchronizers/
- **LipSyncSynchronizer.cs** → RMS/音素→VRM口形状更新  
- **ExpressionSynchronizer.cs** → 感情タグ→表情BlendShape反映  
- **AvatarStateSynchronizer.cs** → モーション/表情同期整合  

### 🔗 Bridges/
- **ChatBridge.cs** → 入力統合・AskAsync/Interrupt提供  
- **ToolBridge.cs** → Function Calling統合  
- **MemoryBridge.cs** → 長期メモリ制御  

### 🪟 UI/
- **ChatPanelUI.cs** → 入力欄＋ログUI  
- **LogPanel.cs** → LLM/STT/TTSログ出力  
- **EmotionHUD.cs** → 現在の感情/モーション表示  

### 🚀 Bootstrap/
- **AvatarChatBootstrap.cs** → 起動時の依存解決・Profile読込・初期配線  

## 📦 Resources / Config / Profiles
- **SystemPrompt.txt** → LLM用システムプロンプト（JSON限定/4フィールド/1.2秒ポーズ）  
- **ExpressionProfile.asset** → EmotionTag→BlendShapeマップ  
- **LipSyncProfile.asset** → RMS→口形状マッピングルール  
- **AnimationProfile.asset** → bodyExpression→AnimatorStateマップ  
- **InteractionConfig.asset** → タイムアウト・音量・再試行設定  

## 🎬 Addressables / Scenes / Editor
- **Animations/** → bodyExpression対応モーションクリップ  
- **Audio/PreGenerated/** → 定型音声キャッシュ  
- **Scenes/Main.unity / Demo.unity** → 実行シーン  
- **Editor/Validator.cs** → Profile検証ツール  
- **Editor/AutoBindTool.cs** → シーン依存自動バインド補助  

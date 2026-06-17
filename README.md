# CardGame — Card Battle

> **YouTube 시연:** _(녹화 후 링크 추가)_

## 문서 유지보수

작업 전 **협업·스택 문서를 반드시 읽는다.**

| 문서 | 용도 |
|------|------|
| `.cursor/rules/collaboration-rules.mdc` | 모드·승인·스코프·스킬 호출 규칙 |
| `.cursor/rules/collaboration-unity.mdc` | Unity 구현·핫패스·Mermaid 등 |
| `.cursor/rules/unity-stack.mdc` | Unity/C# 스택 요약 (`Assets/**` 작업 시) |
| `docs/tech-stack.md` | 스택·C# 9 제약·패키지 정본 |

`Assets/Scripts/CardBattle/` 구조·아키텍처·에디터 절차를 바꿀 때는 **같은 PR/커밋에서 이 README를 함께 갱신**한다. 협업 규칙과 충돌하면 **협업 문서가 우선**한다.

## 환경

- **Unity:** 6000.3.10f1 (Unity 6 LTS)
- **렌더 파이프라인:** Universal Render Pipeline (URP)
- **화면:** Landscape (가로) 고정
- **플랫폼:** Android ARM64 APK

## 구현 기능

### 필수

- [x] 턴제 카드 배틀 (플레이어 6장 / 적 6장, 전장 3 + 대기 3)
- [x] 카드 타입: Normal, Ranged, Musou, Healer (PDF 수식 준수)
- [x] `CardBehaviorAsset` SO 기반 카드 행동 (타입별 전용 SO + 모듈 수집)
- [x] 턴 시작 힐러 광역 자가 회복 (+1, 자신 제외)
- [x] 공격 / 반격 / 사망 / 대기 카드 자동 전진
- [x] 승패 판정 (한 진영 6장 전멸)
- [x] State 패턴 FSM (Init → Player → Battle → Enemy → GameOver)
- [x] 클릭·드래그 타겟 선택 (`CardBattleTargetingPolicy`, `IInputProvider`)
- [x] Utility AI 적 타겟 선정
- [x] UI: 턴 배너, 대기 덱 수, HP바, 결과 팝업, Restart

### 가산점

- [x] URP Post Processing (Bloom 펄스 — 힐/공격)
- [x] DOTween + 코루틴 이중 연출 (HP바, 대시, Shake, 카메라 셰이크)
- [x] Presentation cue 파이프라인 (`PresentationPlayer`, `PresentationSequenceBuilder`)
- [x] 드래그 타겟 라인 프리뷰 (`DragTargetingPresenter`)
- [x] `CardBattle.asmdef` / `CardBattle.Editor.asmdef` 컴파일 격리
- [x] `BattleBridge` / `ICardDataLoader` / `BattleAudioAdapter` 이식 브릿지
- [x] Input System 추상화 (`IInputProvider`)
- [x] UniTask 비동기 턴/연출
- [x] 에디터 셋업 메뉴 (`Setup CardBattle Scene`, Behavior/CardData 일괄 생성)
- [ ] 카드 일러스트 에셋 (`CardDataAsset.illustration` — **에디터 작업**)

## 폴더 구조

```
Assets/Scripts/CardBattle/
├── CardBattle.asmdef
├── AI/EnemyAIController.cs
├── Bridge/BattleBridge.cs, ICardDataLoader.cs, BattleAudioAdapter.cs, ...
├── Cards/CardModel, CardView, CardDataAsset, CardBehaviorAsset, *BehaviorAsset, IBattleEffectModule, ...
├── Core/GameManager, BattleField, CardBattleTargetingPolicy, ...
├── Input/DragTargetingPresenter, DragTargetingContracts
├── Presentation/PresentationPlayer, BattlePresentationModules, ...
├── States/Init, PlayerTurn, EnemyTurn, Battle, GameOver
├── UI/UIManager.cs
└── Editor/CardBattle.Editor.asmdef, CardBattleSceneSetup, CardBattleBehaviorSetup

Assets/Resources/CardBattle/
├── Behaviors/   # Behavior_Normal, Ranged, Musou, Healer
└── Cards/       # Player_*, Enemy_* CardDataAsset
```

## 아키텍처

```
[BattleBridge] ──InitializeBattle──▶ [GameManager FSM]
        │                                    │
        ▼                                    ▼
 [ICardDataLoader]              Init → PlayerTurn → Battle
                                        ↓            ↓
                                   EnemyTurn ←───────┘
                                        ↓
                                   GameOver → UIManager

[CardDataAsset] ──behavior──▶ [CardBehaviorAsset]
        │                              │
        ▼                              ├─ CollectAttackModules → BattleResolver
[CardModel]                            └─ CollectPresentationModules → PresentationSequence
        │
        ▼
[CardView] ◀── Bind ── SyncAllViews
        │
        ├─ IInputProvider (클릭/드래그)
        └─ DragTargetingPresenter (라인 프리뷰)
```

**네임스페이스:** `CardGame.CardBattle.*`

**카드 UI:** 전장 슬롯당 `CardView` 1개(플레이어 3 + 적 3). 카드 종류별 프리팹 없이 `CardDataAsset`(이름·일러스트·HP)로 `Bind()` 갱신.

## 에디터 수동 작업

### A. 씬 셋업

**권장:** `CardGame → CardBattle → Setup CardBattle Scene`  
→ `Assets/Scenes/CardBattleScene.unity` 저장, Canvas·카드 슬롯 6·GameManager·UIManager·Build Settings 자동 구성.

**수동:** 아래 항목을 직접 배치할 때.

1. **씬:** `Assets/Scenes/CardBattleScene.unity`
2. **루트 오브젝트**
   - `BattleSystems` — `GameManager`, `BattleBridge`, `BattleSceneBootstrap`, `BattleAudioAdapter` 부착
   - Canvas — `UIManager` 부착 (Screen Space Overlay, Landscape 레이아웃)
   - `EventSystem` (Input System UI Module)
3. **카드 슬롯 (×6)** — Player 3 / Enemy 3 자식 RectTransform + `CardView` + Image + Slider + TMP
4. **GameManager Inspector** — `playerCardViews[3]`, `enemyCardViews[3]`, `uiManager`, `dragTargetingPresenter` 연결
5. **DragTargetingPresenter** — Canvas 자식에 배치, 라인 Image·`rootCanvas`·`uiCamera` 연결 (SceneSetup 메뉴에는 미포함)
6. **UIManager** — 턴 배너 TMP, 대기 덱 TMP, Win/Lose 패널, Restart 버튼, Global Volume 연결

### B. 데이터 에셋

1. `CardGame → CardBattle → Create Default Behavior Assets` (최초 1회)
2. `CardGame → CardBattle → Link Card Data To Behaviors` (CardData ↔ Behavior 연결)
3. **BattleBridge** — `defaultPlayerDeck` / `defaultEnemyDeck` 각 6장 할당  
   ※ 미할당·무효 덱이면 `RuntimeDeckFactory`가 런타임 테스트 덱 생성

### C. 빌드

1. **Build Settings** — CardBattleScene 추가, Android ARM64 APK 빌드
2. **Bundle ID** — `Player Settings → Android → Package Name` 변경

## AI 도구 활용

본 프로젝트는 ChatGPT/Cursor 등 AI 어시스턴트를 활용하여 설계 검증, Utility AI 수식, asmdef 격리 및 브릿지 설계에 사용했습니다.

## 라이선스

MIT License — Copyright (c) 2026 JongWan Shin. `LICENSE` 파일 참조.

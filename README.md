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
| `docs/card-battle-architecture.md` | CardBattle 레이어·파이프라인·리팩터 현황 |

`Assets/Scripts/CardBattle/` 구조·아키텍처·에디터 절차를 바꿀 때는 **같은 PR/커밋에서 이 README와 `docs/card-battle-architecture.md`를 함께 갱신**한다. 협업 규칙과 충돌하면 **협업 문서가 우선**한다.

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
- [x] 카드 롱프레스(1초) 상세 보기 (`CardDetailOverlayPresenter`, `CardDetailContext`)
- [x] Utility AI 적 타겟 선정
- [x] UI: 턴 배너, 대기 덱 수, HP바, 결과 팝업, Restart
- [x] 3D 보드 (`CardEntity` + `CardBoardPresenter`, PhysicsRaycaster 타겟팅)

### 가산점

- [x] URP Post Processing (Bloom 펄스 — 힐/공격)
- [x] DOTween + UniTask 이중 연출 (HP바, 대시, Shake, 카메라 셰이크)
- [x] Presentation cue 파이프라인 (`PresentationPlayer`, `PresentationSequenceBuilder`, `CardPresentationService`)
- [x] 드래그 타겟 라인 프리뷰 (`DragTargetingPresenter`)
- [x] `CardBattle.asmdef` / `CardBattle.Editor.asmdef` 컴파일 격리
- [x] `BattleBridge` / `ICardDataLoader` / `BattleAudioAdapter` 이식 브릿지
- [x] Input System 추상화 (`IInputProvider`, `UnityInputProvider`)
- [x] UniTask 비동기 턴/연출
- [x] 에디터 셋업 메뉴 (씬·보드·Behavior/CardData 일괄 생성)
- [x] 카드 일러스트 기본 파이프라인 (`Assign Default Card Illustrations` — 개별 아트 교체는 선택)

## 폴더 구조 (요약)

```
Assets/Scripts/CardBattle/
├── CardBattle.asmdef
├── AI/EnemyAIController.cs
├── Bridge/BattleBridge, ICardDataLoader, BattleAudioAdapter, ...
├── Cards/CardModel, CardEntity, CardDataAsset, CardBehaviorAsset, *BehaviorAsset, motion, ...
├── Core/GameManager, BattleField, BattleOrchestrator, BattleLayoutConfig, ...
├── Input/DragTargetingPresenter, DragTargetingContracts
├── Presentation/CardBoardPresenter, PresentationPlayer, CardPresentationService, ...
├── States/Init, PlayerTurn, EnemyTurn, Battle, GameOver
├── UI/UIManager.cs
└── Editor/CardBattleSceneSetup, CardBattleBoardSetup, CardBattleBehaviorSetup

Assets/Prefabs/CardBattle/
└── CardEntity.prefab

Assets/Resources/CardBattle/
├── BattleLayout_Default.asset
├── Behaviors/   # Behavior_Normal, Ranged, Musou, Healer
├── Cards/       # Player_*, Enemy_* CardDataAsset
├── Art/         # 카드 일러스트·뒷면 스프라이트
└── Vfx/         # SharedHitVfx 등
```

상세 디렉터리·레이어 경계는 [`docs/card-battle-architecture.md`](docs/card-battle-architecture.md)를 따른다.

## 아키텍처 (요약)

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
[CardEntity] ◀── CardBoardPresenter (스폰·배치·sync)
        │
        ├─ UnityInputProvider (클릭/드래그, CardInstanceId)
        └─ DragTargetingPresenter (UI 드래그 라인)
```

**네임스페이스:** `CardGame.CardBattle.*`

**카드 표현:** 전장·대기 슬롯마다 `CardEntity` 프리팹 인스턴스. `CardDataAsset`(이름·일러스트·HP)과 `CardViewState`로 `Bind()` 갱신. 배치·연출은 `CardBoardPresenter` / `CardEntity.ApplyPlacement` / motion 드라이버가 담당.

입력·공격·배치 파이프라인, 모델–뷰 분리, 변경 체크리스트는 **아키텍처 문서**에 정리되어 있다.

## 에디터 셋업

### 권장 (일괄)

`CardGame → CardBattle → Setup CardBattle Scene`

→ `Assets/Scenes/CardBattleScene.unity` 저장. Canvas·UIManager·DragTargetingPresenter·BattleBoard·CardBoardPresenter·GameManager·BattleBridge·Build Settings를 자동 구성한다.

### 데이터·에셋 (최초 1회 또는 갱신 시)

| 메뉴 | 용도 |
|------|------|
| `Create Default Behavior Assets` | 타입별 `CardBehaviorAsset` 4종 + 기본 VFX |
| `Link Card Data To Behaviors` | `CardDataAsset` ↔ Behavior 연결 |
| `Assign Default Card Illustrations` | 미할당 카드에 기본 일러스트 |
| `Assign Default Detail Text To Behaviors` | Behavior SO **상세 보기** 타입·설명 기본값 (빈 필드만) |
| `Assign Default Presentation Vfx` | Behavior SO 명중·피격 VFX 재할당 |
| `Wire Default Decks To Scene` | 씬 `BattleBridge`에 Resources 덱 연결 |

### 보드·프리팹 (씬 수동 편집 시)

| 메뉴 | 용도 |
|------|------|
| `Create Card Entity Prefab` | `Assets/Prefabs/CardBattle/CardEntity.prefab` 재생성 |
| `Create Default Battle Layout` | `BattleLayout_Default.asset` |
| `Ensure Board Zone Anchors` | `BattleBoard` 앵커·Presenter 재연결 (슬롯 pose는 유지) |
| `Ensure Card Detail Overlay` | Canvas `CardDetailOverlay` 생성·`GameManager.cardDetailOverlay` 연결 |

### 수동 점검 (Setup 메뉴 이후)

1. **GameManager** — `uiManager`, `cardBoardPresenter`, `dragTargetingPresenter`, **`cardDetailOverlay`** 연결  
   ※ `Ensure Card Detail Overlay` 메뉴로 오버레이·와이어 일괄 처리
2. **BattleBridge** — `defaultPlayerDeck` / `defaultEnemyDeck` 각 6장 (비어 있으면 `DefaultDeckCatalog` + `RuntimeDeckFactory` 폴백)
3. **Build Settings** — CardBattleScene 추가, Android ARM64 APK 빌드
4. **Bundle ID** — `Player Settings → Android → Package Name` 변경

## AI 도구 활용

본 프로젝트는 ChatGPT/Cursor 등 AI 어시스턴트를 활용하여 설계 검증, Utility AI 수식, asmdef 격리 및 브릿지 설계에 사용했습니다.

## 라이선스

MIT License — Copyright (c) 2026 JongWan Shin. `LICENSE` 파일 참조.

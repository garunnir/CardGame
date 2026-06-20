# CardBattle 아키텍처

CardBattle 모듈의 런타임 구조·데이터 흐름·레이어 경계를 정리한 문서입니다.

- 프로젝트 개요·기능 체크리스트·빌드: [README.md](../README.md)
- 스택 공통 사항: [tech-stack.md](./tech-stack.md)

## 어셈블리

| asmdef | 역할 |
|--------|------|
| `CardGame.CardBattle` | 런타임 전체 (논리 폴더로 레이어 분리) |
| `CardGame.CardBattle.Editor` | 씬/보드/behavior 셋업 메뉴 |

물리 asmdef 분리(Core / Presentation / View)는 **미적용·보류**. 모듈 단위 asmdef(`CardGame.CardBattle`, `CardGame.CardBattle.Editor`)는 이미 존재하며, 레이어 쪼개기는 규모 대비 ROI가 낮아 선택 사항.

---

## 논리 레이어

```
┌─────────────────────────────────────────────────────────┐
│  Bridge / UI / States (FSM)                             │
│  GameManager · PlayerTurnState · BattleBridge           │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│  Core (도메인)                                           │
│  BattleField · CardModel · BattleResolver · Orchestrator │
└──────────────────────────┬───────────────────────────────┘
                           │
         ┌─────────────────┴─────────────────┐
         ▼                                   ▼
┌────────────────────┐            ┌──────────────────────────┐
│  Cards (데이터·    │            │  Presentation (연출·보드) │
│  행동 SO·뷰 계약)  │◄───────────│  CardBoardPresenter      │
│  CardViewState     │            │  PresentationPlayer      │
│  CardEntity (3D)   │            │  PresentationModule*   │
└────────────────────┘            └──────────────────────────┘
```

### 책임 요약

| 레이어 | SSOT / 핵심 타입 |
|--------|------------------|
| **Core** | `BattleField` 배치·승패, `CardModel` HP/행동, `AttackOutcome`, FSM 허브 |
| **Cards** | `CardDataAsset` / `CardBehaviorAsset`, `CardViewState`, `ICardBattleView`, `ICardInputHost` |
| **Presentation** | 보드 스폰·sync, 연출 cue 시퀀스, `ICardViewRegistry` |
| **Input** | `UnityInputProvider` (`CardInstanceId` 이벤트), `DragTargetingPresenter` (UI 라인) |

---

## 모델–뷰 분리

| 관심사 | 도메인 | 뷰 |
|--------|--------|-----|
| 식별 | `CardInstanceId` | `ICardBattleView.InstanceId` |
| 표시 데이터 | `CardModel` (HP, 이름, 행동) | `CardViewState` DTO |
| 전장 앞면·타겟 | `BattleField.IsTargetableOnBattlefield` | `CardBoardPhase` (표시만) + `ApplyInputTargeting` (presenter가 field 규칙 반영) |
| HP 연출 | `BattleCommandExecutor` 선적용 | `PresentationPlayer` / `SetHpDisplay` |
| 보드 배치 | `BattleField` 슬롯·reserve | `CardEntity.ApplyPlacement` (reparent·보간) |

`CardEntity`는 `CardModel` 참조를 **보유하지 않음**. `SyncFromModel`은 `CardViewState.FromModel`만 바인딩합니다.

---

## 공격 1회 파이프라인

```
BattleActionRequest
  → BattleResolver.PlanOutcome
  → BattleCommandExecutor.ApplyAttack     (도메인 HP 반영)
  → AttackPresentationSnapshot
  → PresentationSequenceBuilder           (PresentationModuleFactory)
  → PresentationPlayer.PlayAttackAsync    (애니·SFX만)
  → BattleField.ProcessDeathsAndRefill
  → CardBoardPresenter.SyncBoardWithinLockAsync
```

- **Plan → Apply → Snapshot → 연출** 순서. 연출 cue는 HP를 다시 쓰지 않음.
- tail delay: `BattleLayoutConfig.attackPresentationTailDelay`

---

## 입력 파이프라인

**스택:** Player Settings = Input System Package (New). `InputSystemUIInputModule` + PhysicsRaycaster. **`UnityEngine.Input` 사용 금지** — 포인터는 `IPointer*` / `PointerEventData` (롱프레스 이동 취소 포함). [`tech-stack.md`](./tech-stack.md) Input 절.

```
CardEntity (ICardInputHost, InstanceId)
  → UnityInputProvider (CardInstanceId 이벤트)
  → PlayerTurnState (selectedAttackerId 보관)
  → Context.TryGetModel / FindView(id)
  → CardBattleTargetingPolicy + BattleField
  → BattleActionRequest
```

- FSM·입력 경계: ID만 전달, `CardModel`은 `ICardViewRegistry.TryGetModel`로 해석.
- **롱프레스 상세:** `CardEntity` 1초 홀드 → `CardLongPressed` → `GameManager` → `CardDetailContext` → `CardDetailOverlayPresenter` (씬: `GameManager.cardDetailOverlay`, 메뉴 `Ensure Card Detail Overlay`)
- **상세 문구 SSOT:** `CardDataAsset` 덮어쓰기 → `CardBehaviorAsset` 상세 보기 → 코드 폴백 (`CardDetailContextFallback`)
- 드래그 호버 비주얼: `DragTargetingPresenter` (스크린 라인)

---

## 보드·배치

- **위치 SSOT**: 씬 `BattleBoardZoneLayout` (`battlefieldCenter` + `cardSpacing` 직선 정렬)
- **타이밍 SSOT**: `BattleLayoutConfig` (deploy/flip/dash/hp/death/hover/tail) — `CardEntity.ApplyLayout`로 주입
- **엔티티 인덱스**: `CardBoardEntityRegistry` (`CardInstanceId` → `Model` + `Entity`)

### 배치 파이프라인 (Presenter → View)

```
CardBoardPlacement.ResolvePlacement  →  AnchorPlacement (parent, local pose)
  → CardBoardDeployer.PlaceBattlefieldCardAsync / PlaceReserveCardAsync
  → CardEntity.ApplyPlacement(parent, pose, phase, animate)
```

- **Presenter**: 목표 앵커·`CardBoardPhase`·`animate`만 전달. `pendingBattlefieldDeploy` 마킹/해제는 `CardBoardDeployer`만.
- **View**: `ApplyPlacement`가 reparent(`worldPositionStays: true`)·보간(로컬/월드 이동·플립) SSOT.
- **리필(deck→전장)**: reserve 출발 deploy는 **월드 좌표** 이동 + 플립.
- **팀 sync**: 플레이어 → 적 **순차** 실행. 팀별 모델 버퍼는 `CardBoardTeamOps` **로컬** (공유 buffer 금지).

전투 연출(`PlayHpChange`, `PlayAttackDash` 등)은 `ICardBattleView` 경로이며, 배치와 분리됩니다.

### 배치 연출 vs 타겟팅

| 상태 | 도메인 | 뷰 |
|------|--------|-----|
| 전장 슬롯 점유 | `BattleField.IsOnBattlefield` | — |
| 배치 연출 중 | `pendingBattlefieldDeploy` → 타겟 불가 | deploy/flip 애니 |
| 타겟·공격 가능 | `IsTargetableOnBattlefield` | `CardBoardPhase.BattlefieldFaceUp` |

리필 시 도메인은 즉시 슬롯에 올리고, **animate deploy** 동안 `MarkPendingBattlefieldDeploy`로 클릭 타겟을 막습니다.

---

## 주요 설정 자산

| 자산 | 용도 |
|------|------|
| `BattleLayoutConfig` | 프리팹, 뒷면 스프라이트, 연출·입력 파라미터 |
| `CardBehaviorAsset` (+ presentation 필드) | 전투 모듈·연출 파라미터 (SO) |
| `DefaultDeckCatalog` | 덱 SSOT (Bridge Inspector 빈 값 시 폴백) |

연출 모듈 인스턴스화: `PresentationModuleFactory` (Cards → Presentation 역전).

---

## 리팩터링 현황 (2025-06 감사 · 2026-06 갱신)

### 완료된 항목

- `AttackOutcome` / `DeathPresentationPlanner` / 도메인 선적용
- `CardViewState` / `ICardViewRegistry` / `CardInstanceId` 연출 경로
- `BattleOrchestrator` + `ICardBoardSession` 추출
- `PresentationModuleFactory` / `CardPresentationService` → `Presentation/`
- 입력·FSM `CardInstanceId` 경로, `boundModel` 제거
- `BattleField.IsTargetableOnBattlefield` + `pendingBattlefieldDeploy`
- **타겟팅 SSOT**: `CardTargetingRules(BattleField)` → `CardEntity.ApplyInputTargeting` (presenter 갱신)
- `MeleeAttackPresentationModule` — cue 타이밍만 (SFX/VFX는 `CardPresentationService`)
- `CardBoardPresenter` → `EntityRegistry` / `Placement` / `Deployer` / `TeamOps` / `InputTargeting` 분할 (~230줄 오케스트레이터)
- `IBattleContext` — FSM이 `GameManager` concrete 대신 인터페이스 사용
- **보드 배치 View SSOT**: `CardEntity.ApplyPlacement` → `CardBoardMotion` (`ICardBoardMotion`)
- **전투 연출 View**: `CardCombatMotion` (`ICardBattleView` 메서드 위임)
- `ICardMotionHost` — motion 드라이버용 내부 호스트 계약
- 덱 리필 연출: world deploy, 팀 순차 sync, 로컬 model buffer
- 레거시 배치 API 제거 (`PlayDeploy*`, `SnapReserve*`, `EnsureAnchorParent`)
- `CardEntity.Motion` Coroutine → UniTask (HP/dash/shake 폴백)
- **P1** `CardEntity` motion 분리: `CardBoardMotion` / `CardCombatMotion` + `ICardBoardMotion`
- `UIManager` 턴 배너 fade Coroutine → UniTask
- 초기 스폰: 전 카드 덱 스택 배치 후 전장 3장 순차 deploy
- 카드 면: `CardFace_Default` 불투명·`Cull Back` (양면 스프라이트 머티리얼 제거)

### 남은 항목 (선택)

| 우선순위 | 항목 | 이유 |
|----------|------|------|
| **P3** | `CardBoardDeployer` → `TeamOps` 합치기 | ~60줄, 선택적 정리 |

### 권장 다음 라운드 순서

1. 게임 기능·연출 (덱 뽑기 연출 등 UX)
2. 소규모 정리 (`CardBoardDeployer` 합치기) — 필요 시만

---

## 디렉터리 맵 (런타임)

```
Assets/Scripts/CardBattle/
├── Bridge/          # 씬 진입·오디오
├── Cards/           # Model, SO, CardEntity, targeting rules
│   ├── CardBoardMotion.cs         # 보드 배치 motion
│   ├── CardCombatMotion.cs        # 전투 연출 motion
│   ├── ICardBoardMotion.cs
│   └── ICardMotionHost.cs (internal)
├── Core/            # Field, FSM hub, resolver, orchestrator, layout SO
├── Input/           # Drag line, contracts
├── Presentation/    # Board, player, cues, modules
│   ├── CardBoardPresenter.cs      # lock·오케스트레이션
│   ├── CardBoardEntityRegistry.cs
│   ├── CardBoardTeamOps.cs        # spawn/sync
│   ├── CardBoardDeployer.cs
│   ├── CardBoardPlacement.cs
│   └── CardBoardInputTargeting.cs
├── States/          # FSM states
└── UI/              # UIManager
```

---

## 에디터 셋업

일괄 진입: `CardGame → CardBattle → Setup CardBattle Scene` (README 에디터 절차 참조).

| 메뉴 | 스크립트 |
|------|----------|
| Setup CardBattle Scene | `CardBattleSceneSetup` |
| Ensure Card Detail Overlay | `CardBattleSceneSetup` — 기존 씬에 롱프레스 상세 UI 추가 |
| Create Card Entity Prefab / Create Default Battle Layout / Ensure Board Zone Anchors / Wire Default Decks To Scene | `CardBattleBoardSetup` |
| Create Default Behavior Assets / Link Card Data To Behaviors / Assign Default Presentation Vfx / Assign Default Card Illustrations | `CardBattleBehaviorSetup` |

`Ensure Board Zone Anchors` — 기존 `BattleBoard` 앵커·Presenter 재연결 (슬롯 pose 덮어쓰지 않음).

---

## 변경 시 체크리스트

- [ ] 도메인 HP 변경이 `BattleCommandExecutor` 외 경로에 없는가
- [ ] 연출이 `CardModel` 직접 참조 대신 `CardInstanceId` / snapshot 사용하는가
- [ ] 새 타겟 규칙이 `BattleField.IsTargetableOnBattlefield`와 일치하는가
- [ ] 배치 연출 추가 시 `pendingBattlefieldDeploy` 마킹/해제하는가
- [ ] 보드 배치 보간이 Presenter가 아닌 `CardEntity.ApplyPlacement`에 있는가
- [ ] 팀 sync가 공유 buffer 없이 순차·로컬 buffer로 동작하는가
- [ ] 연출 타이밍이 `BattleLayoutConfig`에 있는가 (GameManager SerializeField 금지)

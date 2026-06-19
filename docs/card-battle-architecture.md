# CardBattle 아키텍처

CardBattle 모듈의 런타임 구조·데이터 흐름·레이어 경계를 정리한 문서입니다.  
스택 공통 사항은 [tech-stack.md](./tech-stack.md)를 따릅니다.

## 어셈블리

| asmdef | 역할 |
|--------|------|
| `CardGame.CardBattle` | 런타임 전체 (논리 폴더로 레이어 분리) |
| `CardGame.CardBattle.Editor` | 씬/보드/behavior 셋업 메뉴 |

물리 asmdef 분리(Domain / Presentation / View)는 **미적용**. 단일 asmdef로 컴파일 순환은 없으나, 논리적 Core → Presentation → Cards → Core 의존이 존재합니다.

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

```
CardEntity (ICardInputHost, InstanceId)
  → UnityInputProvider (CardInstanceId 이벤트)
  → PlayerTurnState (selectedAttackerId 보관)
  → Context.TryGetModel / FindView(id)
  → CardBattleTargetingPolicy + BattleField
  → BattleActionRequest
```

- FSM·입력 경계: ID만 전달, `CardModel`은 `ICardViewRegistry.TryGetModel`로 해석.
- 드래그 호버 비주얼: `DragTargetingPresenter` (스크린 라인)

---

## 보드·배치

- **위치 SSOT**: 씬 `BattleBoardZoneLayout` (`battlefieldCenter` + `cardSpacing` 직선 정렬)
- **타이밍 SSOT**: `BattleLayoutConfig` (deploy/flip/dash/hp/death/hover/tail)
- **엔티티 인덱스**: `CardBoardPresenter.boardSlots` (`CardInstanceId` → `Model` + `Entity`)

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

## 리팩터링 현황 (2025-06 감사)

### 완료된 항목

- `AttackOutcome` / `DeathPresentationPlanner` / 도메인 선적용
- `CardViewState` / `ICardViewRegistry` / `CardInstanceId` 연출 경로
- `BattleOrchestrator` + `ICardBoardSession` 추출
- `PresentationModuleFactory` / `CardPresentationService` → `Presentation/`
- 입력·FSM `CardInstanceId` 경로, `boundModel` 제거
- `BattleField.IsTargetableOnBattlefield` + `pendingBattlefieldDeploy`
- **타겟팅 SSOT**: `CardTargetingRules(BattleField)` → `CardEntity.ApplyInputTargeting` (presenter 갱신)
- `MeleeAttackPresentationModule` — cue 타이밍만 (SFX/VFX는 `CardPresentationService`)

### 추가 라운드 필요: **일부 (P1)**

| 우선순위 | 항목 | 이유 |
|----------|------|------|
| **P1** | `CardEntity` (~800줄 partial) | motion/input/뷰 단일 MonoBehaviour |
| **P1** | `CardBoardPresenter` (~650줄) | spawn/sync/lock God class |
| **P1** | `GameManager` composition root | FSM·보드·연출·UI 허브 비대 |
| **P2** | asmdef 레이어 분리 | 논리 경계 컴파일 강제 (승인 후) |
| **P2** | `CardEntity.Motion` Coroutine → UniTask | tech-stack 정책 정합 |

### 권장 다음 라운드 순서

1. `CardBoardPresenter` spawn/sync 책임 분할 (`CardBoardSpawner` / `CardBoardSync`)
2. `CardEntity` motion → 별 컴포넌트 또는 `ICardMotionView`
3. `IBattleContext`로 States ↔ `GameManager` 결합 완화

---

## 디렉터리 맵 (런타임)

```
Assets/Scripts/CardBattle/
├── Bridge/          # 씬 진입·오디오
├── Cards/           # Model, SO, CardEntity, targeting rules
├── Core/            # Field, FSM hub, resolver, orchestrator, layout SO
├── Input/           # Drag line, contracts
├── Presentation/    # Board, player, cues, modules
├── States/          # FSM states
└── UI/              # UIManager
```

---

## 에디터 셋업

- `CardGame → CardBattle → Ensure Board Zone Anchors` — 구 `Slot_*` 마이그레이션
- `CardBattleSceneSetup` / `CardBattleBoardSetup` — 씬·보드 일괄 구성

---

## 변경 시 체크리스트

- [ ] 도메인 HP 변경이 `BattleCommandExecutor` 외 경로에 없는가
- [ ] 연출이 `CardModel` 직접 참조 대신 `CardInstanceId` / snapshot 사용하는가
- [ ] 새 타겟 규칙이 `BattleField.IsTargetableOnBattlefield`와 일치하는가
- [ ] 배치 연출 추가 시 `pendingBattlefieldDeploy` 마킹/해제하는가
- [ ] 연출 타이밍이 `BattleLayoutConfig`에 있는가 (GameManager SerializeField 금지)

# CardGame — Card Battle

> **YouTube 시연:** _(녹화 후 링크 추가)_

턴제 3D 카드 배틀 프로토타입. 플레이어·적 각 6장(전장 3 + 대기 3), 영웅 패널, ScriptableObject 기반 행동·연출 파이프라인.

---

## 사용한 Unity 버전

| 항목 | 버전 |
|------|------|
| **Unity Editor** | **6000.3.10f1** (Unity 6 LTS) |
| **렌더 파이프라인** | Universal Render Pipeline (URP) 17.x |
| **C#** | 9.0 (`docs/tech-stack.md` — C# 10+ 문법 금지) |
| **화면** | Landscape (가로) 고정 |
| **빌드 타깃** | Android ARM64 APK |

---

## 구현한 기능 목록

### 필수 (과제·코어 게임플레이)

- [x] 턴제 카드 배틀 — 플레이어 6장 / 적 6장, 전장 3 + 대기 3
- [x] 카드 타입 4종 — Normal, Ranged, Musou, Healer (`CardBehaviorAsset` SO + 모듈 수집)
- [x] PDF 수식 기반 데미지·반격·무쌍 2타 (`BehaviorDamageMath`, `BattleResolver`)
- [x] 턴 시작 힐러 광역 자가 회복 (+1, 자신 제외)
- [x] 공격 / 반격 / 사망 / 대기 카드 자동 전진·리필
- [x] 승패 판정 (한 진영 6장 전멸)
- [x] State 패턴 FSM — Init → Player → Battle → Enemy → GameOver
- [x] 클릭·드래그 타겟 선택 (`CardBattleTargetingPolicy`, `IInputProvider`)
- [x] Utility AI 적 타겟 선정 (`EnemyAIController`)
- [x] UI — 턴 배너, 대기 덱 수, HP바, 결과 팝업, Restart
- [x] 3D 보드 — `CardEntity` + `CardBoardPresenter`, PhysicsRaycaster 타겟팅

### 가산·확장 기능

- [x] **영웅 패널** — HP·Shield·MP, 카드 공격 후 영웅 타격(`HeroStrikeController`), 보호막 버프
- [x] **카드 → 영웅 공격** — CardVsHero 연출 모듈 (근접·원거리)
- [x] **카드 롱프레스(1초) 상세 보기** — `CardDetailOverlayPresenter`, `CardDetailContext`
- [x] **드래그 타겟 라인 프리뷰** — `DragTargetingPresenter`
- [x] **URP Post Processing** — Bloom 펄스 (힐/공격)
- [x] **DOTween + UniTask 연출** — HP바, 대시, Shake, 카메라 셰이크, 투사체 비행
- [x] **Presentation cue 파이프라인** — 모듈 수집 → cue 시퀀스 → Command handler 실행
- [x] **데미지·힐 floating text** — TMP 3D, 오브젝트 풀, HpLabel 기준 스폰
- [x] **턴 시작 heal 연출** — 투사체 병렬 비행, impact, HP tween
- [x] `CardBattle.asmdef` / `CardBattle.Editor.asmdef` 컴파일 격리
- [x] `BattleBridge` / `ICardDataLoader` / `BattleAudioAdapter` 브릿지
- [x] Input System 전용 (`UnityInputProvider`, `InputSystemUIInputModule`)
- [x] 에디터 셋업 메뉴 — 씬·보드·Behavior/CardData/Hero 일괄 생성

---

## 주요 코드 구조 설명

### 논리 레이어

```
Bridge / UI / States (FSM)
        │
        ▼
Core (도메인) — BattleField, CardModel, BattleResolver, HeroArenaField
        │
   ┌────┴────┐
   ▼         ▼
Cards       Presentation
(SO·뷰)    (보드·연출 cue)
```

| 레이어 | 경로 | 역할 |
|--------|------|------|
| **Core** | `Core/` | 배치·승패·공격 해석·명령 적용·영웅 타격 |
| **Cards** | `Cards/` | `CardDataAsset`, `CardBehaviorAsset`, `CardEntity`, motion |
| **Presentation** | `Presentation/` | 보드 sync, cue 빌드·재생, SFX/VFX |
| **States** | `States/` | FSM 상태 (PlayerTurn, Battle, …) |
| **Input** | `Input/` | 드래그 라인, 상세 오버레이 |
| **AI** | `AI/` | 적 Utility AI |
| **Bridge** | `Bridge/` | 씬 진입, 오디오, Resources 로더 |
| **Editor** | `Editor/` | 씬·보드·SO 일괄 셋업 메뉴 |

**네임스페이스:** `CardGame.CardBattle.*`

### 공격 1회 파이프라인

```
BattleActionRequest
  → BattleResolver.PlanOutcome
  → BattleCommandExecutor.ApplyAttack      (도메인 HP 선적용)
  → AttackPresentationSnapshot
  → PresentationSequenceBuilder            (IPresentationModule → cues)
  → PresentationPlayer                     (PresentationCueDispatcher → handlers)
  → BattleField.ProcessDeathsAndRefill
  → CardBoardPresenter.SyncBoardWithinLockAsync
```

- 연출은 **snapshot·cue만** 사용 — HP를 연출 중 다시 쓰지 않음.
- SFX/VFX 매핑: `BehaviorPresentationClipResolver` / `HeroPresentationClipResolver`
- 연출 재생: `CardPresentationService` (`PlayClipAt`)

### Presentation cue 구조 (2026-06 리팩터)

```
PresentationModuleFactory / TurnStartPresentationPlanner
        ↓  CollectCues
PresentationSequence (PresentationCue[])
        ↓
PresentationPlayer
        ↓
PresentationCueDispatcher  ──▶  IPresentationCueHandler (kind별)
        ↓
CardPresentationService / PresentationCueMotionBridge / BattleStatFloatingTextPresenter
```

주요 타입:

| 타입 | 설명 |
|------|------|
| `PresentationCue` | 연출 단위 struct (kind, subjectId, hpFrom/To, …) |
| `PresentationCueContext` | battle / turn-start 공통 실행 컨텍스트 |
| `PresentationCueHandlers` | Wait, FlyProjectile, HitShake, HpBarTween 등 handler |
| `BattlePresentationModules` | Melee, Ranged, Counter, HeroStrike 등 cue 수집 |
| `PresentationStatFeedback` | damage/heal floating text cue helper |

### 모델–뷰 분리

- `CardEntity`는 `CardModel` 참조를 **보유하지 않음** — `CardViewState` DTO로 `Bind()`.
- 타겟·배치 규칙 SSOT: `BattleField.IsTargetableOnBattlefield`, `pendingBattlefieldDeploy`.
- 보드 배치 SSOT: `CardEntity.ApplyPlacement` (`CardBoardMotion`).

### 폴더 구조 (요약)

```
Assets/Scripts/CardBattle/
├── AI/
├── Bridge/
├── Cards/          CardModel, CardEntity, *BehaviorAsset, motion
├── Core/           GameManager, BattleField, BattleResolver, HeroStrikeController
├── Input/
├── Presentation/   CardBoardPresenter, PresentationPlayer, cue handlers, …
├── States/
├── UI/
└── Editor/

Assets/Resources/CardBattle/
├── Behaviors/      Behavior_Normal, Ranged, Musou, Healer
├── Cards/          Player_*, Enemy_* CardDataAsset
├── Heroes/         HeroData_Player, HeroData_Enemy
├── HeroBehaviors/  NormalAttack, Shield
├── Presentation/   StatFloatingText, ProjectilePresentation
├── Art/            카드·영웅 일러스트
└── Vfx/            SharedHitVfx 등
```

상세 아키텍처·체크리스트: [`docs/card-battle-architecture.md`](docs/card-battle-architecture.md)

---

## AI 도구 · 외부 에셋 · AI 생성 리소스

### AI 도구 (개발 보조)

| 도구 | 활용 범위 | 비고 |
|------|-----------|------|
| **Cursor (AI Agent)** | CardBattle Presentation 레이어 리팩터(cue Command 패턴, clip resolver, floating text 풀), README·아키텍처 문서 정리, 코드 리뷰·버그 수정 | 게임 **로직·수치 설계의 SSOT는 코드·SO**; AI 출력은 리뷰 후 반영 |
| **ChatGPT 등** | 초기 Utility AI 수식 검증, asmdef·브릿지 설계 논의 | (README 이전 버전 기록) |

AI가 **직접 생성·수정한 것:** C# 스크립트 구조, 에디터 셋업 보조 코드, 문서. **AI가 생성하지 않은 것:** Unity 씬·프리팹 바이너리, Behavior/Card SO 필드 값(에디터·메뉴로 시드).

### 외부 패키지 · 에셋

| 이름 | 출처 | 프로젝트 내 활용 |
|------|------|------------------|
| **UniTask** | [Cysharp/UniTask](https://github.com/Cysharp/UniTask) (Git UPM) | 턴·연출 async, FSM 대기 (`docs/tech-stack.md` 필수) |
| **DOTween / DOTween Pro** | Demigiant (`Assets/Plugins/Demigiant/`) | HP tween, dash, shake, floating text, 투사체 이동 |
| **Odin Inspector** | Sirenix (`Assets/Plugins/Sirenix/`) | SO·Inspector `[FoldoutGroup]` 등 **에디터 UI 전용** |
| **TextMesh Pro** | Unity Package | 카드 HP 라벨, stat floating text, UI |
| **Input System** | `com.unity.inputsystem` | 포인터·드래그 (`UnityEngine.Input` 미사용) |
| **URP** | `com.unity.render-pipelines.universal` | 렌더·Post Processing Bloom |
| **Cartoon FX Remaster FREE** | JMO Assets (`Assets/JMO Assets/Cartoon FX Remaster/`) | SharedHitVfx 등 이펙트 **후보·데모** (Behavior SO VFX 슬롯에 연결 가능) |
| **Katuri 폰트** | `Assets/Fonts/Katuri/` | UI·상세 오버레이 TMP SDF (`CardBattleSceneSetup`) |
| **Cursor IDE integration** | [com.unity.ide.cursor](https://github.com/boxqkrtm/com.unity.ide.cursor) | 에디터 연동 (개발 환경) |
| **Unity MCP** | [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | 에디터 MCP (개발·디버그 보조) |

### AI·프로시저럴 생성 리소스 (아트)

| 리소스 | 출처·범위 |
|--------|-----------|
| `Resources/CardBattle/Art/*.png` | 프로젝트 기본 카드·영웅 일러스트 (`Assign Default Card Illustrations` 메뉴). **제3자 AI 생성 여부는 파일별로 상이** — 배포 시 출처·라이선스 개별 확인 권장 |
| `CardIllustration_Default.png`, `hero1.png`, `hero2.png` | 기본 placeholder / 영웅 초상 |
| `SharedHitVfx.prefab` | Cartoon FX 또는 에디터 메뉴 `Assign Default Presentation Vfx`로 Behavior에 연결; 미할당 시 `CardPresentationDefaults` Resources 폴백 |

### 라이선스

- **본 저장소 코드:** MIT License — Copyright (c) 2026 JongWan Shin ([`LICENSE`](LICENSE))
- **서드파티:** DOTween, Odin, Cartoon FX, UniTask, Katuri 등 **각 패키지 LICENSE·Asset Store 약관 준수**

---

## 에디터 셋업 (요약)

**권장 일괄:** `CardGame → CardBattle → Setup CardBattle Scene`

| 메뉴 | 용도 |
|------|------|
| `Create Default Behavior Assets` | Behavior SO 4종 + 기본 VFX |
| `Link Card Data To Behaviors` | CardData ↔ Behavior 연결 |
| `Assign Default Card Illustrations` | 기본 일러스트 |
| `Ensure Hero Arena Presenter` | 3D 영웅 패널 |
| `Ensure Card Detail Overlay` | 롱프레스 상세 UI |
| `Wire Default Decks To Scene` | BattleBridge 덱 연결 |

전체 절차·수동 점검: 이전 README 절차와 동일. 씬 경로: `Assets/Scenes/CardBattleScene.unity`.

---

## 문서 유지보수

| 문서 | 용도 |
|------|------|
| [`docs/tech-stack.md`](docs/tech-stack.md) | 스택·C# 9 제약·패키지 정본 |
| [`docs/card-battle-architecture.md`](docs/card-battle-architecture.md) | 레이어·파이프라인·리팩터 체크리스트 |
| `.cursor/rules/collaboration-rules.mdc` | 협업·Agent 승인 규칙 |

`Assets/Scripts/CardBattle/` 구조를 바꿀 때는 **README와 `docs/card-battle-architecture.md`를 함께 갱신**한다.

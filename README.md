# CardGame — Card Battle

> **YouTube 시연:** _(녹화 후 링크 추가)_

## 환경

- **Unity:** 6000.3.10f1 (Unity 6 LTS)
- **렌더 파이프라인:** Universal Render Pipeline (URP)
- **화면:** Landscape (가로) 고정
- **플랫폼:** Android ARM64 APK

## 구현 기능

### 필수

- [x] 턴제 카드 배틀 (플레이어 6장 / 적 6장, 전장 3 + 대기 3)
- [x] 카드 타입: Normal, Ranged, Musou, Healer (PDF 수식 준수)
- [x] 턴 시작 힐러 광역 자가 회복 (+1, 자신 제외)
- [x] 공격 / 반격 / 사망 / 대기 카드 자동 전진
- [x] 승패 판정 (한 진영 6장 전멸)
- [x] State 패턴 FSM (Init → Player → Battle → Enemy → GameOver)
- [x] Strategy 패턴 카드 효과
- [x] Utility AI 적 타겟 선정
- [x] UI: 턴 배너, 대기 덱 수, HP바, 결과 팝업, Restart

### 가산점

- [x] URP Post Processing (Bloom 펄스 — 힐/공격)
- [x] DOTween + 코루틴 이중 연출 (HP바, 대시, Shake, 카메라 셰이크)
- [x] `CardBattle.asmdef` 컴파일 격리
- [x] `BattleBridge` / `ICardDataLoader` / `BattleAudioAdapter` 이식 브릿지
- [x] Input System 추상화 (`IInputProvider`)
- [x] UniTask 비동기 턴/연출
- [ ] 카드 일러스트 에셋 (Inspector/SO 할당 필요 — **에디터 작업**)

## 폴더 구조

```
Assets/Scripts/CardBattle/
├── CardBattle.asmdef
├── AI/EnemyAIController.cs
├── Bridge/BattleBridge.cs, ICardDataLoader.cs, ...
├── Cards/CardModel, CardView, CardEffects, ...
├── Core/GameManager, BattleField, ...
├── States/Init, PlayerTurn, EnemyTurn, Battle, GameOver
└── UI/UIManager.cs
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

[CardModel] ──▶ [ICardEffect / CardEffectRegistry]
                  Normal | Ranged | Musou | Healer
```

**네임스페이스:** `CardGame.CardBattle.*`

## 에디터 수동 작업

1. **씬:** `Assets/Scenes/CardBattleScene.unity`
2. **루트 오브젝트**
   - `BattleSystems` — `GameManager`, `BattleBridge`, `BattleSceneBootstrap` 부착
   - `UIManager` — `UIManager` 부착, Canvas (Screen Space Overlay, Landscape 레이아웃)
   - `EventSystem` (Input System UI Module)
3. **카드 슬롯 (×6)** — Player 3 / Enemy 3 자식 RectTransform + `CardView` + Image + Slider + TMP
4. **GameManager Inspector** — `playerCardViews[3]`, `enemyCardViews[3]`, `uiManager` 드래그 연결
5. **UIManager** — 턴 배너 TMP, 대기 덱 TMP, Win/Lose 패널, Restart 버튼, Global Volume 연결
6. **(선택)** `Assets/Resources/CardBattle/Cards/` 에 CardDataAsset 6종 생성 후 BattleBridge에 할당  
   ※ 미할당 시 `RuntimeDeckFactory`가 런타임 테스트 덱 생성
7. **Build Settings** — CardBattleScene 추가, Android ARM64 APK 빌드
8. **Bundle ID** — `Player Settings → Android → Package Name` 변경

## AI 도구 활용

본 프로젝트는 ChatGPT/Cursor 등 AI 어시스턴트를 활용하여 설계 검증, Utility AI 수식, asmdef 격리 및 브릿지 설계에 사용했습니다.

## 라이선스

MIT License — Copyright (c) 2026 JongWan Shin. `LICENSE` 파일 참조.

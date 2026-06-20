# Tech Stack (Canonical)

## Engine & Pipeline

* Unity **6000.3.10f1 (LTS)**, **URP**

## C# 9.0

* **PROHIBITED:** C# 10+ (global using, file-scoped types, required members, etc.)
* **PROHIBITED:** `record` unless `IsExternalInit` is validated in the project
* **CONDITIONAL:** `init`-only setters only with validated `IsExternalInit`; else `private set` or constructor immutability
* **ALLOWED:** pattern matching

## Packages & Input

* **UniTask** — mandatory for new async logic
* **Odin Inspector** — UI/Inspector attributes only; no `OdinSerializer`
* **Input System** (`com.unity.inputsystem`) — **New Input only**

### Input (New Input System exclusive)

* **Player Settings:** Active Input Handling = **Input System Package (New)** (or Both only during migration; new code must not depend on legacy)
* **PROHIBITED:** `UnityEngine.Input` — `Input.mousePosition`, `Input.GetKey`, `Input.touchCount`, `Input.GetTouch`, etc.
* **PROHIBITED:** `StandaloneInputModule` on new scenes (use `InputSystemUIInputModule`)
* **PREFERRED (UI / 3D pointer):** EventSystem + `InputSystemUIInputModule`; handle via `IPointerDownHandler`, `IPointerMoveHandler`, `IBeginDragHandler`, etc. and `PointerEventData.position` — no legacy polling
* **If polling is unavoidable:** `UnityEngine.InputSystem` — `Mouse.current`, `Touchscreen.current`, `Keyboard.current` (never `UnityEngine.Input`)
* **CardBattle:** card hit → PhysicsRaycaster + `ICardInputHost` events; domain routing via `IInputProvider` / `UnityInputProvider`

## Assembly & Namespace

* `.asmdef` present → assembly name = root namespace
* No `.asmdef` → infer `ProjectName.FeatureName` from context; one conservative default + confirm if conflict
* New namespace or assembly requires prior approval

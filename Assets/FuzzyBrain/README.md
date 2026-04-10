# FuzzyBrain

Fuzzy Pattern Matching for game AI and behaviour. Based on the system described in the Game Developer article *"Creating Behaviours with Fuzzy Pattern Matching"*.

---

## Core Idea

An `Actor` holds a **ScriptableActivityList** — a prioritised set of **Acts**. Each tick, acts are evaluated top-to-bottom. The first act whose conditions all pass fires (FSM mode), or all matching acts fire (FuSM mode).

Acts are sorted automatically by **specificity**: the more conditions an act has, the higher its priority. An act with 3 conditions always beats an act with 1 condition.

---

## Quick Start

1. Add an **Actor** component to your GameObject.
2. Create an **Activity List** asset: `Assets > Create > FuzzyBrain > Activity List`.
3. Assign it to the Actor's **Activities** field.
4. Open the editor via `Tools > FuzzyBrain > Editor` (or the button on the Actor inspector).
5. Add **Act** rows, drag **Condition** assets onto each row, and wire **On Fire** to any method.

---

## Logic Operators

| Operator | How |
|---|---|
| AND | All conditions on an act must pass (implicit) |
| NOT | Toggle the **Inverted** checkbox on any condition asset |
| OR | Not supported — add a second Act row instead (see below) |

### Why no OR?

OR breaks the specificity-priority system. If an act has conditions `(A OR B)`, how many conditions does it have — 1 or 2? The answer changes depending on which branch is true, making a consistent sort order impossible.

**Idiomatic OR — add two rows:**

```
Act: Jump Attack   [IsGrounded] [IsAttacking]   → PlayJumpAttackAnim()
Act: Jump Attack   [IsGrounded] [IsFalling]     → PlayJumpAttackAnim()
```

Both rows wire to the same method. The act fires under either set of circumstances, and priority remains consistent.

---

## Output: UnityEvents

Each `Act` asset has an **On Fire** `UnityEvent`. Wire it to any method on any component — identical to `Button.onClick`. No C# wiring required.

---

## FSM vs FuSM

Toggled on the **Actor** component (`isFuSM` field).

| Mode | Behaviour |
|---|---|
| FSM (default) | Stops after the first matching act each tick |
| FuSM | Evaluates all acts and fires every match |

- Use **FSM** for exclusive state machines (locomotion, enemy states).
- Use **FuSM** for layered behaviours (audio + animation + VFX all reacting to the same condition set).

---

## Custom Conditions

Subclass `Condition<T>` where `T` is the component your condition reads from.
The component is resolved from the Actor's cache — `GetComponent` is never called in the hot path.

```csharp
[CreateAssetMenu(menuName = "FuzzyBrain/Conditions/HasAmmo")]
public class HasAmmoCondition : Condition<WeaponComponent>
{
    public int minAmmo = 1;

    protected override bool Verify(WeaponComponent weapon)
    {
        bool result = weapon.ammo >= minAmmo;
        return inverted ? !result : result;
    }
}
```

Use `Tools > FuzzyBrain > New Condition` to generate this boilerplate automatically.

### IGizmoDrawable

Implement `IGizmoDrawable` on spatial conditions to draw their query volume in the Scene view when the Actor is selected.

```csharp
public class IsGrounded : Condition<Collider2D>, IGizmoDrawable
{
    protected override bool Verify(Collider2D col) { ... }

    public void DrawGizmo(ActContext ctx)
    {
        Collider2D col = ctx.Get<Collider2D>();
        bool hit = /* repeat query */;
        Gizmos.color = Application.isPlaying ? (hit ? Color.green : Color.red) : Color.grey;
        Gizmos.DrawLine(...);
    }
}
```

---

## ActContext

Every condition and act receives an `ActContext` — a zero-allocation readonly struct built once per tick.

```csharp
// O(1) cached component access — never calls GetComponent at runtime
Rigidbody2D rb = ctx.Get<Rigidbody2D>();

// Per-tick condition cache — each unique condition SO evaluates Verify() at most once
bool passed = ctx.Evaluate(someCondition);

// Actor reference
ctx.Actor.Die();
```

---

## Built-in Conditions

| Class | Required Component | Description |
|---|---|---|
| `OnGround` | `Collider2D` | Downward raycast to a specific layer. IGizmoDrawable. |
| `AgainstWall` | `Collider2D` | Horizontal raycast left or right. IGizmoDrawable. |
| `IsTouchingLayer` | `Collider2D` | `IsTouchingLayers` contact check. IGizmoDrawable. |
| `OnSurface` | `Collider2D` | Downward raycast excluding actor's own layer. IGizmoDrawable. |
| `FallingCondition` | `Rigidbody2D` | `linearVelocity.y` below threshold. |
| `IsStill` | `Rigidbody2D` | Speed magnitude below threshold. |
| `CanAct` | `Actor` | `Actor.canAct` is true. |
| `IdleCondition` | `Actor` | `Actor.isIdle` is true. |
| `RandomRollCondition` | `Actor` | 1-in-N probability gate. |

---

## Built-in Actor Methods (wire via UnityEvent)

| Method | Effect |
|---|---|
| `Die()` | Sets `isAlive = false`, deactivates the GameObject |
| `AddIdleTime()` | Accumulates idle time, sets `isIdle` once threshold is reached |
| `ResetIdle()` | Clears idle time and the `isIdle` flag |

---

## DynamicBehaviourManager (optional)

Add a single `DynamicBehaviourManager` component to the scene to distribute actor evaluation across staggered time buckets. Actors register automatically on `OnEnable`.

- **bucketCount** — how many stagger groups (default 4).
- **tickInterval** — seconds between full evaluations per actor (default 0.1s).

Without a manager in the scene, each Actor self-ticks every frame via `Update()`. Both modes produce identical behaviour.

---

## Editor Tools

| Window | Open via |
|---|---|
| FuzzyBrain Editor | `Tools > FuzzyBrain > Editor` or Actor inspector button |
| New Condition | `Tools > FuzzyBrain > New Condition` |
| New Act | `Tools > FuzzyBrain > New Act` |
| Project Settings | `Edit > Project Settings > DynamicBehaviour` |

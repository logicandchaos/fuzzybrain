# FuzzyBrain

A lightweight, data-driven behaviour system for Unity. Based on the fuzzy pattern matching approach described in the Game Developer article *"Creating Behaviours with Fuzzy Pattern Matching"*.

---

## Core Idea

An **Actor** holds a **ScriptableActivityList** — a prioritised list of **Acts**. Each tick, acts are evaluated top-to-bottom. The first act whose conditions all pass fires its `onFire` UnityEvent (FSM mode), or every matching act fires (FuSM mode).

Acts are sorted automatically by **specificity**: more conditions = higher priority. No manual ordering required.

```
Act — 3 conditions   ← evaluated first
Act — 2 conditions
Act — 1 condition
Act — 0 conditions   ← fallback, always fires
```

---

## Quick Start

1. Add an **Actor** component to your GameObject.
2. Open `Tools > FuzzyBrain > Editor`.
3. With the Actor selected, click **+ New List** to create a `ScriptableActivityList` asset.
4. Click **+ New Act** to open the Act Wizard, or **+ Add Act** to pick an existing Act asset.
5. Select an act row in the list to open its detail panel — add conditions and wire **On Fire** to any method.

---

## Evaluation Modes

Toggle `isFuSM` on the Actor component.

| Mode | Behaviour |
|---|---|
| **FSM** (default) | Stops after the first matching act each tick. |
| **FuSM** | Evaluates all acts and fires every match. |

- Use **FSM** for exclusive states (locomotion, enemy AI).
- Use **FuSM** for layered reactions (audio + animation + VFX all driven by the same conditions).

---

## Logic Operators

| Operator | How |
|---|---|
| AND | All conditions on an act must pass (implicit). |
| NOT | Toggle **Inverted** on any condition asset. |
| OR | Not directly supported — add a second Act with the alternate conditions instead. |

**Why no OR?** OR breaks the specificity sort. Add two Act rows wired to the same method:

```
Act [IsGrounded][IsAttacking] → PlayJumpAttack()
Act [IsGrounded][IsFalling]   → PlayJumpAttack()
```

---

## Writing a Custom Condition

Subclass `Condition<T>` where `T` is the component your condition reads. The component is resolved from a cache built on `Awake` — `GetComponent` is never called in the hot path.

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

Use `Tools > FuzzyBrain > New Condition` to generate this boilerplate, or use the **Quick Condition** tab to generate a field-comparison condition without writing any code.

---

## ActContext

Every condition and act receives an `ActContext` — a zero-allocation `readonly struct` built once per tick.

```csharp
// O(1) cached component access — never calls GetComponent at runtime
Rigidbody2D rb = ctx.Get<Rigidbody2D>();

// Per-tick condition deduplication — each unique condition SO calls Verify() at most once
bool passed = ctx.Evaluate(someCondition);

// Actor reference
ctx.Actor.Die();
```

---

## IGizmoDrawable

Implement `IGizmoDrawable` on spatial conditions to draw their query volume in the Scene view when the Actor is selected. Grey in edit mode, green when passing, red when failing.

```csharp
public class IsGrounded : Condition<Collider2D>, IGizmoDrawable
{
    public int groundLayer;

    protected override bool Verify(Collider2D col)
    {
        float len = col.bounds.extents.y + 0.1f;
        bool hit  = Physics2D.Raycast(col.transform.position, Vector2.down, len, 1 << groundLayer);
        return inverted ? !hit : hit;
    }

    public void DrawGizmo(ActContext ctx)
    {
        Collider2D col = ctx.Get<Collider2D>();
        if (col == null) return;
        float len = col.bounds.extents.y + 0.1f;
        bool hit  = Physics2D.Raycast(col.transform.position, Vector2.down, len, 1 << groundLayer);
        Gizmos.color = Application.isPlaying ? (hit ? Color.green : Color.red) : Color.grey;
        Gizmos.DrawLine(col.transform.position, col.transform.position + Vector3.down * len);
    }
}
```

---

## Built-in Conditions

| Class | Component | Description |
|---|---|---|
| `CanAct` | `Actor` | Passes when `Actor.canAct` is true. Use as a cooldown gate. |
| `IdleCondition` | `Actor` | Passes when `Actor.isIdle` is true. |
| `RandomRollCondition` | `Actor` | 1-in-N random chance each evaluation. |
| `OnGround` | `Collider2D` | Downward raycast to a ground layer. IGizmoDrawable. |
| `OnSurface` | `Collider2D` | Downward raycast excluding the actor's own layer. IGizmoDrawable. |
| `AgainstWall` | `Collider2D` | Horizontal raycast left or right. IGizmoDrawable. |
| `IsTouchingLayer` | `Collider2D` | Contact check against a layer mask. IGizmoDrawable. |
| `FallingCondition` | `Rigidbody2D` | `linearVelocity.y` below threshold. |
| `IsStill` | `Rigidbody2D` | Speed magnitude below threshold. |

---

## Built-in Actor Methods

Safe to wire directly to any Act's `onFire` UnityEvent.

| Method | Effect |
|---|---|
| `Die()` | Sets `isAlive = false` and deactivates the GameObject. |
| `AddIdleTime()` | Accumulates idle time each call. Sets `isIdle = true` once `idleDelay` is exceeded. |
| `ResetIdle()` | Clears `isIdle` and resets the idle accumulation timer. |
| `StartResetCanAct(float)` | Starts a cooldown coroutine that restores `canAct` after the given delay. |

---

## Act Cooldowns

Enable **Set Can Act** on an Act and add a **CanAct** condition to it. When the act fires, `Actor.canAct` is set to `false` for **Reset Time** seconds — any act including **CanAct** will not fire during that window.

---

## Idle Detection

Wire `Actor.AddIdleTime()` to the `onFire` of a zero-condition fallback act. Wire `Actor.ResetIdle()` (or enable **Reset Idle**) on any active acts. Add an `IdleCondition` to acts that should trigger after prolonged inactivity. Set `idleDelay` on the Actor to control the threshold in seconds.

---

## FuzzyBrainManager (optional)

Add a single **FuzzyBrainManager** component to the scene to distribute actor evaluations across staggered time buckets. Actors register and unregister automatically.

| Field | Default | Description |
|---|---|---|
| `bucketCount` | `4` | Number of stagger groups. |
| `tickInterval` | `0.1` | Seconds between full evaluations per actor. |

Without a manager, each Actor self-ticks every `Update()` frame. Both modes produce identical behaviour — the manager only changes timing and batching.

---

## Editor Tools

| Tool | Open via |
|---|---|
| **FuzzyBrain Window** | `Tools > FuzzyBrain > Editor` or the button on the Actor Inspector |
| **Act Wizard** | `Tools > FuzzyBrain > New Act` or **+ New Act** in the window |
| **Condition Wizard** | `Tools > FuzzyBrain > New Condition` or the button in the Act detail panel |
| **Project Settings** | `Edit > Project Settings > FuzzyBrain` |

The Condition Wizard has three tabs: **Generate Script** (boilerplate `Condition<T>` subclass), **Create Asset** (instantiate a compiled condition type), and **Quick Condition** (generate a field-comparison condition with no code).

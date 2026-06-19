# FuzzyBrain

A lightweight, data-driven behaviour system for Unity. Acts and Conditions are ScriptableObject assets — behaviour is configured in the editor, not hard-coded.

---

## Core Idea

An **Actor** holds a **ScriptableActList** — a prioritised list of **Acts**. Each tick the manager calls `ActorUpdate` on every registered Actor. Acts are evaluated top-to-bottom; the first act whose conditions all pass fires and stops evaluation for that tick.

Acts are sorted automatically by **specificity**: more conditions = higher priority. The zero-condition act is always last and acts as the fallback.

```
Act — 3 conditions   ← evaluated first
Act — 2 conditions
Act — 1 condition
Act — 0 conditions   ← fallback, always fires if nothing else matches
```

---

## Quick Start

1. Add an **Actor** component to your GameObject.
2. Open `Tools > FuzzyBrain > Editor`.
3. With the Actor selected, click **+ New List** to create a `ScriptableActList` asset.
4. Click **+ New Act** to open the Act Wizard, or drag an existing Act asset into the list.
5. Select a row to open the Act detail panel — add Condition assets and set `maxClockTime` if the act should lock.

---

## Writing a Custom Act

Subclass `Act` and override `PerformAct`. Use the Act Wizard (`Tools > FuzzyBrain > New Act`) or right-click the Project window and choose **Create > FuzzyBrain > Act Script** to generate the boilerplate.

```csharp
[CreateAssetMenu(menuName = "FuzzyBrain/Acts/PlayAttackAnim")]
public class PlayAttackAnim : Act
{
    public override void PerformAct(ActContext ctx)
    {
        var anim = ctx.Get<Animator>();
        if (anim == null) return;
        anim.SetTrigger("Attack");
    }
}
```

Override `OnStart` for one-shot setup that runs once per lock cycle. Override `IsComplete` to keep the Actor locked until a signal arrives (animation end, physics event, etc.). Always set `maxClockTime` on the asset as a safety timeout when overriding `IsComplete`.

---

## Writing a Custom Condition

Subclass `Condition<T>` where `T` is the component the condition reads. The component is resolved from a cache built on `Awake` — `GetComponent` is never called in the hot path. Use the Condition Wizard (`Tools > FuzzyBrain > New Condition`) or right-click the Project window and choose **Create > FuzzyBrain > Condition Script** to generate the boilerplate.

```csharp
[CreateAssetMenu(menuName = "FuzzyBrain/Conditions/HasAmmo")]
public class HasAmmo : Condition<WeaponComponent>
{
    public int minAmmo = 1;

    protected override bool Verify(WeaponComponent weapon)
    {
        bool result = weapon.ammo >= minAmmo;
        return inverted ? !result : result;
    }
}
```

Always apply `inverted` before returning — without it the Inspector's invert toggle silently does nothing.

Use the **Quick Condition** tab in the Condition Wizard to generate a field-comparison condition without writing any code.

---

## ActContext

Every Act and Condition receives an `ActContext` — a zero-allocation `readonly struct` built once per tick.

```csharp
// O(1) cached component lookup — never calls GetComponent at runtime
var rb = ctx.Get<Rigidbody2D>();

// Actor reference
ctx.Actor.Die();
```

`ctx.Get<T>()` returns `null` and logs a warning if the component is not on the Actor. Acts must null-check the result.

---

## Logic Operators

| Operator | How |
|---|---|
| AND | All conditions on an act must pass (implicit). |
| NOT | Toggle `inverted` on any Condition asset. |
| OR | Not directly supported — add a second Act row wired to the same behaviour instead. |

**Why no OR?** OR breaks the specificity sort. Add two Act rows with the alternate condition sets:

```
Act [IsGrounded][IsAttacking] → PlayJumpAttack()
Act [IsGrounded][IsFalling]   → PlayJumpAttack()
```

---

## IGizmoDrawable

Implement `IGizmoDrawable` on spatial conditions to visualise their query volume in the Scene view when the Actor is selected. Grey in Edit mode, green when passing, red when failing.

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
        var col = ctx.Get<Collider2D>();
        if (col == null) return;
        float len = col.bounds.extents.y + 0.1f;
        bool  hit = Physics2D.Raycast(col.transform.position, Vector2.down, len, 1 << groundLayer);
        Gizmos.color = Application.isPlaying ? (hit ? Color.green : Color.red) : Color.grey;
        Gizmos.DrawLine(col.transform.position, col.transform.position + Vector3.down * len);
    }
}
```

---

## Built-in Conditions

| Class | Component | Description |
|---|---|---|
| `IdleCondition` | `Actor` | Passes when `Actor.isIdle` is `true`. |
| `RandomRollCondition` | `Actor` | 1-in-N random roll per evaluation. Default N = 5 (20% chance). |
| `PreviousActCondition` | `ActHistory` | Passes when the act at a history offset matches an expected Act asset. |
| `OnGround` | `Collider2D` | Downward raycast to a ground layer. `IGizmoDrawable`. |
| `OnSurface` | `Collider2D` | Downward raycast excluding the actor's own layer. `IGizmoDrawable`. |
| `AgainstWall` | `Collider2D` | Horizontal raycast left or right. `IGizmoDrawable`. |
| `IsTouchingLayer` | `Collider2D` | Contact check against a layer mask. `IGizmoDrawable`. |
| `FallingCondition` | `Rigidbody2D` | `linearVelocity.y` below threshold (default `-1`). |
| `IsStill` | `Rigidbody2D` | Speed magnitude below threshold (default `1`). |

All conditions expose an `inverted` field. The four `Collider2D` conditions implement `IGizmoDrawable`.

---

## Actor Public API

| Member | Type | Description |
|---|---|---|
| `isIdle` | `bool` field | Reset to `false` each tick. Set to `true` by `IdleAct.PerformAct`. |
| `isAlive` | `bool` field | Set to `false` by `Die()`. Read by external systems to know if the actor is alive. |
| `CurrentAct` | `Act` property | The act currently locked. `null` when no act is running. |
| `LastFiredAct` | `Act` property | The act that fired most recently. Used by the FuzzyBrain Window for play-mode highlighting. |
| `Die()` | method | Sets `isAlive = false` and deactivates the GameObject. Safe to wire to UnityEvents. |
| `ResetActor()` | method | Resets `isAlive`, `isIdle`, and the current act lock to initial values. |
| `EnableActor()` | method | Sorts the act list and calls `ResetActor()`. Called automatically on `OnEnable`. |
| `Refresh()` | method | Rebuilds the component cache and re-sorts the act list. Call after modifying acts or components at runtime. |
| `SetActList(list)` | method | Assigns a new act list and calls `Refresh()`. Does not reset actor state. |

---

## Idle Detection

`Actor.isIdle` is reset to `false` at the start of every tick. The built-in `IdleAct` sets `ctx.Actor.isIdle = true` in its `PerformAct`. Because it carries zero conditions it sorts last, firing only when every other act fails.

Add `IdleAct` with zero conditions as the fallback row. Add an `IdleCondition` to acts that should respond to idleness — they sort above `IdleAct` and fire on the tick after `isIdle` first becomes `true`.

```
PlayIdleAnim  [IdleCondition]   ← fires the tick after isIdle becomes true
Idle          (0 conditions)    ← sets isIdle = true
```

Subclass `IdleAct` to attach extra behaviour; call `base.PerformAct(ctx)` to preserve `isIdle` management.

---

## Act Cooldowns

When `OnStart` and `PerformAct` fire and `IsComplete` returns `false`, the Actor locks to that act — no other acts are evaluated until it unlocks. `IsComplete` is polled each tick; `maxClockTime` forces an unlock if it is exceeded.

**Fire-and-forget** — override only `PerformAct`. `IsComplete` returns `true` immediately; no lock is set.

**Fixed-duration lock** — leave `IsComplete` at its default; set `maxClockTime` on the asset. The lock lasts exactly that many seconds.

**Signal-driven lock** — override `IsComplete` to check an animation state, collision, or any game condition. Always set `maxClockTime` as a safety timeout.

```csharp
public override bool IsComplete(ActContext ctx)
{
    var anim = ctx.Get<Animator>();
    return anim == null || !anim.GetCurrentAnimatorStateInfo(0).IsName("Attack");
}
// Set maxClockTime: 2.0 on the asset — forces unlock if the animator gets stuck.
```

The lock state lives on the `Actor` and `ActClock` — multiple actors sharing the same `Act` asset each have independent lock state.

---

## Combo Sequences

Add an **ActHistory** component to the Actor and use **PreviousActCondition** assets to require an exact act sequence. No custom code needed.

`ActHistory` records each act when it unlocks. `PreviousActCondition.historyOffset = 0` checks the most recent completed act; `1` checks the one before that.

```
FinisherAttack  [IsPressingFinisher, PrevAct=Heavy@0, PrevAct=Light@1]   maxClockTime: 1.2
HeavyAttack     [IsPressingHeavy, PrevAct=Light@0]                        maxClockTime: 0.8
LightAttack     [IsPressingLight]                                          maxClockTime: 0.5
Idle            []
```

The specificity sort guarantees the finisher (most conditions) is evaluated first automatically.

---

## FuzzyBrainManager

Add a single **FuzzyBrainManager** component to the scene to distribute actor evaluations across staggered time buckets. All Actors register and unregister automatically via `OnEnable`/`OnDisable`.

If no manager is present when an Actor enables, one is created automatically with default settings.

| Field | Default | Description |
|---|---|---|
| `bucketCount` | `4` | Number of stagger groups. Higher values spread cost more evenly across frames. |
| `tickInterval` | `0.1` | Seconds between full evaluations per actor. `0` evaluates every frame. |

Set `bucketCount` to match the expected actor count — a good rule of thumb is one bucket per 10–20 actors. Manual registration: `FuzzyBrainManager.Instance?.Register(actor)` / `Unregister(actor)`.

---

## Runtime List Mutation

Swap an actor's list with `SetActList()`, or mutate the existing list and call `Refresh()` to re-sort and rebuild the component cache without resetting actor state.

Use `Clone()` to give each actor a private in-memory copy of a shared list asset:

```csharp
var brain = sharedTemplate.Clone();
actor.SetActList(brain);

brain.Add(newActAsset);    // marks dirty
actor.Refresh();           // re-sorts and rebuilds cache

brain.Remove(oldActAsset); // marks dirty
actor.Refresh();
```

Call `ResetActor()` alongside any of the above if a clean actor state is also needed.

---

## Editor Tools

| Tool | Open via |
|---|---|
| **FuzzyBrain Window** | `Tools > FuzzyBrain > Editor` |
| **Act Wizard** | `Tools > FuzzyBrain > New Act` or **+ New Act** in the window |
| **Condition Wizard** | `Tools > FuzzyBrain > New Condition` or the button in the Act detail panel |
| **Project Settings** | `Edit > Project Settings > FuzzyBrain` |
| **Create Actor** | Right-click Hierarchy → **FuzzyBrain > Actor** |
| **Act Script template** | Right-click Project window → **Create > FuzzyBrain > Act Script** |
| **Condition Script template** | Right-click Project window → **Create > FuzzyBrain > Condition Script** |

**Act Script / Condition Script** — writes a script to the folder you right-clicked using the same template as the wizard Generate Script tab. The namespace is read from Project Settings. For Condition Script, a small modal asks for the class name and component type before writing.

**FuzzyBrain Window** — select an Actor in the Hierarchy to load its list. The **Validate Act List** button runs a dry-run validation pass in Edit Mode: checks for missing components and calls `PerformAct` with a null-safe context.

**Act Wizard** — two tabs: **Generate Script** (Act subclass boilerplate) and **Create Asset** (instantiate a compiled type, optionally adding it directly to the open list).

**Condition Wizard** — three tabs: **Generate Script** (`Condition<T>` boilerplate), **Create Asset** (instantiate a compiled type), and **Quick Condition** (generate a field-comparison condition with no code).

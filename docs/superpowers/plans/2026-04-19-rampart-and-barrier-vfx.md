# Rampart And Barrier VFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce the Prismatic Barrier model size and improve readability, then add a persistent golden Fortified Rampart aura that follows the rook, covers the logical 5x5 area as an inscribed circle, and fades from strong to weak over the buff lifetime.

**Architecture:** Keep the existing gameplay logic intact and add visuals as an additive layer. Copy the Teleport aura prefab and its required dependencies into the stable `Assets/Resource/Prefab/texiao` area, then drive the persistent aura with a small runtime controller attached to the copied prefab instance and lifecycle data stored alongside the existing rampart aura state in `LogicManager`.

**Tech Stack:** Unity, C#, URP, prefab assets, particle systems, existing `SpellVFXManager` and `LogicManager` gameplay hooks.

---

### Task 1: Barrier readability and size fixes

**Files:**
- Modify: `Assets/Resource/Prefab/texiao/PrismaticBarrierModel.prefab`
- Modify: `Assets/Scripts/Gameplay/Board/LogicManager.cs`

- [ ] Reduce the barrier prefab root scale to roughly half-size and disable shadow cast/receive on the renderer.
- [ ] Raise the barrier spawn height slightly in `LogicManager.PlacePrismaticBarrier` so the reduced model sits visibly above the board plane.
- [ ] Refresh Unity and verify the prefab still resolves without missing references.

### Task 2: Stable Fortified Rampart aura asset migration

**Files:**
- Create: `Assets/Resource/Prefab/texiao/Teleport_1.prefab`
- Create: `Assets/Resource/Prefab/texiao/Teleport_1.prefab.meta` or let Unity generate it
- Create: copied dependency assets under `Assets/Resource/Prefab/texiao/PortalParticle_*`

- [ ] Identify the exact assets referenced by `Assets/Portal Particle/Prefab/Teleport_1.prefab`.
- [ ] Duplicate the prefab and each required dependency into the stable texiao path so deleting `Assets/Portal Particle` later will not break the new aura.
- [ ] Refresh Unity and verify the copied prefab opens with no missing mesh or material references.

### Task 3: Persistent Fortified Rampart aura hookup

**Files:**
- Create: `Assets/Scripts/Gameplay/Spells/vfx/RampartAuraVisual.cs`
- Modify: `Assets/Scripts/Gameplay/Board/LogicManager.cs`

- [ ] Add lifecycle fields to the existing rampart aura state so gameplay duration and VFX duration stay synchronized.
- [ ] Instantiate the copied Teleport aura prefab as an additive visual only, without removing the existing `SpellVFXManager` burst effects.
- [ ] Recolor the aura toward yellow/gold, scale it to the inscribed-circle size of the 5x5 protected area, and soften transparency.
- [ ] Make the aura follow the source rook while active and fade from stronger to weaker intensity across the 3-round lifetime.
- [ ] Clean up the aura instance as soon as the rampart buff expires or the source piece disappears.

### Task 4: Verification

**Files:**
- Verify: `Assets/Scripts/Gameplay/Board/LogicManager.cs`
- Verify: `Assets/Scripts/Gameplay/Spells/vfx/RampartAuraVisual.cs`
- Verify: copied aura assets in `Assets/Resource/Prefab/texiao`

- [ ] Refresh assets and request script compilation in Unity.
- [ ] Read the Unity console and confirm there are no new compile or missing-reference errors.
- [ ] Use editor inspection or screenshots to confirm the barrier is smaller/brighter and the rampart aura is golden, centered on the rook, sized to the 5x5 inscribed circle, and visually weaker near expiry.

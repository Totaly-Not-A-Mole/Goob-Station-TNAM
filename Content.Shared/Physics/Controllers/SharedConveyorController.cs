// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Alice "Arimah" Heurlin <30327355+arimah@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Chief-Engineer <119664036+Chief-Engineer@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 DrSmugleaf <10968691+DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Flareguy <78941145+Flareguy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 HS <81934438+HolySSSS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 IProduceWidgets <107586145+IProduceWidgets@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Mr. 27 <45323883+Dutch-VanDerLinde@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 PJBot <pieterjan.briers+bot@gmail.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Rouge2t7 <81053047+Sarahon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 Truoizys <153248924+Truoizys@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 TsjipTsjip <19798667+TsjipTsjip@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ubaser <134914314+UbaserB@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Vasilis <vasilis@pikachu.systems>
// SPDX-FileCopyrightText: 2024 beck-thompson <107373427+beck-thompson@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 lzk <124214523+lzk228@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 osjarw <62134478+osjarw@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 plykiya <plykiya@protonmail.com>
// SPDX-FileCopyrightText: 2024 Арт <123451459+JustArt1m@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Conveyor;
using Content.Shared.Gravity;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Controllers;

public abstract class SharedConveyorController : VirtualController
{
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    protected const string ConveyorFixture = "conveyor";

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private ValueList<EntityUid> _ents = new();
    private HashSet<Entity<ConveyorComponent>> _conveyors = new();

    public override void Initialize()
    {
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        UpdatesAfter.Add(typeof(SharedMoverController));

        SubscribeLocalEvent<ConveyorComponent, StartCollideEvent>(OnConveyorStartCollide);
        SubscribeLocalEvent<ConveyorComponent, EndCollideEvent>(OnConveyorEndCollide);

        base.Initialize();
    }

    private void OnConveyorStartCollide(EntityUid uid, ConveyorComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!args.OtherFixture.Hard || args.OtherBody.BodyType == BodyType.Static || component.State == ConveyorState.Off)
            return;

        var conveyed = EnsureComp<ConveyedComponent>(otherUid);

        if (conveyed.Colliding.Contains(uid))
            return;

        conveyed.Colliding.Add(uid);
        Dirty(otherUid, conveyed);
    }

    private void OnConveyorEndCollide(Entity<ConveyorComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp(args.OtherEntity, out ConveyedComponent? conveyed))
            return;

        if (!conveyed.Colliding.Remove(ent.Owner))
            return;

        Dirty(args.OtherEntity, conveyed);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<ConveyedComponent, TransformComponent, PhysicsComponent>();
        _ents.Clear();

        while (query.MoveNext(out var uid, out var comp, out var xform, out var physics))
        {
            if (TryConvey((uid, comp, physics, xform), prediction, frameTime))
                continue;

            _ents.Add(uid);
        }

        foreach (var ent in _ents)
        {
            RemComp<ConveyedComponent>(ent);
        }
    }

    private bool TryConvey(Entity<ConveyedComponent, PhysicsComponent, TransformComponent> entity, bool prediction, float frameTime)
    {
        var physics = entity.Comp2;
        var xform = entity.Comp3;
        var contacting = entity.Comp1.Colliding.Count > 0;

        if (!contacting)
            return false;

        // Client moment
        if (!physics.Predict && prediction)
            return true;

        if (physics.BodyType == BodyType.Static)
            return false;

        if (!_gridQuery.TryComp(xform.GridUid, out var grid))
            return true;

        var gridTile = _maps.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);
        _conveyors.Clear();

        // Check for any conveyors on the attached tile.
        Lookup.GetLocalEntitiesIntersecting(xform.GridUid.Value, gridTile, _conveyors);
        DebugTools.Assert(_conveyors.Count <= 1);

        // No more conveyors.
        if (_conveyors.Count == 0)
            return true;

        if (physics.BodyStatus == BodyStatus.InAir ||
            _gravity.IsWeightless(entity, physics, xform))
        {
            return true;
        }

        Entity<ConveyorComponent> bestConveyor = default;
        var bestSpeed = 0f;

        foreach (var conveyor in _conveyors)
        {
            if (conveyor.Comp.Speed > bestSpeed && CanRun(conveyor))
            {
                bestSpeed = conveyor.Comp.Speed;
                bestConveyor = conveyor;
            }
        }

        if (bestSpeed == 0f || bestConveyor == default)
            return true;

        var comp = bestConveyor.Comp!;
        var conveyorXform = _xformQuery.GetComponent(bestConveyor.Owner);
        var conveyorPos = conveyorXform.LocalPosition;
        var conveyorRot = conveyorXform.LocalRotation;

        conveyorRot += bestConveyor.Comp!.Angle;

        if (comp.State == ConveyorState.Reverse)
            conveyorRot += MathF.PI;

        var direction = conveyorRot.ToWorldVec();

        var localPos = xform.LocalPosition;
        var itemRelative = conveyorPos - localPos;

        localPos += Convey(direction, bestSpeed, frameTime, itemRelative);

        TransformSystem.SetLocalPosition(entity, localPos, xform);

        // Force it awake for collisionwake reasons.
        Physics.SetAwake((entity, physics), true);
        Physics.SetSleepTime(physics, 0f);

        return true;
    }

    private static Vector2 Convey(Vector2 direction, float speed, float frameTime, Vector2 itemRelative)
    {
        if (speed == 0 || direction.Length() == 0)
            return Vector2.Zero;

        /*
         * Basic idea: if the item is not in the middle of the conveyor in the direction that the conveyor is running,
         * move the item towards the middle. Otherwise, move the item along the direction. This lets conveyors pick up
         * items that are not perfectly aligned in the middle, and also makes corner cuts work.
         *
         * We do this by computing the projection of 'itemRelative' on 'direction', yielding a vector 'p' in the direction
         * of 'direction'. We also compute the rejection 'r'. If the magnitude of 'r' is not (near) zero, then the item
         * is not on the centerline.
         */

        var p = direction * (Vector2.Dot(itemRelative, direction) / Vector2.Dot(direction, direction));
        var r = itemRelative - p;

        if (r.Length() < 0.1)
        {
            var velocity = direction * speed;
            return velocity * frameTime;
        }
        else
        {
            // Give a slight nudge in the direction of the conveyor to prevent
            // to collidable objects (e.g. crates) on the locker from getting stuck
            // pushing each other when rounding a corner.
            var velocity = (r + direction*0.2f).Normalized() * speed;
            return velocity * frameTime;
        }
    }

    public bool CanRun(ConveyorComponent component)
    {
        return component.State != ConveyorState.Off && component.Powered;
    }
}
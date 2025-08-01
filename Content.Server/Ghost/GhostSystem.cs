// SPDX-FileCopyrightText: 2021 Acruid
// SPDX-FileCopyrightText: 2021 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto
// SPDX-FileCopyrightText: 2021 metalgearsloth
// SPDX-FileCopyrightText: 2022 Illiux
// SPDX-FileCopyrightText: 2022 Jacob Tong
// SPDX-FileCopyrightText: 2022 Júlio César Ueti
// SPDX-FileCopyrightText: 2022 mirrorcult
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2023 Checkraze
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 Jezithyr
// SPDX-FileCopyrightText: 2023 Leon Friedrich
// SPDX-FileCopyrightText: 2023 Nemanja
// SPDX-FileCopyrightText: 2023 ShadowCommander
// SPDX-FileCopyrightText: 2023 Visne
// SPDX-FileCopyrightText: 2024 Kara
// SPDX-FileCopyrightText: 2024 LordCarve
// SPDX-FileCopyrightText: 2024 Plykiya
// SPDX-FileCopyrightText: 2024 Whatstone
// SPDX-FileCopyrightText: 2024 Winkarst
// SPDX-FileCopyrightText: 2024 deltanedas
// SPDX-FileCopyrightText: 2024 lzk
// SPDX-FileCopyrightText: 2024 nikthechampiongr
// SPDX-FileCopyrightText: 2025 Alice "Arimah" Heurlin
// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 SpaceManiac
// SPDX-FileCopyrightText: 2025 Tayrtahn
// SPDX-FileCopyrightText: 2025 keronshb
// SPDX-FileCopyrightText: 2025 monolith8319
// SPDX-FileCopyrightText: 2025 slarticodefast
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers; // Frontier
using Content.Server.Cargo.Systems; // Frontier
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.FixedPoint;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Preferences.Managers;

namespace Content.Server.Ghost
{
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly IAdminLogManager _adminLog = default!;
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly JobSystem _jobs = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly MindSystem _minds = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly IAdminManager _admin = default!; // Frontier
        [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;

        private EntityQuery<GhostComponent> _ghostQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;

        public override void Initialize()
        {
            base.Initialize();

            _ghostQuery = GetEntityQuery<GhostComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnGhostStartup);
            SubscribeLocalEvent<GhostComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<GhostComponent, ComponentShutdown>(OnGhostShutdown);

            SubscribeLocalEvent<GhostComponent, ExaminedEvent>(OnGhostExamine);

            SubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            SubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<GhostOnMoveComponent, MoveInputEvent>(OnRelayMoveInput);

            SubscribeNetworkEvent<GhostWarpsRequestEvent>(OnGhostWarpsRequest);
            SubscribeNetworkEvent<GhostReturnToBodyRequest>(OnGhostReturnToBodyRequest);
            SubscribeNetworkEvent<GhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);
            SubscribeNetworkEvent<GhostnadoRequestEvent>(OnGhostnadoRequest);

            SubscribeLocalEvent<GhostComponent, BooActionEvent>(OnActionPerform);
            SubscribeLocalEvent<GhostComponent, ToggleGhostHearingActionEvent>(OnGhostHearingAction);
            SubscribeLocalEvent<GhostComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);
            SubscribeLocalEvent<GhostComponent, PriceCalculationEvent>(OnPriceCalculation); // Frontier

            SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => MakeVisible(true));
            SubscribeLocalEvent<ToggleGhostVisibilityToAllEvent>(OnToggleGhostVisibilityToAll);

            SubscribeLocalEvent<GhostComponent, MindAddedMessage>(OnMindAdded);

            SubscribeLocalEvent<GhostComponent, GetVisMaskEvent>(OnGhostVis);
        }

        private void OnGhostVis(Entity<GhostComponent> ent, ref GetVisMaskEvent args)
        {
            // If component not deleting they can see ghosts.
            if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            {
                args.VisibilityMask |= (int)VisibilityFlags.Ghost;
            }
        }

        private void OnGhostHearingAction(EntityUid uid, GhostComponent component, ToggleGhostHearingActionEvent args)
        {
            args.Handled = true;

            if (HasComp<GhostHearingComponent>(uid))
            {
                RemComp<GhostHearingComponent>(uid);
                _actions.SetToggled(component.ToggleGhostHearingActionEntity, true);
            }
            else
            {
                AddComp<GhostHearingComponent>(uid);
                _actions.SetToggled(component.ToggleGhostHearingActionEntity, false);
            }

            var str = HasComp<GhostHearingComponent>(uid)
                ? Loc.GetString("ghost-gui-toggle-hearing-popup-on")
                : Loc.GetString("ghost-gui-toggle-hearing-popup-off");

            Popup.PopupEntity(str, uid, uid);
            Dirty(uid, component);
        }

        private void OnActionPerform(EntityUid uid, GhostComponent component, BooActionEvent args)
        {
            if (args.Handled)
                return;

            var entities = _lookup.GetEntitiesInRange(args.Performer, component.BooRadius).ToList();
            // Shuffle the possible targets so we don't favor any particular entities
            _random.Shuffle(entities);

            var booCounter = 0;
            foreach (var ent in entities)
            {
                var handled = DoGhostBooEvent(ent);

                if (handled)
                    booCounter++;

                if (booCounter >= component.BooMaxTargets)
                    break;
            }

            if (booCounter == 0)
                _popup.PopupEntity(Loc.GetString("ghost-component-boo-action-failed"), uid, uid);

            args.Handled = true;
        }

        private void OnRelayMoveInput(EntityUid uid, GhostOnMoveComponent component, ref MoveInputEvent args)
        {
            // If they haven't actually moved then ignore it.
            if ((args.Entity.Comp.HeldMoveButtons &
                 (MoveButtons.Down | MoveButtons.Left | MoveButtons.Up | MoveButtons.Right)) == 0x0)
            {
                return;
            }

            // Let's not ghost if our mind is visiting...
            if (HasComp<VisitingMindComponent>(uid))
                return;

            if (!_minds.TryGetMind(uid, out var mindId, out var mind) || mind.IsVisitingEntity)
                return;

            if (component.MustBeDead && (_mobState.IsAlive(uid) || _mobState.IsCritical(uid)))
                return;

            OnGhostAttempt(mindId, component.CanReturn, mind: mind);
        }

        private void OnGhostStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            // Allow this entity to be seen by other ghosts.
            var visibility = EnsureComp<VisibilityComponent>(uid);

            if (_gameTicker.RunLevel != GameRunLevel.PostRound)
            {
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            _eye.RefreshVisibilityMask(uid);
            var time = _gameTiming.CurTime;
            component.TimeOfDeath = time;
            Dirty(uid, component); // Frontier
        }

        private void OnGhostShutdown(EntityUid uid, GhostComponent component, ComponentShutdown args)
        {
            // Perf: If the entity is deleting itself, no reason to change these back.
            if (Terminating(uid))
                return;

            // Entity can't be seen by ghosts anymore.
            if (TryComp(uid, out VisibilityComponent? visibility))
            {
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            // Entity can't see ghosts anymore.
            _eye.RefreshVisibilityMask(uid);
            _actions.RemoveAction(uid, component.BooActionEntity);
        }

        private void OnMapInit(EntityUid uid, GhostComponent component, MapInitEvent args)
        {
            _actions.AddAction(uid, ref component.BooActionEntity, component.BooAction);
            _actions.AddAction(uid, ref component.ToggleGhostHearingActionEntity, component.ToggleGhostHearingAction);
            _actions.AddAction(uid, ref component.ToggleLightingActionEntity, component.ToggleLightingAction);
            _actions.AddAction(uid, ref component.ToggleFoVActionEntity, component.ToggleFoVAction);
            _actions.AddAction(uid, ref component.ToggleGhostsActionEntity, component.ToggleGhostsAction);
        }

        private void OnGhostExamine(EntityUid uid, GhostComponent component, ExaminedEvent args)
        {
            var timeSinceDeath = _gameTiming.RealTime.Subtract(component.TimeOfDeath);
            var deathTimeInfo = timeSinceDeath.Minutes > 0
                ? Loc.GetString("comp-ghost-examine-time-minutes", ("minutes", timeSinceDeath.Minutes))
                : Loc.GetString("comp-ghost-examine-time-seconds", ("seconds", timeSinceDeath.Seconds));

            args.PushMarkup(deathTimeInfo);
        }

        #region Ghost Deletion

        private void OnMindRemovedMessage(EntityUid uid, GhostComponent component, MindRemovedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnMindUnvisitedMessage(EntityUid uid, GhostComponent component, MindUnvisitedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnPlayerDetached(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            DeleteEntity(uid);
        }

        private void DeleteEntity(EntityUid uid)
        {
            if (Deleted(uid) || Terminating(uid))
                return;

            QueueDel(uid);
        }

        #endregion

        private void OnGhostReturnToBodyRequest(GhostReturnToBodyRequest msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached
                || !_ghostQuery.TryComp(attached, out var ghost)
                || !ghost.CanReturnToBody
                || !TryComp(attached, out ActorComponent? actor))
            {
                Log.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(GhostReturnToBodyRequest)}");
                return;
            }

            _mind.UnVisit(actor.PlayerSession);
        }

        #region Warp

        private void OnGhostWarpsRequest(GhostWarpsRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} entity
                || !_ghostQuery.HasComp(entity))
            {
                Log.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");
                return;
            }

            // Frontier: get admin status for entity.
            bool isAdmin = _admin.IsAdmin(entity);

            // Only include admin ghosts if the requester is an admin
            var warps = GetPlayerWarps(entity)
                .Concat(GetLocationWarps(isAdmin));

            if (isAdmin)
            {
                // Add admin ghosts and regular ghosts to the warp list for admin users
                warps = warps.Concat(GetAdminGhostWarps(entity))
                            .Concat(GetRegularGhostWarps(entity));
            }

            var response = new GhostWarpsResponseEvent(warps.ToList());
            RaiseNetworkEvent(response, args.SenderSession.Channel);
        }

        private void OnGhostWarpToTargetRequest(GhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached
                || !_ghostQuery.HasComp(attached))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Target} without being a ghost.");
                return;
            }

            var target = GetEntity(msg.Target);

            if (!Exists(target))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to an invalid entity id: {msg.Target}");
                return;
            }

            // Frontier: check admin status when warping to admin-only warp points
            if (!_admin.IsAdmin(attached) &&
                TryComp<WarpPointComponent>(target, out var warpPoint) &&
                warpPoint.AdminOnly)
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to an admin-only warp point: {msg.Target}");
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{EntityManager.ToPrettyString(attached):player} tried to warp to admin warp point {EntityManager.ToPrettyString(msg.Target)}");
                return;
            }
            // End Frontier

            WarpTo(attached, target);
        }

        private void OnGhostnadoRequest(GhostnadoRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {} uid
                || !_ghostQuery.HasComp(uid))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to ghostnado without being a ghost.");
                return;
            }

            if (_followerSystem.GetMostGhostFollowed() is not {} target)
                return;

            WarpTo(uid, target);
        }

        private void WarpTo(EntityUid uid, EntityUid target)
        {
            _adminLog.Add(LogType.GhostWarp, $"{ToPrettyString(uid)} ghost warped to {ToPrettyString(target)}");

            if ((TryComp(target, out WarpPointComponent? warp) && warp.Follow) || HasComp<MobStateComponent>(target) || HasComp<GhostComponent>(target))
            {
                _followerSystem.StartFollowingEntity(uid, target);
                return;
            }

            var xform = Transform(uid);
            _transformSystem.SetCoordinates(uid, xform, Transform(target).Coordinates);
            _transformSystem.AttachToGridOrMap(uid, xform);
            if (_physicsQuery.TryComp(uid, out var physics))
                _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        }

        private IEnumerable<GhostWarp> GetLocationWarps(bool isAdmin) // Frontier: add isAdmin
        {
            var allQuery = AllEntityQuery<WarpPointComponent>();

            while (allQuery.MoveNext(out var uid, out var warp))
            {
                if (warp.AdminOnly && !isAdmin) // Frontier: skip admin-only warp points if not an admin
                    continue; // Frontier

                yield return new GhostWarp(GetNetEntity(uid), warp.Location ?? Name(uid), true);
            }
        }

        private IEnumerable<GhostWarp> GetPlayerWarps(EntityUid except)
        {
            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} attached)
                    continue;

                if (attached == except) continue;

                TryComp<MindContainerComponent>(attached, out var mind);

                var jobName = _jobs.MindTryGetJobName(mind?.Mind);
                var playerInfo = $"{Comp<MetaDataComponent>(attached).EntityName} ({jobName})";

                if (_mobState.IsAlive(attached) || _mobState.IsCritical(attached))
                    yield return new GhostWarp(GetNetEntity(attached), playerInfo, false);
            }
        }

        private IEnumerable<GhostWarp> GetAdminGhostWarps(EntityUid except)
        {
            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} attached)
                    continue;

                if (attached == except) continue;

                // Skip if not a ghost or not an admin
                if (!_ghostQuery.HasComp(attached) || !_admin.IsAdmin(attached))
                    continue;

                TryComp<MindContainerComponent>(attached, out var mind);
                var jobName = _jobs.MindTryGetJobName(mind?.Mind);

                // Add "(Admin Ghost)" suffix to the display name
                var playerInfo = $"{Comp<MetaDataComponent>(attached).EntityName} (Admin Ghost)";

                yield return new GhostWarp(GetNetEntity(attached), playerInfo, false);
            }
        }

        private IEnumerable<GhostWarp> GetRegularGhostWarps(EntityUid except)
        {
            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} attached)
                    continue;

                if (attached == except) continue;

                // Skip if not a ghost or if it's an admin
                if (!_ghostQuery.HasComp(attached) || _admin.IsAdmin(attached))
                    continue;

                TryComp<MindContainerComponent>(attached, out var mind);
                var jobName = _jobs.MindTryGetJobName(mind?.Mind);

                // Add "(Ghost)" suffix to the display name
                var playerInfo = $"{Comp<MetaDataComponent>(attached).EntityName} (Ghost)";

                yield return new GhostWarp(GetNetEntity(attached), playerInfo, false);
            }
        }

        #endregion

        private void OnEntityStorageInsertAttempt(EntityUid uid, GhostComponent comp, ref InsertIntoEntityStorageAttemptEvent args)
        {
            args.Cancelled = true;
        }

        private void OnToggleGhostVisibilityToAll(ToggleGhostVisibilityToAllEvent ev)
        {
            if (ev.Handled)
                return;

            ev.Handled = true;
            MakeVisible(true);
        }

        /// <summary>
        /// When the round ends, make all players able to see ghosts.
        /// </summary>
        public void MakeVisible(bool visible)
        {
            var entityQuery = EntityQueryEnumerator<GhostComponent, VisibilityComponent>();
            while (entityQuery.MoveNext(out var uid, out var _, out var vis))
            {
                if (!_tag.HasTag(uid, "AllowGhostShownByEvent"))
                    continue;

                if (visible)
                {
                    _visibilitySystem.AddLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                    _visibilitySystem.RemoveLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                }
                else
                {
                    _visibilitySystem.AddLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                    _visibilitySystem.RemoveLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                }
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: vis);
            }
        }

        public bool DoGhostBooEvent(EntityUid target)
        {
            var ghostBoo = new GhostBooEvent();
            RaiseLocalEvent(target, ghostBoo, true);

            return ghostBoo.Handled;
        }

        public EntityUid? SpawnGhost(Entity<MindComponent?> mind, EntityUid targetEntity,
            bool canReturn = false)
        {
            _transformSystem.TryGetMapOrGridCoordinates(targetEntity, out var spawnPosition);
            return SpawnGhost(mind, spawnPosition, canReturn);
        }

        private bool IsValidSpawnPosition(EntityCoordinates? spawnPosition)
        {
            if (spawnPosition?.IsValid(EntityManager) != true)
                return false;

            var mapUid = spawnPosition?.GetMapUid(EntityManager);
            var gridUid = spawnPosition?.EntityId;
            // Test if the map is being deleted
            if (mapUid == null || TerminatingOrDeleted(mapUid.Value))
                return false;
            // Test if the grid is being deleted
            if (gridUid != null && TerminatingOrDeleted(gridUid.Value))
                return false;

            return true;
        }

        public EntityUid? SpawnGhost(Entity<MindComponent?> mind, EntityCoordinates? spawnPosition = null,
            bool canReturn = false)
        {
            if (!Resolve(mind, ref mind.Comp))
                return null;

            // Test if the map or grid is being deleted
            if (!IsValidSpawnPosition(spawnPosition))
                spawnPosition = null;

            // If it's bad, look for a valid point to spawn
            spawnPosition ??= _gameTicker.GetObserverSpawnPoint();

            // Make sure the new point is valid too
            if (!IsValidSpawnPosition(spawnPosition))
            {
                Log.Warning($"No spawn valid ghost spawn position found for {mind.Comp.CharacterName}"
                    + $" \"{ToPrettyString(mind)}\"");
                _minds.TransferTo(mind.Owner, null, createGhost: false, mind: mind.Comp);
                return null;
            }

            var ghost = SpawnAtPosition(GameTicker.ObserverPrototypeName, spawnPosition.Value);
            var ghostComponent = Comp<GhostComponent>(ghost);

            // Try setting the ghost entity name to either the character name or the player name.
            // If all else fails, it'll default to the default entity prototype name, "observer".
            // However, that should rarely happen.
            if (!string.IsNullOrWhiteSpace(mind.Comp.CharacterName))
                _metaData.SetEntityName(ghost, mind.Comp.CharacterName);
            else if (!string.IsNullOrWhiteSpace(mind.Comp.Session?.Name))
                _metaData.SetEntityName(ghost, mind.Comp.Session.Name);

            if (mind.Comp.TimeOfDeath.HasValue)
            {
                SetTimeOfDeath(ghost, mind.Comp.TimeOfDeath!.Value, ghostComponent);
            }

            SetCanReturnToBody(ghostComponent, canReturn);

            if (canReturn)
                _minds.Visit(mind.Owner, ghost, mind.Comp);
            else
                _minds.TransferTo(mind.Owner, ghost, mind: mind.Comp);
            Log.Debug($"Spawned ghost \"{ToPrettyString(ghost)}\" for {mind.Comp.CharacterName}.");

            // Apply admin OOC color to the ghost if the player has one
            ApplyAdminOOCColor(ghost, mind.Owner);

            return ghost;
        }

        /// <summary>
        /// Applies the admin OOC color to a ghost entity if the player has one set
        /// </summary>
        /// <param name="ghostEntity">The ghost entity to apply the color to</param>
        /// <param name="mindId">The mind ID of the player</param>
        public void ApplyAdminOOCColor(EntityUid ghostEntity, EntityUid mindId) // Mono
        {
            if (!_mind.TryGetSession(mindId, out var session))
                return;

            // Only apply admin OOC color if the player is actually an admin
            if (!_admin.IsAdmin(session))
                return;

            if (!_preferencesManager.TryGetCachedPreferences(session.UserId, out var prefs))
                return;

            // Only apply the color if it's not transparent (the default)
            if (prefs.AdminOOCColor == Color.Transparent)
                return;

            // Make the color slightly transparent for ghosts
            var ghostColor = prefs.AdminOOCColor;

            if (TryComp<GhostComponent>(ghostEntity, out var ghostComp))
            {
                ghostComp.Color = ghostColor;
                Dirty(ghostEntity, ghostComp);
            }
        }

        private void OnMindAdded(EntityUid uid, GhostComponent component, MindAddedMessage args)
        {
            // When a mind is added to a ghost, check if the player has an admin OOC color
            // and apply it to the ghost if they do
            if (args.Mind == default)
                return;

            ApplyAdminOOCColor(uid, args.Mind);
        }

        public bool OnGhostAttempt(EntityUid mindId, bool canReturnGlobal, bool viaCommand = false, bool forced = false, MindComponent? mind = null)
        {
            if (!Resolve(mindId, ref mind))
                return false;

            var playerEntity = mind.CurrentEntity;

            if (playerEntity != null && viaCommand)
            {
                if (forced)
                    _adminLog.Add(LogType.Mind, $"{EntityManager.ToPrettyString(playerEntity.Value):player} was forced to ghost via command");
                else
                    _adminLog.Add(LogType.Mind, $"{EntityManager.ToPrettyString(playerEntity.Value):player} is attempting to ghost via command");
            }

            var handleEv = new GhostAttemptHandleEvent(mind, canReturnGlobal);
            RaiseLocalEvent(handleEv);

            // Something else has handled the ghost attempt for us! We return its result.
            if (handleEv.Handled)
                return handleEv.Result;

            if (mind.PreventGhosting && !forced)
            {
                if (mind.Session != null) // Logging is suppressed to prevent spam from ghost attempts caused by movement attempts
                {
                    _chatManager.DispatchServerMessage(mind.Session, Loc.GetString("comp-mind-ghosting-prevented"),
                        true);
                }

                return false;
            }

            if (TryComp<GhostComponent>(playerEntity, out var comp) && !comp.CanGhostInteract)
                return false;

            if (mind.VisitingEntity != default)
            {
                _mind.UnVisit(mindId, mind: mind);
            }

            var position = Exists(playerEntity)
                ? Transform(playerEntity.Value).Coordinates
                : _gameTicker.GetObserverSpawnPoint();

            if (position == default)
                return false;

            // Ok, so, this is the master place for the logic for if ghosting is "too cheaty" to allow returning.
            // There's no reason at this time to move it to any other place, especially given that the 'side effects required' situations would also have to be moved.
            // + If CharacterDeadPhysically applies, we're physically dead. Therefore, ghosting OK, and we can return (this is critical for gibbing)
            //   Note that we could theoretically be ICly dead and still physically alive and vice versa.
            //   (For example, a zombie could be dead ICly, but may retain memories and is definitely physically active)
            // + If we're in a mob that is critical, and we're supposed to be able to return if possible,
            //   we're succumbing - the mob is killed. Therefore, character is dead. Ghosting OK.
            //   (If the mob survives, that's a bug. Ghosting is kept regardless.)
            var canReturn = canReturnGlobal && _mind.IsCharacterDeadPhysically(mind);

            if (_configurationManager.GetCVar(CCVars.GhostKillCrit) &&
                canReturnGlobal &&
                TryComp(playerEntity, out MobStateComponent? mobState))
            {
                if (_mobState.IsCritical(playerEntity.Value, mobState))
                {
                    canReturn = true;

                    //todo: what if they dont breathe lol
                    //cry deeply

                    FixedPoint2 dealtDamage = 200;

                    if (TryComp<DamageableComponent>(playerEntity, out var damageable)
                        && TryComp<MobThresholdsComponent>(playerEntity, out var thresholds))
                    {
                        var playerDeadThreshold = _mobThresholdSystem.GetThresholdForState(playerEntity.Value, MobState.Dead, thresholds);
                        dealtDamage = playerDeadThreshold - damageable.TotalDamage;
                    }

                    DamageSpecifier damage = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), dealtDamage);

                    _damageable.TryChangeDamage(playerEntity, damage, true);
                }
            }

            if (playerEntity != null)
                _adminLog.Add(LogType.Mind, $"{EntityManager.ToPrettyString(playerEntity.Value):player} ghosted{(!canReturn ? " (non-returnable)" : "")}");

            var ghost = SpawnGhost((mindId, mind), position, canReturn);

            if (ghost == null)
                return false;

            return true;
        }

        // Frontier: worthless ghosts
        private void OnPriceCalculation(Entity<GhostComponent> ent, ref PriceCalculationEvent args)
        {
            args.Price = 0;
            args.Handled = true;
        }
        // End Frontier
    }

    public sealed class GhostAttemptHandleEvent(MindComponent mind, bool canReturnGlobal) : HandledEntityEventArgs
    {
        public MindComponent Mind { get; } = mind;
        public bool CanReturnGlobal { get; } = canReturnGlobal;
        public bool Result { get; set; }
    }
}

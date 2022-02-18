﻿using System;

namespace BossMod.P4S
{
    using static BossModule;

    // state related to curtain call mechanic
    // TODO: unhardcode relative order in pairs, currently tanks/healers pop first...
    class CurtainCall : Component
    {
        private P4S _module;
        private int[] _playerOrder = new int[8];
        private int _numCasts = 0;

        public CurtainCall(P4S module)
        {
            _module = module;
        }

        public override void AddHints(int slot, WorldState.Actor actor, TextHints hints, MovementHints? movementHints)
        {
            if (_playerOrder[slot] > _numCasts)
            {
                var relOrder = _playerOrder[slot] - _numCasts;
                hints.Add($"Tether break order: {relOrder}", relOrder == 1);
            }
        }

        public override void DrawArenaForeground(MiniArena arena)
        {
            var pc = _module.Player();
            if (_playerOrder[_module.PlayerSlot] > _numCasts && pc != null)
            {
                var tetherTarget = pc.Tether.Target != 0 ? _module.WorldState.FindActor(pc.Tether.Target) : null;
                if (tetherTarget != null)
                    arena.AddLine(pc.Position, tetherTarget.Position, pc.Tether.ID == (uint)TetherID.WreathOfThorns ? arena.ColorDanger : arena.ColorSafe);
            }
        }

        public override void OnStatusGain(WorldState.Actor actor, int index)
        {
            if (actor.Statuses[index].ID == (uint)SID.Thornpricked)
            {
                int slot = _module.RaidMembers.FindSlot(actor.InstanceID);
                if (slot >= 0)
                {
                    _playerOrder[slot] = 2 * (int)((actor.Statuses[index].ExpireAt - _module.WorldState.CurrentTime).TotalSeconds / 10); // 2/4/6/8
                    if (actor.Role == Role.Tank || actor.Role == Role.Healer)
                        --_playerOrder[slot];
                }
            }
        }

        public override void OnStatusLose(WorldState.Actor actor, int index)
        {
            if (actor.Statuses[index].ID == (uint)SID.Thornpricked)
                ++_numCasts;
        }
    }
}

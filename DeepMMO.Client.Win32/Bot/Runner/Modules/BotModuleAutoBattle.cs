using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Voxel;
using DeepCore.Reflection;

namespace DeepMMO.Client.BotTest.Runner.Modules
{
    public class BotModuleAutoBattle : BotModule
    {
        private Random random = new Random();

        public BotModuleAutoBattle(BotRunner r) : base(r)
        {
            base.Client.OnZoneActorEntered += Client_OnZoneActorEntered;
        }

        private void Client_OnZoneActorEntered(LayerPlayer obj)
        {
            obj.SendUnitGuard(base.IsEnable);
            obj.Parent.AddTimePeriodicMS(Config.RandomMoveIntervalMS, OnMoveTick);
        }
        protected override void OnEnableChanged(bool enable)
        {
            var obj = Client.CurrentZoneActor;
            if (obj != null)
            {
                obj.SendUnitGuard(base.IsEnable);
            }
        }
        protected override void OnUpdate(int intervalMS)
        {
            base.OnUpdate(intervalMS);
        }
        private void OnMoveTick(TimeTaskMS tick)
        {
            var obj = Client.CurrentZoneActor;
            if (obj != null)
            {
                obj.SendUnitGuard(base.IsEnable);
                if (base.IsEnable)
                {
                    if (obj.Parent.Terrain3D is VoxelClientTerrain3D vt)
                    {
                        var pos = obj.Position;
                        if (vt.World.Terrain.TryGetVoxelLayerByObject(ref pos, out var cell, out var layer))
                        {
                            int size = Math.Max(1, (int)(Config.RandomMoveDistance / vt.World.Terrain.GridCellSize));
                            var tp = vt.World.FindNearRandomMoveableNode(random, layer, size);
                            //if (pos != null)
                            {
                                //var pos = Terrain.GetUpwardCenterPos(tp);//layer.UpwardCenterPos
                                obj.SendUnitAttackMoveTo(tp.UpwardCenterPos, false);
                            }
                        }
                    }
                }
            }
        }
        [Desc("自动战斗配置")]
        [Expandable]
        public class Config : BotModuleConfig
        {
            [Desc("自动随机移动时间间隔")]
            public static int RandomMoveIntervalMS = 60000;

            [Desc("自动随机移动距离")]
            public static float RandomMoveDistance = 100;
        }

    }
}

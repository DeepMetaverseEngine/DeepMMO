using DeepCore.Game3D.Slave.Agent;
using DeepCore.Game3D.Slave;

namespace DeepMMO.Unity3D.Terrain
{
    public abstract class AbstractMoveAgent : AbstractAgent
    {
        public abstract ILayerWayPoint WayPoints { get; }
        public abstract bool IsFinish { get; }

    }
}

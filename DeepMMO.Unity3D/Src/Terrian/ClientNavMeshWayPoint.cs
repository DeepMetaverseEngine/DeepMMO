using System;
using System.Collections.Generic;
using DeepCore.Astar;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Voxel;
using DeepCore.GameData.Data;
using DeepCore.GameData.Zone;
using DeepCore.IO;
using DeepCore.Unity3D;
using UnityEngine;
using ILayerWayPoint = DeepCore.Game3D.Slave.ILayerWayPoint;
using Vector2 = DeepCore.Geometry.Vector2;
using Vector3 = DeepCore.Geometry.Vector3;

namespace DeepMMO.Unity3D.Terrain
{

    public class NavMeshClientWayPoint : ILayerWayPoint
    {
        private NavMeshWayPoint p;

        public NavMeshClientWayPoint(NavMeshWayPoint p)
        {
            this.p = p;
        }

        public void LinkNext(NavMeshClientWayPoint next)
        {
            this.Next = next;
            next.Prev = this;
//                (Tail as NavMeshClientWayPoint).Next = next;
//                next.Prev = (Tail as NavMeshClientWayPoint);
        }

        public static NavMeshClientWayPoint CreateFromVoxel(NavMeshWayPoint p)
        {
            if (p == null)
            {
                return null;
            }

            var start = new NavMeshClientWayPoint(p);
            var wp = start;
            while (p.Next != null)
            {
                p = p.Next;
                var next = new NavMeshClientWayPoint(p);
                wp.Next = next;
                next.Prev = wp;
                wp = next;
            }

            return start;
        }

        public ILayerWayPoint Next { get; private set; }
        public ILayerWayPoint Prev { get; private set; }

        public ILayerWayPoint Tail
        {
            get
            {
                var wp = this as ILayerWayPoint;
                while (wp.Next != null)
                {
                    wp = wp.Next;
                }

                return wp;
            }
        }

        public float X
        {
            get => p.X;
        }

        public float Y
        {
            get => p.Y;
        }

        public float Z
        {
            get => p.Z;
        }

        public Vector3 Position
        {
            get => p.Position;
        }

        public bool PosEquals(ILayerWayPoint w)
        {
            return p.PosEquals((w as NavMeshClientWayPoint).p);
        }

        public float GetTotalDistance()
        {
            return p.GetTotalDistance();
        }

        //--------------------------------------------------------------------------------------------------------
    }
    public class NavMeshWayPoint : IWayPoint<ClientMapNode, NavMeshWayPoint>
    {
        public float X { get => Position.X; }
        public float Y { get => Position.Y; }
        public float Z { get => Position.Z; }
        public Vector3 Position { get; set; }
        internal NavMeshWayPoint(ClientMapNode mapNode) : base(mapNode)
        {
            this.Position = base.Node.Position;
        }
        public override bool PosEquals(NavMeshWayPoint w)
        {
            return this.Position == w.Position;
        }
        public virtual float GetTotalDistance()
        {
            float ret = 0;
            var cur = this;
            while (cur != null)
            {
                var nex = cur.Next;
                if (cur != null && nex != null)
                {
                    ret += Vector3.Distance(cur.Position, nex.Position);
                }
                cur = nex;
            }
            return ret;
        }
  

    }
}
using System.Collections.Generic;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    public static class SplitUtils
    {
        public static int GetSplitHashCode(int x, int y, int z)
        {
            unchecked
            {
                var hashCode = x * 1000000000;
                hashCode += y * 100000;
                hashCode += z;
                return hashCode;
            }
        }

        public static Vector3Int GetID(in Vector3 position, in Vector3 splitSize)
        {
            var xId = 0;
            var yId = 0;
            var zId = 0;

            if (splitSize.x > 0)
            {
                xId = Mathf.FloorToInt(position.x / splitSize.x);

                if (Mathf.Abs((position.x / splitSize.x) - Mathf.RoundToInt(position.x / splitSize.x)) < 0.001f)
                {
                    xId = Mathf.RoundToInt(position.x / splitSize.x);
                }
            }

            if (splitSize.y > 0)
            {
                yId = Mathf.FloorToInt(position.y / splitSize.y);
                if (Mathf.Abs((position.y / splitSize.y) - Mathf.RoundToInt(position.y / splitSize.y)) < 0.001f)
                {
                    yId = Mathf.RoundToInt(position.y / splitSize.y);
                }
            }

            if (splitSize.z > 0)
            {
                zId = Mathf.FloorToInt(position.z / splitSize.z);

                if (Mathf.Abs((position.z / splitSize.z) - Mathf.RoundToInt(position.z / splitSize.z)) < 0.001f)
                {
                    zId = Mathf.RoundToInt(position.z / splitSize.z);
                }
            }

            return new Vector3Int(xId, yId, zId);
        }

        public static int[] GetID(in Vector3 splitSize, in Bounds worldBounds)
        {
            var allIds = new HashSet<Vector3Int>();
            var idMin = GetID(worldBounds.min, in splitSize);
            var idMax = GetID(worldBounds.max, in splitSize);
            var idCenter = GetID(worldBounds.center, in splitSize);

            allIds.Add(new Vector3Int(idCenter.x, idCenter.y, idCenter.z));

            for (var x = idMin[0]; x <= idMax[0]; x++)
            {
                for (var y = idMin[1]; y <= idMax[1]; y++)
                {
                    for (var z = idMin[2]; z <= idMax[2]; z++)
                    {
                        allIds.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            var retArr = new int[allIds.Count * 3];
            var index = 0;
            foreach (var posId in allIds)
            {
                retArr[index++] = posId[0];
                retArr[index++] = posId[1];
                retArr[index++] = posId[2];
            }

            return retArr;
        }

        public static int[] GetID(GameObject obj, in Vector3 splitSize, float scaleBounds = 1f)
        {
            // if (obj.name == "terrain_public_zhaogecheng")
            // {
            // }
            // Debug.Log($"TryGuessWorldBounds {obj.name}", obj);
            var worldBounds = Utils.TryGuessWorldBounds(obj, Utils.GuessOpt.IncludeChildren);
            //0.8倍精度
            worldBounds.size *= scaleBounds;
            return GetID(in splitSize, in worldBounds);
        }
    }
}
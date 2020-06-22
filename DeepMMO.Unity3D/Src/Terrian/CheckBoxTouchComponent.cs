using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepMMO.Unity3D.Terrain
{
    public class CheckBoxTouchComponent
    {
        private bool ground;
        private int laymask;
        public System.Action<string, GameObject> mAction;
        public Vector3 currentUnityPos;
        private float StepIntercept;
        private string[] LayerName = 
        {
            "NavLayer",
            "Default",
        };
        public CheckBoxTouchComponent(float stepIntercept)
        {
            laymask = CreateLayerMask(LayerName);
            StepIntercept = stepIntercept;
        }

        protected virtual int CreateLayerMask(string[] layerName)
        {
            return LayerMask.GetMask(layerName);
        }

        


        private Vector3 pointbottom;
        private Vector3 pointtop;
        private Vector3 pointhalf;
        public float radius { get; set; }
        public float height { get; set; }

        public float bodySize = 0.5f;

        public Tuple<bool, Vector3> IsBottomHit(Vector3 pos)
        {
            pointbottom = pos;

            Vector3 bottompos = pos;

            float dis = 100;
            Vector3 hitpos = pos;
            hitpos.y = 0;
            var bottomhit = RayHit(pos + Vector3.up * height / 2, Vector3.down, 10);
            bool isBottomhit = bottomhit.Item1;
            if (isBottomhit)
            {
                hitpos = bottomhit.Item2.point;
                dis = Mathf.Abs(pointbottom.y - hitpos.y);
                isBottomhit = dis <= 0.02f || pointbottom.y < (hitpos.y);
                bottompos = hitpos;
            }
            
            return new Tuple<bool, Vector3>(isBottomhit, bottompos);
        }

        //Bounds
        public Tuple<bool, Vector3> IsInWater(Vector3 pos,float WaterDistance)
        {
            pointtop = pos + Vector3.up * height/2;
            Physics.queriesHitBackfaces = true;
            var tophit = RayHit(pointtop, Vector3.up, 2,"Water");
            Physics.queriesHitBackfaces = false;
            var isInwater = tophit.Item1;
            if (!isInwater)
            {
                tophit = RayHit(pointtop, Vector3.down, height/2 + 0.2f,"Water");
                if (tophit.Item1)
                {
                    isInwater = true;
                }
            }
            if (isInwater)
            {
                var downhit = RayHit(pointtop, Vector3.down, 5, "NavLayer");//Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 3, LayerMask.GetMask("NavLayer"));
                if (!downhit.Item1 || (downhit.Item1 && Mathf.Abs(tophit.Item2.point.y - downhit.Item2.point.y) >= WaterDistance))
                {
                    return new Tuple<bool, Vector3>(true, tophit.Item2.point);
                }
            }
            return new Tuple<bool, Vector3>(false,pos);
        }

        public Tuple<bool, Vector3> IsTopHit(Vector3 pos)
        {
            pointtop = pos + Vector3.up * (height);
            Physics.queriesHitBackfaces = true;
            var tophit = RayHit(pointtop + Vector3.down * (height / 2), Vector3.up, 10);
            Physics.queriesHitBackfaces = false;
            bool isTophit = tophit.Item1;
            if (isTophit)
            {
                var dis = Mathf.Abs(pointtop.y - (tophit.Item2.point.y - StepIntercept / 2));
                //Debug.Log("top=====dis "+dis+" pointtop="+pointtop.y+" toppos="+(tophit.Item2.point.y - StepIntercept/2));
                isTophit = dis <= 0.2f || pointtop.y >= tophit.Item2.point.y - StepIntercept / 2; //头部超过天花板
                if (tophit.Item2.transform.gameObject.layer == LayerMask.NameToLayer("Water"))
                {
                    isTophit = false;
                }
            }

            return new Tuple<bool, Vector3>(isTophit, tophit.Item2.point);
        }

        public Tuple<bool, Vector3> IsSideHit(Vector3 startpos, Vector3 pos)
        {
            var startpointhalf = startpos + Vector3.up * height / 2;
            pointhalf = pos + Vector3.up * height / 2;
            var touchpos = startpos;
            var dir = (pointhalf - startpointhalf).normalized;
            var distance = UnityEngine.Vector3.Distance(pointhalf, startpos);
            var iscollider = RayHit(startpointhalf, dir, distance - bodySize/2);
            if (iscollider.Item1)
            {
                // if (iscollider.Item2.transform.gameObject.layer == LayerMask.NameToLayer("Water"))
                // {
                //     return new Tuple<bool, Vector3>(false,pos);
                // }
                
                return new Tuple<bool, Vector3>(true, startpos);
            }

            return new Tuple<bool, Vector3>(false, pos);
        }


        public static HashSet<Transform> IgnoreVoxel = new HashSet<Transform>();

        public Tuple<bool, RaycastHit> RayHit(Vector3 pos, Vector3 dir, float dis,string layerName = "")
        {
            var layer = string.IsNullOrEmpty(layerName)?laymask:1<<LayerMask.NameToLayer(layerName);
            var isTouch = Physics.Raycast(pos, dir, out RaycastHit hit, dis, layer);
            if (hit.transform && IgnoreVoxel.Contains(hit.transform))
            {
                return new Tuple<bool, RaycastHit>(false, default);
            }

            return new Tuple<bool, RaycastHit>(isTouch, hit);
        }
        
      
        public List<RaycastHit> RaycastHitAll(Vector3 pos, Vector3 dir, float dis)
        {
            var isTouch = Physics.RaycastAll(pos, dir, dis, laymask);
            var list = new List<RaycastHit>();
            foreach (var touch in isTouch)
            {
                if (touch.transform && !IgnoreVoxel.Contains(touch.transform))
                {
                    list.Add(touch);
                }
            }
            return list;
        }

        private GameObject GetMinDistance(Collider[] hits)
        {
            if (hits == null || hits.Length == 0)
            {
                return null;
            }

            var distance = 999;
            GameObject _object = null;
            foreach (var hit in hits)
            {
                var _dis = Vector3.Distance(hit.transform.position, currentUnityPos);
                if (_dis < distance)
                {
                    _object = hit.gameObject;
                }
            }

            return _object;
        }
        //    public void Update()
        //    {
        //        
        //        var bottomhit = Physics.OverlapSphere(pointbottom, 0.3f, laymask);
        //        var tophit = Physics.OverlapSphere(pointtop, 0.3f, laymask);
        //        var halfhit = Physics.OverlapBox(pointhalf, new Vector3(0.5f, 0.5f, 0.5f),Quaternion.identity, laymask);
        //        Debug.Log("hit==========" + bottomhit.Length);
        //        Debug.Log("tophit==========" + tophit.Length);
        //        Debug.Log("halfhit==========" + halfhit.Length);
        //    }


        private void OnDrawGizmos()
        {
            pointbottom = currentUnityPos;
            pointtop = currentUnityPos + Vector3.up * 1.8f;
            pointhalf = currentUnityPos + Vector3.up * 1.8f / 2;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pointbottom, 0.3f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(pointtop, 0.3f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(pointhalf, new Vector3(0.5f, 0.5f, 0.5f));
        }


        public void Dispose()
        {
            mAction = null;
        }

        public int GetLayerMask()
        {
            return laymask;
        }
    }
}
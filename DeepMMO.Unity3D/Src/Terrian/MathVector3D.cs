using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace DeepMMO.Unity3D.Terrain
{
    public static class MathVector3D
    {
        public static DeepCore.Geometry.Vector3 GetSimpleNumber(this DeepCore.Geometry.Vector3 o, int dec = 1)
        {
            return new DeepCore.Geometry.Vector3(ToSimpleNumber(o.X, dec), ToSimpleNumber(o.Y, dec),
                ToSimpleNumber(o.Z, dec));
        }

        public static Vector3 GetSimpleNumber(this Vector3 o, int dec = 1)
        {
            return new Vector3(ToSimpleNumber(o.x, dec), ToSimpleNumber(o.y, dec), ToSimpleNumber(o.z, dec));
        }

        public static Vector2 GetToUnitPosSimpleNumberVector2(this Vector3 o, int dec = 1)
        {
            //var var3 = o.GetSimpleNumber(dec);
            return new Vector2(o.x, o.z);
        }

        public static Vector2 GetToZonePosSimpleNumberVector2(this Vector3 o, float h, int dec = 1)
        {
            var var3 = o.GetSimpleNumber(dec);
            var zonevar3 = BattleUtils.UnityPos2ZonePos(h, var3);
            return new Vector2(zonevar3.X, zonevar3.Y);
        }

        public static float Get3DTo2DDistance(this DeepCore.Geometry.Vector3 src, DeepCore.Geometry.Vector3 dst)
        {
            var dis = Mathf.Sqrt((src.X - dst.X) * (src.X - dst.X) + (src.Y - dst.Y) * (src.Y - dst.Y));
            return dis;
        }

        public static float Get2DDistance(float srcx, float srcy, float dstx, float dsty)
        {
            var dis = Mathf.Sqrt((srcx - dstx) * (srcx - dstx) + (srcy - dsty) * (srcy - dsty));
            return dis;
        }

        public static float ToSimpleNumber(float number, int dec = 1)
        {
            int num = (int) (number * Math.Pow(10, dec));
            return (float) (num * Math.Pow(0.1f, dec));
        }

        public static void move(ref Vector2 v, float dx, float dy)
        {
            v.x += dx;
            v.y += dy;
        }

        public static void movePolar(ref Vector2 src, float degree, float distance)
        {
            float dx = (float) Math.Cos(degree) * distance;
            float dy = (float) Math.Sin(degree) * distance;
            move(ref src, dx, dy);
        }

        public static bool moveTo(ref Vector2 src, Vector2 dst, float distance)
        {
            float num1 = dst.x - src.x;
            float num2 = dst.y - src.y;
            if (Math.Abs(num1) < distance && Math.Abs(num2) < distance)
            {
                src.x = dst.x;
                src.y = dst.y;
                return true;
            }

            float degree = (float) Math.Atan2(num2, num1);
            movePolar(ref src, degree, distance);
            return false;
        }

        //    public static bool moveTo(ref Vector3 org, Vector3 target, float length)
        //    {
        //        var dis = Vector3.Distance(org, target);
        //        if (ToSimpleNumber(dis)<=ToSimpleNumber(length))
        //        {
        //            org = target;
        //            return true;
        //        }
        //
        //        var dir = target - org;
        //        var quater = Quaternion.LookRotation(dir);
        //        org = org +  quater * Vector3.forward * length;
        //        return false;
        //    }
        //    
        //    public static void move(IVector3 v, float dx, float dy)
        //    {
        //        v.X += dx;
        //        v.Y += dy;
        //    }
        //
        //    public static void movePolar(IVector3 v, float degree, float distance)
        //    {
        //        float dx = (float) Math.Cos((double) degree) * distance;
        //        float dy = (float) Math.Sin((double) degree) * distance;
        //        MathVector3D.move(v, dx, dy);
        //    }
        //
        //    public static void movePolar(ref float x, ref float y, float degree, float distance)
        //    {
        //        x += (float) Math.Cos((double) degree) * distance;
        //        y += (float) Math.Sin((double) degree) * distance;
        //    }
        //
        //    public static void movePolar(IVector2 v, float degree, float speed, float interval_ms)
        //    {
        //        float distanceSpeedTime = MathVector.getDistanceSpeedTime(speed, interval_ms);
        //        MathVector3D.movePolar(v, degree, distanceSpeedTime);
        //    }
        //
        //    public static void movePolar(
        //        ref float x,
        //        ref float y,
        //        float degree,
        //        float speed,
        //        float interval_ms)
        //    {
        //        float distanceSpeedTime = MathVector.getDistanceSpeedTime(speed, interval_ms);
        //        MathVector3D.movePolar(ref x, ref y, degree, distanceSpeedTime);
        //    }
        //
        //    public static bool moveTo(IVector2 v, float dx, float dy, float distance)
        //    {
        //        float num1 = dx - v.X;
        //        float num2 = dy - v.Y;
        //        if ((double) Math.Abs(num1) < (double) distance && (double) Math.Abs(num2) < (double) distance)
        //        {
        //            v.X = dx;
        //            v.Y = dy;
        //            return true;
        //        }
        //
        //        float degree = (float) Math.Atan2((double) num2, (double) num1);
        //        MathVector3D.movePolar(v, degree, distance);
        //        return false;
        //    }
        //
        //    public static bool moveTo(ref float x, ref float y, float dx, float dy, float distance)
        //    {
        //        float num1 = dx - x;
        //        float num2 = dy - y;
        //        if ((double) Math.Abs(num1) < (double) distance && (double) Math.Abs(num2) < (double) distance)
        //        {
        //            x = dx;
        //            y = dy;
        //            return true;
        //        }
        //
        //        float degree = (float) Math.Atan2((double) num2, (double) num1);
        //        MathVector3D.movePolar(ref x, ref y, degree, distance);
        //        return false;
        //    }
        //
        //    public static bool moveTo(IVector2 v, float dx, float dy, float distance, float angle_offset)
        //    {
        //        float num1 = dx - v.X;
        //        float num2 = dy - v.Y;
        //        if ((double) Math.Abs(num1) < (double) distance && (double) Math.Abs(num2) < (double) distance)
        //        {
        //            v.X = dx;
        //            v.Y = dy;
        //            return true;
        //        }
        //
        //        float degree = (float) Math.Atan2((double) num2, (double) num1) + angle_offset;
        //        MathVector3D.movePolar(v, degree, distance);
        //        return false;
        //    }
        //
        //    public static bool moveTo(
        //        ref float x,
        //        ref float y,
        //        float dx,
        //        float dy,
        //        float distance,
        //        float angle_offset)
        //    {
        //        float num1 = dx - x;
        //        float num2 = dy - y;
        //        if ((double) Math.Abs(num1) < (double) distance && (double) Math.Abs(num2) < (double) distance)
        //        {
        //            x = dx;
        //            y = dy;
        //            return true;
        //        }
        //
        //        float degree = (float) Math.Atan2((double) num2, (double) num1) + angle_offset;
        //        MathVector3D.movePolar(ref x, ref y, degree, distance);
        //        return false;
        //    }
        //
        //    public static bool moveToX(IVector2 v, float x, float distance)
        //    {
        //        float num = x - v.X;
        //        if ((double) Math.Abs(num) < (double) distance)
        //        {
        //            v.X = x;
        //            return true;
        //        }
        //
        //        if ((double) num > 0.0)
        //            v.X += distance;
        //        else
        //            v.X += -distance;
        //        return false;
        //    }
        //
        //    public static bool moveToY(IVector2 v, float y, float distance)
        //    {
        //        float num = y - v.Y;
        //        if ((double) Math.Abs(num) < (double) distance)
        //        {
        //            v.Y = y;
        //            return true;
        //        }
        //
        //        if ((double) num > 0.0)
        //            v.Y += distance;
        //        else
        //            v.Y += -distance;
        //        return false;
        //    }
        //
        //    public static void scale(IVector2 v, float scale)
        //    {
        //        v.X *= scale;
        //        v.Y *= scale;
        //    }
        //
        //    public static void scale(IVector2 v, float scale_x, float scale_y)
        //    {
        //        v.X *= scale_x;
        //        v.Y *= scale_y;
        //    }
        //
        //    public static void scale(ref float x, ref float y, float scale_x, float scale_y)
        //    {
        //        x *= scale_x;
        //        y *= scale_y;
        //    }
        //
        //    public static void rotate(IVector2 v, float degree)
        //    {
        //        float num1 = (float) Math.Cos((double) degree);
        //        float num2 = (float) Math.Sin((double) degree);
        //        float num3 = (float) ((double) v.X * (double) num1 - (double) v.Y * (double) num2);
        //        float num4 = (float) ((double) v.Y * (double) num1 + (double) v.X * (double) num2);
        //        v.X = num3;
        //        v.Y = num4;
        //    }
        //
        //    public static void rotate(IVector2 v, IVector2 p0, float degree)
        //    {
        //        float num1 = v.X - p0.X;
        //        float num2 = v.Y - p0.Y;
        //        float num3 = (float) Math.Cos((double) degree);
        //        float num4 = (float) Math.Sin((double) degree);
        //        float num5 = (float) ((double) p0.X + (double) num1 * (double) num3 - (double) num2 * (double) num4);
        //        float num6 = (float) ((double) p0.Y + (double) num2 * (double) num3 + (double) num1 * (double) num4);
        //        v.X = num5;
        //        v.Y = num6;
        //    }
        //
        //    public static void rotate(IVector2 v, float px, float py, float degree)
        //    {
        //        float num1 = v.X - px;
        //        float num2 = v.Y - py;
        //        float num3 = (float) Math.Cos((double) degree);
        //        float num4 = (float) Math.Sin((double) degree);
        //        float num5 = (float) ((double) px + (double) num1 * (double) num3 - (double) num2 * (double) num4);
        //        float num6 = (float) ((double) py + (double) num2 * (double) num3 + (double) num1 * (double) num4);
        //        v.X = num5;
        //        v.Y = num6;
        //    }
        //
        //    public static void rotate(ref float x, ref float y, float px, float py, float degree)
        //    {
        //        float num1 = x - px;
        //        float num2 = y - py;
        //        float num3 = (float) Math.Cos((double) degree);
        //        float num4 = (float) Math.Sin((double) degree);
        //        float num5 = (float) ((double) px + (double) num1 * (double) num3 - (double) num2 * (double) num4);
        //        float num6 = (float) ((double) py + (double) num2 * (double) num3 + (double) num1 * (double) num4);
        //        x = num5;
        //        y = num6;
        //    }
        //
        //    public static float getDirection(float d)
        //    {
        //        if ((double) d > 0.0)
        //            return 1f;
        //        return (double) d < 0.0 ? -1f : 0.0f;
        //    }
        //
        //    public static float getDistanceSpeedTime(float speed, float interval_ms)
        //    {
        //        float num = interval_ms / 1000f;
        //        return speed * num;
        //    }
        //
        //    public static float getDistance(float rx, float ry)
        //    {
        //        return (float) Math.Sqrt((double) rx * (double) rx + (double) ry * (double) ry);
        //    }
        //
        //    public static float getDistance(float x1, float y1, float x2, float y2)
        //    {
        //        float num1 = x1 - x2;
        //        float num2 = y1 - y2;
        //        return (float) Math.Sqrt((double) num1 * (double) num1 + (double) num2 * (double) num2);
        //    }
        //
        //    public static float getDistanceSquare(float x1, float y1, float x2, float y2)
        //    {
        //        float num1 = x1 - x2;
        //        float num2 = y1 - y2;
        //        return (float) ((double) num1 * (double) num1 + (double) num2 * (double) num2);
        //    }
        //
        //    public static float getDistance(IVector2 v1, IVector2 v2)
        //    {
        //        float num1 = v1.X - v2.X;
        //        float num2 = v1.Y - v2.Y;
        //        return (float) Math.Sqrt((double) num1 * (double) num1 + (double) num2 * (double) num2);
        //    }
        //
        //    public static float getDistanceSquare(IVector2 v1, IVector2 v2)
        //    {
        //        float num1 = v1.X - v2.X;
        //        float num2 = v1.Y - v2.Y;
        //        return (float) ((double) num1 * (double) num1 + (double) num2 * (double) num2);
        //    }
        //
        //    public static float getDegree(float dx, float dy)
        //    {
        //        return (float) Math.Atan2((double) dy, (double) dx);
        //    }
        //
        //    public static float getDegree(float x1, float y1, float x2, float y2)
        //    {
        //        return (float) Math.Atan2((double) y2 - (double) y1, (double) x2 - (double) x1);
        //    }
        //
        //    public static float getDegree(float x1, float y1, float x2, float y2, float x3, float y3)
        //    {
        //        float num = (float) Math.Atan2((double) y2 - (double) y1, (double) x2 - (double) x1);
        //        return (float) Math.Atan2((double) y3 - (double) y1, (double) x3 - (double) x1) - num;
        //    }
        //
        //    public static float getDegree(IVector2 v)
        //    {
        //        return (float) Math.Atan2((double) v.Y, (double) v.X);
        //    }
        //
        //    public static float getDegree(IVector2 a, IVector2 b)
        //    {
        //        return (float) Math.Atan2((double) b.Y - (double) a.Y, (double) b.X - (double) a.X);
        //    }
        //
        //    public static Vector2 vectorAdd(IVector2 a, IVector2 b)
        //    {
        //        return new Vector2() {x = a.X + b.X, y = a.Y + b.Y};
        //    }
        //
        //    public static Vector2 vectorSub(IVector2 a, IVector2 b)
        //    {
        //        return new Vector2() {x = a.X - b.X, y = a.Y - b.Y};
        //    }
        //
        //    public static Vector2 vectorAdd(IVector2 a, float degree, float distance)
        //    {
        //        Vector2 vector2 = new Vector2();
        //        vector2.x = a.X;
        //        vector2.y = a.Y;
        //        MathVector3D.movePolar((IVector2) vector2, degree, distance);
        //        return vector2;
        //    }
        //
        //    public static Vector2 vectorAdd(IVector2 a, float distance)
        //    {
        //        Vector2 vector2 = new Vector2();
        //        vector2.x = a.X;
        //        vector2.y = a.Y;
        //        MathVector3D.movePolar((IVector2) vector2, MathVector3D.getDegree((IVector2) vector2), distance);
        //        return vector2;
        //    }
        //
        //    public static Vector2 vectorScale(IVector2 a, float scale)
        //    {
        //        return new Vector2()
        //        {
        //            x = a.X * scale,
        //            y = a.Y * scale
        //        };
        //    }
        //
        //    public static float vectorDot(IVector2 v1, IVector2 v2)
        //    {
        //        return (float) ((double) v1.X * (double) v2.X + (double) v1.Y * (double) v2.Y);
        //    }
        //
        //    public static float vectorDot(float x1, float y1, float x2, float y2)
        //    {
        //        return (float) ((double) x1 * (double) x2 + (double) y1 * (double) y2);
        //    }
        //
        //    public static void moveImpact(
        //        ICollection<IRoundObject> vectors,
        //        IRoundObject obj,
        //        float angle,
        //        float distance,
        //        int depth,
        //        int max_depth)
        //    {
        //        float num1 = (float) Math.Cos((double) angle) * distance;
        //        float num2 = (float) Math.Sin((double) angle) * distance;
        //        obj.X += num1;
        //        obj.Y += num2;
        //        if (depth >= max_depth)
        //            return;
        //        foreach (IRoundObject vector in (IEnumerable<IRoundObject>) vectors)
        //        {
        //            if (!vector.Equals((object) obj))
        //            {
        //                float num3 = MathVector3D.getDistance((IVector2) vector, (IVector2) obj) - vector.RadiusSize -
        //                             obj.RadiusSize;
        //                if ((double) num3 < 0.0)
        //                {
        //                    float degree = MathVector3D.getDegree(obj.X, obj.Y, vector.X, vector.Y);
        //                    MathVector3D.moveImpact(vectors, vector, degree, -num3, depth + 1, max_depth);
        //                }
        //            }
        //        }
        //    }
    }
}
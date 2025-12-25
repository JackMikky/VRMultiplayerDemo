using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXR.Construction.Runtime
{
    /// <summary>
    /// 計算ユーティリティクラス
    /// </summary>
    public static class MathUtil
    {
        #region < Common >
        /// <summary>
        /// 与えられた点群から重心を算出します
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static Vector3 CalcCentroid(List<GameObject> pts)
        {
            List<Vector3> list = new List<Vector3>();
            foreach (var pt in pts)
            {
                list.Add(pt.transform.position);
            }
            return CalcCentroid(list);
        }

        /// <summary>
        /// 与えられた点群から重心を算出します
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static Vector3 CalcCentroid(List<Vector3> pts)
        {
            Vector3 center = Vector3.zero;
            foreach (var pt in pts)
            {
                center += pt;
            }
            return center / pts.Count;
        }

        /// <summary>
        /// 点と直線の距離を求めます
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="origin"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float CalcLineAndPointDist(Vector3 pt, Vector3 v)
        {
            Vector3 o2p = pt;
            v.Normalize();
            float dot = Vector3.Dot(v, o2p);
            return (o2p - v * dot).magnitude;
        }
        #endregion

        #region < Project >
        /// <summary>
        /// 任意の点とベクトルに対し、ベクトルと、点がベクトルに落とす影の長さの比を返します
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="c"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static float CalcProjectPointRatio(Vector3 pt, Vector3 c, Vector3 n)
        {
            Vector3 c2p = pt - c;
            float dot = Vector3.Dot(n, c2p);
            Vector3 c2pProjN = n * dot;

            return dot;
        }

        /// <summary>
        /// 平面に対する投影点の位置と、元々の平面との距離を算出します
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="c"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static (Vector3 projectPoint, float originDist) CalcProjectPoint(Vector3 pt, Vector3 c, Vector3 n)
        {
            Vector3 c2p = pt - c;
            float dot = Vector3.Dot(n, c2p);
            Vector3 c2pProjN = n * dot;

            Vector3 projectPoint = c + c2p - c2pProjN;
            float originDist = Mathf.Abs(dot);

            return (projectPoint, originDist);
        }
        #endregion

        public static bool Approximately(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }

        public static float Floor(float value, int digits)
        {
            var value_strs = value.ToString().Split(".");
            if (value_strs.Length >= 2)
            {
                var str = value_strs[^1];
                value_strs[^1] = str[0..Mathf.Min(digits, str.Length)];
            }
            return float.Parse(string.Join(".", value_strs));
        }

        public static TimeSpan Clamp(TimeSpan value, TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromSeconds(Math.Clamp(value.TotalSeconds, min.TotalSeconds, max.TotalSeconds));
        }
    }
}
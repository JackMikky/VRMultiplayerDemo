using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXR.Construction.Runtime
{
    [RequireComponent(typeof(BoxCollider))]
    public class TwinFieldBase : MonoBehaviour
    {
        /// <summary>
        /// 対になるField
        /// </summary>
        public TwinFieldBase Twin;

        /// <summary>
        /// 管理対象の子オブジェクト群
        /// </summary>
        protected List<TwinObjectBase> Children = new List<TwinObjectBase>();

        /// <summary>
        /// コライダ
        /// </summary>
        private BoxCollider _collider;

        /// <summary>
        /// コライダ
        /// </summary>
        public BoxCollider Collider
        {
            get
            {
                if (_collider == null)
                {
                    _collider = GetComponent<BoxCollider>();
                }
                return _collider;
            }
        }

        /// <summary>
        /// コライダ定義
        /// </summary>
        private readonly Vector3 size = new Vector3(1, 1, 1);

        /// <summary>
        /// スケール
        /// </summary>
        [Range(0.1f, 10f)]
        public float Scale = 1f;

        #region Vector3
        private Vector3 right => transform.right;
        private Vector3 up => transform.up;
        private Vector3 forward => transform.forward;
        Vector3 ruf => ( right * size.x + up * size.y * 2f + forward * size.z) * Scale * 0.5f;
        Vector3 rub => ( right * size.x + up * size.y * 2f - forward * size.z) * Scale * 0.5f;
        Vector3 rdf => ( right * size.x                    + forward * size.z) * Scale * 0.5f;
        Vector3 rdb => ( right * size.x                    - forward * size.z) * Scale * 0.5f;
        Vector3 luf => (-right * size.x + up * size.y * 2f + forward * size.z) * Scale * 0.5f;
        Vector3 lub => (-right * size.x + up * size.y * 2f - forward * size.z) * Scale * 0.5f;
        Vector3 ldf => (-right * size.x                    + forward * size.z) * Scale * 0.5f;
        Vector3 ldb => (-right * size.x                    - forward * size.z) * Scale * 0.5f;
        #endregion

        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {
            // 最終的にはStart文で一度だけ実施
            {
                Collider.center = Vector3.up * Collider.size.y / 2f;
                Collider.size = size;
                this.transform.localScale = Vector3.one * Scale;
            }

#if UNITY_EDITOR
            DrawDebugLines();
#endif
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            
        }

        /// <summary>
        /// ミラーリング要求を受けた際の処理
        /// </summary>
        /// <param name="requester"></param>
        protected void OnMirroringRequested(TwinObjectBase requester)
        {
            requester.Twin.transform.localPosition = this.transform.InverseTransformPoint(requester.transform.position);
            requester.Twin.transform.localRotation = requester.transform.localRotation;
        }

        /// <summary>
        /// 子要素を追加します
        /// </summary>
        /// <param name="obj"></param>
        public void AddChildren(TwinObjectBase obj)
        {
            obj.transform.parent = this.transform;
            Children.Add(obj);
        }

#if UNITY_EDITOR
        /// <summary>
        /// デバッグ用当たり判定エリア描画
        /// </summary>
        private void DrawDebugLines()
        {
            var p = this.transform.position;
            var c = Color.black;
            Debug.DrawLine(p + rdf, p + ldf, c);
            Debug.DrawLine(p + ldf, p + ldb, c);
            Debug.DrawLine(p + ldb, p + rdb, c);
            Debug.DrawLine(p + rdb, p + rdf, c);

            Debug.DrawLine(p + ruf, p + luf, c);
            Debug.DrawLine(p + luf, p + lub, c);
            Debug.DrawLine(p + lub, p + rub, c);
            Debug.DrawLine(p + rub, p + ruf, c);

            Debug.DrawLine(p + ruf, p + rdf, c);
            Debug.DrawLine(p + luf, p + ldf, c);
            Debug.DrawLine(p + rub, p + rdb, c);
            Debug.DrawLine(p + lub, p + ldb, c);
        }
#endif
    }
}

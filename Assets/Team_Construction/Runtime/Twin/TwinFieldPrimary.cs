using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PXR.Construction.Runtime
{
    public class TwinFieldPrimary : TwinFieldBase
    {
        /// <summary>
        /// 管理対象のオブジェクト群
        /// </summary>
        [SerializeField] private List<TwinObjectPrimary> targetObjects;

        /// <summary>
        /// コライダ内のChild群
        /// </summary>
        private List<TwinObjectBase> inCollderChildren = new List<TwinObjectBase>();

        protected override void Awake()
        {
            InitTargetObjects();
        }

        private void InitTargetObjects()
        {
            foreach (var target in targetObjects)
            {
                AddChildren(target);

                target.OnObjectStoped += OnChildStoped;
                target.OnObjectMoved += OnChildMoved;
            }
        }

        /// <summary>
        /// 触れたオブジェクトが自身の処理対象であればイベント登録
        /// </summary>
        /// <param name="other"></param>
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            // TODO : もう少しマトモな参照取得
            var parent = other.transform.parent;
            if(parent == null || parent.parent == null)
            {
                return;
            }

            var view = parent.parent.GetComponent<ViewObjectController>();
            if(view == null)
            {
                return;
            }

            var child = Children.Find(p => p.transform == view.Parent);
            if(child == null)
            {
                return;
            }

            child.Twin.Show();
            inCollderChildren.Add(child);
        }

        /// <summary>
        /// 離れたオブジェクトが自身の処理対象であればイベント登録解除
        /// </summary>
        /// <param name="other"></param>
        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);

            var parent = other.transform.parent;
            if (parent == null)
            {
                return;
            }

            var child = inCollderChildren.Find(o => o.gameObject == parent.gameObject);
            if(child == null)
            {
                return;
            }

            child.Twin.Hide();
            inCollderChildren.Remove(child);
        }

        /// <summary>
        /// 子要素が静止したとき
        /// </summary>
        /// <param name="obj"></param>
        private void OnChildStoped(TwinObjectBase obj)
        {
            if (!inCollderChildren.Contains(obj))
            {
                return;
            }

            Debug.Log("<color=cyan>OnChildStoped</color>:" + obj.name);
            obj.Twin.Show();
        }

        /// <summary>
        /// 子要素が動き始めたとき
        /// </summary>
        /// <param name="obj"></param>
        private void OnChildMoved(TwinObjectBase obj)
        {
            if(!inCollderChildren.Contains(obj))
            {
                return;
            }

            Debug.Log("<color=green>OnChildMoved</color>:" + obj.name);
            obj.Twin.Hide();
        }

        /// <summary>
        /// 子要素を追加します
        /// </summary>
        /// <param name="obj"></param>
        public void AddChildren(TwinObjectBase obj)
        {
            // 基底実装処理＋ミラーリング要求のListen開始
            {
                base.AddChildren(obj);
                obj.MirroringRequest += OnMirroringRequested;
            }

            // 対になるオブジェクトを対Fieldの子要素として登録
            {
                Twin.AddChildren(obj.Twin);
                obj.Twin.Hide();
            }
        }
    }
}

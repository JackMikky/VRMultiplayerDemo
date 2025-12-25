using System;
using UnityEngine;

namespace PXR.Construction.Runtime
{
    public class TwinObjectBase : MonoBehaviour
    {
        /// <summary>
        /// 対になるObject
        /// </summary>
        public TwinObjectBase Twin;

        /// <summary>
        /// 外観オブジェクト
        /// </summary>
        [SerializeField] private ViewObjectController viewObject;

        /// <summary>
        /// 静止イベント
        /// </summary>
        public Action<TwinObjectBase> OnObjectStoped;

        /// <summary>
        /// 移動イベント
        /// </summary>
        public Action<TwinObjectBase> OnObjectMoved;

        /// <summary>
        /// 対オブジェクトミラーリング要求
        /// </summary>
        public Action<TwinObjectBase> MirroringRequest;

        /// <summary>
        /// 位置静止閾値[m]
        /// </summary>
        private readonly float posVecThresh = 0.0001f;

        /// <summary>
        /// 回転静止閾値[deg]
        /// </summary>
        private readonly float rotVecThresh = 0.0001f;

        /// <summary>
        /// 前回位置
        /// </summary>
        private Vector3 lastPosBuf;

        /// <summary>
        /// 前回回転
        /// </summary>
        private Quaternion lastRotBuf;

        /// <summary>
        /// 現在静止中かどうか
        /// </summary>
        protected bool IsStoppingCurrent
        {
            get => avePosSpeed < posVecThresh &&
                   aveRotSpeed < rotVecThresh;
        }

        /// <summary>
        /// 現在の位置速度[m/s]
        /// </summary>
        private float currentPosSpeed => (this.lastPosBuf - this.transform.position).magnitude;

        /// <summary>
        /// 平滑化位置速度[m/s]
        /// </summary>
        private float avePosSpeed = 0f;

        /// <summary>
        /// 現在の回転速度[deg/s]
        /// </summary>
        private float currentRotSpeed => Quaternion.Angle(this.transform.rotation, lastRotBuf);

        /// <summary>
        /// 平滑化回転速度[deg/s]
        /// </summary>
        private float aveRotSpeed = 0f;

        /// <summary>
        /// 前回フレームが静止中判定だったかどうか
        /// </summary>
        protected bool IsStoppingPrev = false;

        /// <summary>
        /// 初回のバッファ更新
        /// </summary>
        protected virtual void Start()
        {
            lastPosBuf = this.transform.position;
            lastRotBuf = this.transform.rotation;
        }

        /// <summary>
        /// 継承先で必要に応じて実装
        /// </summary>
        protected virtual void Update()
        {

        }

        /// <summary>
        /// 速度計算とイベント発火処理
        /// </summary>
        protected virtual void LateUpdate()
        {
            // 速度の平滑化
            avePosSpeed = avePosSpeed * 0.9f + currentPosSpeed * 0.1f;
            aveRotSpeed = aveRotSpeed * 0.9f + currentRotSpeed * 0.1f;

            // 必要に応じてイベント発火
            InvokeIfNecessary();

            // 次フレームの計算のため値を更新
            IsStoppingPrev = IsStoppingCurrent;
            lastPosBuf = this.transform.position;
            lastRotBuf = this.transform.rotation;
        }

        /// <summary>
        /// ビューオブジェクトの設定
        /// </summary>
        /// <param name="idx"></param>
        public void SetView(int idx)
        {
            viewObject.ResetView(idx);
            Twin.viewObject.ResetView(idx);
        }

        /// <summary>
        /// ビューオブジェクトを表示します
        /// </summary>
        public void Show()
        {
            viewObject.Show();
        }

        /// <summary>
        /// ビューオブジェクトを非表示にします
        /// </summary>
        public void Hide()
        {
            viewObject.Hide();
        }

        /// <summary>
        /// 必要に応じてイベント発火
        /// </summary>
        private void InvokeIfNecessary()
        {
            if (IsStoppingCurrent != IsStoppingPrev)
            {
                if (IsStoppingCurrent)
                {
                    OnObjectStoped?.Invoke(this);
                }
                else
                {
                    OnObjectMoved?.Invoke(this);
                }
            }
        }
    }
}
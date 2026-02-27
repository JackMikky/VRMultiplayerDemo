using System;
using System.Collections;
using UnityEngine;

namespace Assets.Team_Prevention.Script.Effects
{
    /// <summary>
    /// 指定したGameObjectをONにし、一定時間後にOFFに戻すエフェクト。
    /// Lightの点灯、子モデルの表示など汎用用途向け。
    /// </summary>
    public sealed class ToggleGameObjectEffectPlayer : MonoBehaviour, IEffectPlayer
    {
        [Header("対象")]
        [Tooltip("ON/OFFしたい対象。未設定ならこのGameObjectを対象にします。")]
        [SerializeField] private GameObject _target;

        [Header("動作")]
        [Tooltip("ONにしておく時間（秒）")]
        [SerializeField] private float _duration = 1.0f;

        [Tooltip("Play()呼び出し時にONにするか")]
        [SerializeField] private bool _setActiveOnPlay = true;

        [Tooltip("完了時にOFFに戻すか")]
        [SerializeField] private bool _setInactiveOnComplete = true;

        private Coroutine _routine;

        public float Play()
        {
            var target = (_target != null) ? _target : gameObject;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_setActiveOnPlay)
            {
                target.SetActive(true);
            }

            float d = Mathf.Max(0f, _duration);

            if (_setInactiveOnComplete)
            {
                _routine = StartCoroutine(SetInactiveAfter(target, d));
            }

            return d;
        }

        private IEnumerator SetInactiveAfter(GameObject target, float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
            else
            {
                yield return null;
            }

            try
            {
                if (target != null)
                {
                    target.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ToggleGameObjectEffectPlayer] SetActive(false) に失敗しました: {ex}", this);
            }
            finally
            {
                _routine = null;
            }
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private void OnValidate()
        {
            _duration = Mathf.Max(0f, _duration);
        }
    }
}
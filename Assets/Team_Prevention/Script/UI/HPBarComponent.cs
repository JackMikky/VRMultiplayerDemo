using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Team_Prevention.Script.UI
{
    public class HPBarComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PlayerInfo targetPlayer;

        private ProgressBar _hpBar;

        private void Awake()
        {
            var root = uiDocument.rootVisualElement;
            _hpBar = root.Q<ProgressBar>("HPBarProgress");

            // 初期値反映
            _hpBar.lowValue = 0f;
            OnHPChanged(targetPlayer.CurrentHP, targetPlayer.MaxHP);

            // 変更イベント購読
            targetPlayer.OnHPChanged += OnHPChanged;
            targetPlayer.OnScoreChanged += OnScoreChanged;
        }

        private void OnDestroy()
        {
            targetPlayer.OnHPChanged -= OnHPChanged;
            targetPlayer.OnScoreChanged -= OnScoreChanged;
        }


        public void OnHPChanged(float currentHP, float maxHP)
        {
            _hpBar.highValue = maxHP;
            _hpBar.value = currentHP;
            _hpBar.title = $"{currentHP}/{maxHP}";
        }

        public void OnScoreChanged(float currentHP)
        {
            ///ToDo : スコア表示が必要ならここに実装
            ///スコアあるんか
        }


    }
}

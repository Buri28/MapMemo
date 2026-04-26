using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MapMemo.Patches
{
    /// <summary>
    /// HMUI.HoverHintController.SetupAndShowHintPanel のポストフィックスパッチ。
    /// ツールチップパネルのテキストが長い→短いに変わったとき、
    /// Unity の ContentSizeFitter がパネルサイズを更新しない問題を修正します。
    /// パッチ後に LayoutRebuilder.ForceRebuildLayoutImmediate を呼び、
    /// パネルのサイズを強制再計算させます。
    /// </summary>
    public static class HoverHintController_ShowHint_Patch
    {
        private static FieldInfo _panelField;

        /// <summary>
        /// カバー画像に設定された HoverHint コンポーネント。
        /// MemoPanelController から登録します。
        /// </summary>
        public static HMUI.HoverHint CoverHoverHint { get; set; }

        /// <summary>
        /// Harmony パッチを適用します。MapMemoPatcher.ApplyPatches() から呼び出してください。
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            var controllerType = typeof(HMUI.HoverHintController);

            // テキスト設定後に実際にパネルを配置・表示するメソッドにパッチを当てる
            var method = controllerType.GetMethod(
                "SetupAndShowHintPanel",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                Plugin.Log?.Warn("HoverHintController_Patch: SetupAndShowHintPanel not found, skipping patch");
                return;
            }

            _panelField = controllerType.GetField(
                "_hoverHintPanel",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (_panelField == null)
            {
                Plugin.Log?.Warn("HoverHintController_Patch: _hoverHintPanel field not found, skipping patch");
                return;
            }

            var postfix = new HarmonyMethod(
                typeof(HoverHintController_ShowHint_Patch),
                nameof(Postfix));
            harmony.Patch(method, postfix: postfix);

            if (Plugin.VerboseLogs) Plugin.Log?.Info("HoverHintController_Patch: patch applied to SetupAndShowHintPanel");
        }

        static void Postfix(object __instance, HMUI.HoverHint hoverHint)
        {
            if (_panelField == null)
            {
                Plugin.Log?.Warn("HoverHintController_Patch: _panelField is null in Postfix, cannot adjust layout");
                return;
            }

            var panel = _panelField.GetValue(__instance) as MonoBehaviour;
            if (panel == null)
            {
                Plugin.Log?.Warn("HoverHintController_Patch: _hoverHintPanel is null in Postfix, cannot adjust layout");
                return;
            }

            var rt = panel.GetComponent<RectTransform>();
            if (rt == null)
            {
                Plugin.Log?.Warn("HoverHintController_Patch: RectTransform is null in Postfix, cannot adjust layout");
                return;
            }

            // カバー画像のホバーヒントのみ対象とする
            if (CoverHoverHint == null) return;
            if (hoverHint != CoverHoverHint)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("HoverHintController_Patch: skipping non-cover hover hint");
                return;
            }

            // パディング・スペーシングを収集する
            float padH = 0f, padV = 0f, spacingV = 0f;
            foreach (var vlg in panel.GetComponentsInChildren<VerticalLayoutGroup>(true))
            {
                padV += vlg.padding.top + vlg.padding.bottom;
                padH = Mathf.Max(padH, vlg.padding.left + vlg.padding.right);
                int activeChildren = 0;
                for (int i = 0; i < vlg.transform.childCount; i++)
                    if (vlg.transform.GetChild(i).gameObject.activeInHierarchy) activeChildren++;
                if (activeChildren > 1)
                    spacingV += vlg.spacing * (activeChildren - 1);
            }

            // 1. 一度 ForceMeshUpdate して preferredWidth（折り返しなしの自然な幅）を得る
            float maxTextWidth = 0f;
            foreach (var tmp in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                tmp.text = tmp.text;
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"HoverHintController_Patch: text={tmp.text}, preferredWidth={tmp.preferredWidth}");
                maxTextWidth = Mathf.Max(maxTextWidth, tmp.preferredWidth);
            }
            if (maxTextWidth <= 0f) return;

            // 2. 幅を設定する（0～70までとする） 
            float newWidth = Mathf.Max(0f, (maxTextWidth * 1.6f) + padH);
            newWidth = Mathf.Min(newWidth, 70f);

            if (Plugin.VerboseLogs) Plugin.Log?.Info($"HoverHintController_Patch: maxTextWidth={maxTextWidth}, padH={padH}, newWidth={newWidth}");
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

            // 3. 幅確定後に再度 ForceMeshUpdate → 折り返しを考慮した正確な preferredHeight を得る
            float textHeight = 0f;
            foreach (var tmp in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                textHeight += tmp.preferredHeight;
            }
            if (textHeight <= 0f) return;

            // 4. 高さを設定する
            float newHeight = textHeight + padV + spacingV + 5f; // 5 は余裕分
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

            if (Plugin.VerboseLogs) Plugin.Log?.Info("HoverHintController_Patch: ForceMeshUpdate + ForceRebuildLayoutImmediate called");
        }
    }
}

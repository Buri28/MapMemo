using BeatSaberMarkupLanguage.Components;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
// Note: Avoid UnityEngine.UI dependency; use UnityEngine.Canvas explicitly

namespace MapMemo.UI.Edit
{

    /// <summary>
    /// ClickableText のクリックイベントをハンドルし、対応する文字列をモーダルに挿入するリスナー。
    /// </summary>
    public class InputKeyClickListener : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>ApplyKeyBindings により設定されるキーエントリ</summary>
        public MapMemo.Core.InputKeyEntry keyEntry;
        /// <summary>キーエントリを設定します。</summary>
        public void SetKeyEntry(MapMemo.Core.InputKeyEntry entry) { this.keyEntry = entry; }

        /// <summary>
        /// クリックイベントハンドラ。設定された KeyEntry または ClickableText のテキストを取得してモーダルに挿入します。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                Plugin.Log?.Info("EmojiClickListener: OnPointerClick called");

                string txt = null;

                // keyEntry が設定されている場合、その内容に基づいて挿入するテキストを決定します。
                if (keyEntry != null)
                {
                    if (string.Equals(keyEntry.type, "Literal", StringComparison.OrdinalIgnoreCase))
                    {
                        txt = keyEntry.@char ?? keyEntry.label;
                    }
                    else if (string.Equals(keyEntry.type, "EmojiRange", StringComparison.OrdinalIgnoreCase))
                    {
                        txt = keyEntry.label;
                        // if (!string.IsNullOrEmpty(keyEntry.label)) txt = keyEntry.label;
                        // else if (keyEntry.ranges != null && keyEntry.ranges.Count > 0)
                        // {
                        //     var r = keyEntry.ranges[0];
                        //     if (!string.IsNullOrEmpty(r.start))
                        //     {
                        //         if (int.TryParse(r.start.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? r.start.Substring(2) : r.start,
                        //             System.Globalization.NumberStyles.HexNumber, null, out int cp))
                        //         {
                        //             txt = char.ConvertFromUtf32(cp);
                        //         }
                        //     }
                        // }
                    }
                }

                // // フォールバック: ClickableText.text
                // if (string.IsNullOrEmpty(txt))
                // {
                //     var ct = GetComponent<ClickableText>();
                //     if (ct != null) txt = ct.text.Trim().Replace("　", "");
                // }

                // ひらがな・カタカナ変換を行う
                txt = MemoEditModalController.Instance.isKanaMode ?
                    InputKeyController.HiraganaToKatakana(txt) :
                    InputKeyController.KatakanaToHiragana(txt);
                // Shift 状態に応じて大文字・小文字を切り替える
                txt = MemoEditModalController.Instance.isShift ?
                    txt.ToLowerInvariant() :
                    txt.ToUpperInvariant();

                if (string.IsNullOrEmpty(txt)) return;
                Plugin.Log?.Info($"KeyClickListener: Key '{txt}' clicked, appending to memo");

                MemoEditModalController.Instance.Append(txt);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"KeyClickListener.OnPointerClick: {ex.Message}");
            }
        }
    }
}
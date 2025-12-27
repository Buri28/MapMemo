using System;
using Mapmemo.Models;
using MapMemo.Services;
using MapMemo.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MapMemo.UI.Edit
{

    /// <summary>
    /// ClickableText のクリックイベントをハンドルし、対応する文字列をモーダルに挿入するリスナー。
    /// </summary>
    public class InputKeyClickListener : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>ApplyKeyBindings により設定されるキーエントリ</summary>
        public InputKeyEntry keyEntry;
        /// <summary>キーエントリを設定します。</summary>
        public void SetKeyEntry(InputKeyEntry entry) { this.keyEntry = entry; }

        /// <summary>
        /// クリックイベントハンドラ。設定された KeyEntry または ClickableText のテキストを
        /// 取得してモーダルに挿入します。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("EmojiClickListener: OnPointerClick called");

                string txt = null;

                // keyEntry が設定されている場合、その内容に基づいて挿入するテキストを決定します。
                if (keyEntry != null)
                {
                    if (keyEntry.IsLiteralType())
                    {
                        txt = keyEntry.@char ?? keyEntry.label;
                    }
                    else if (keyEntry.IsEmojiType())
                    {
                        txt = keyEntry.label;
                    }
                }
                // KeyEntry が設定されていないか、テキストが空の場合は空文字を設定する
                if (string.IsNullOrEmpty(txt))
                {
                    txt = "";
                }
                // ひらがな・カタカナ変換を行う
                txt = MemoEditModalController.Instance.isKanaMode ?
                    InputKeyHandler.HiraganaToKatakana(txt) :
                    InputKeyHandler.KatakanaToHiragana(txt);
                // Shift 状態に応じて大文字・小文字を切り替える
                txt = MemoEditModalController.Instance.isShift ?
                    txt.ToLowerInvariant() :
                    txt.ToUpperInvariant();

                // 濁点・半濁点変換を行う
                txt = StringHelper.ConvertDakutenHandakuten(
                    txt, MemoEditModalController.Instance.dakutenMode);

                if (string.IsNullOrEmpty(txt)) return;
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"KeyClickListener: "
                    + $"Key '{txt}' clicked, appending to memo");

                MemoEditModalController.Instance.Append(txt);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"KeyClickListener.OnPointerClick: {ex.Message}");
            }
        }
    }
}
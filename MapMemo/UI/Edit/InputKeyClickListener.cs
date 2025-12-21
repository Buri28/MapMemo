using BeatSaberMarkupLanguage.Components;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
// Note: Avoid UnityEngine.UI dependency; use UnityEngine.Canvas explicitly

namespace MapMemo.UI.Edit
{

    public class InputKeyClickListener : MonoBehaviour, IPointerClickHandler
    {
        public MemoEditModalController controller;
        // Populated by ApplyKeyBindings when available
        public MapMemo.Core.InputKeyEntry keyEntry;
        public void SetKeyEntry(MapMemo.Core.InputKeyEntry entry) { this.keyEntry = entry; }

        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                Plugin.Log?.Info("EmojiClickListener: OnPointerClick called");

                string txt = null;

                // Prefer configured KeyEntry when present
                if (keyEntry != null)
                {
                    if (string.Equals(keyEntry.type, "Literal", StringComparison.OrdinalIgnoreCase))
                    {
                        txt = keyEntry.@char ?? keyEntry.label;
                    }
                    else if (string.Equals(keyEntry.type, "EmojiRange", StringComparison.OrdinalIgnoreCase))
                    {
                        // Prefer explicit label, fallback to first codepoint in ranges
                        if (!string.IsNullOrEmpty(keyEntry.label)) txt = keyEntry.label;
                        else if (keyEntry.ranges != null && keyEntry.ranges.Count > 0)
                        {
                            var r = keyEntry.ranges[0];
                            if (!string.IsNullOrEmpty(r.start))
                            {
                                if (int.TryParse(r.start.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? r.start.Substring(2) : r.start,
                                    System.Globalization.NumberStyles.HexNumber, null, out int cp))
                                {
                                    txt = char.ConvertFromUtf32(cp);
                                }
                            }
                        }
                    }
                }

                // Fallback: ClickableText.text
                if (string.IsNullOrEmpty(txt))
                {
                    var ct = GetComponent<ClickableText>();
                    if (ct != null) txt = ct.text.Trim().Replace("　", "");
                }
                if (MemoEditModalController.Instance.isKanaMode)
                {
                    // かなモードの場合、ひらがな・カタカナ変換を行う
                    txt = InputKeyController.HiraganaToKatakana(txt);
                }
                else
                {
                    txt = InputKeyController.KatakanaToHiragana(txt);
                }
                if (MemoEditModalController.Instance.isShift)
                {
                    txt = txt.ToLowerInvariant();
                }
                else
                {
                    txt = txt.ToUpperInvariant();
                }

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
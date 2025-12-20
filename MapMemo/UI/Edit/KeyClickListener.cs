using BeatSaberMarkupLanguage.Components;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
// Note: Avoid UnityEngine.UI dependency; use UnityEngine.Canvas explicitly

namespace MapMemo.UI.Edit
{

    public class KeyClickListener : MonoBehaviour, IPointerClickHandler
    {
        public MemoEditModalController controller;
        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                Plugin.Log?.Info("EmojiClickListener: OnPointerClick called");

                // Try ClickableText first
                string txt = null;
                var ct = GetComponent<ClickableText>();
                if (ct != null) txt = ct.text.Trim().Replace("　", "");

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
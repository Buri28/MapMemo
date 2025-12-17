using System;
using System.Linq;
using UnityEngine;
using TMPro;
using HMUI;
using UnityEngine.UI;
using BeatSaberMarkupLanguage.Components;

namespace MapMemo.UI.Edit
{
    public static class MemoEditModalHelper
    {
        public static void ApplyAlphaButtonCosmetics(ModalView modal, bool isShift)
        {
            if (modal == null) return;
            try
            {
                foreach (var btn in modal.gameObject.GetComponentsInChildren<ClickableText>(true))
                {
                    try
                    {
                        if (btn == null) continue;
                        btn.fontSize = 3.8f;
                        btn.fontStyle = FontStyles.Italic | FontStyles.Underline;
                        btn.alignment = TextAlignmentOptions.Center;
                        btn.color = Color.cyan;
                        btn.DefaultColor = Color.cyan;
                        btn.HighlightColor = new Color(1f, 0.3f, 0f, 1f);
                        btn.outlineColor = Color.yellow;
                        btn.outlineWidth = 0.3f;

                        var layout = btn.gameObject.GetComponent<LayoutElement>();
                        if (layout == null)
                            layout = btn.gameObject.AddComponent<LayoutElement>();
                        layout.preferredWidth = 5f;
                        layout.minWidth = 5f;

                        var label = btn.text.Trim().Replace(" ", "");
                        label = isShift ? label.ToLowerInvariant() : label.ToUpperInvariant();
                        btn.text = EditLabel(label);
                    }
                    catch { /* ignore per-button failures */ }
                }
            }
            catch { /* ignore overall failures */ }
        }

        public static void UpdateAlphaButtonLabels(ModalView modal, bool isShift)
        {
            if (modal == null)
            {
                MapMemo.Plugin.Log?.Warn("MemoEditModal.UpdateAlphaButtonLabels: modal is null, cannot collect buttons");
                return;
            }
            if (modal.gameObject == null)
            {
                MapMemo.Plugin.Log?.Warn("MemoEditModal.UpdateAlphaButtonLabels: modal.gameObject is null, cannot collect buttons");
                return;
            }
            try
            {
                var comps = modal.gameObject.GetComponentsInChildren<ClickableText>(true);
                MapMemo.Plugin.Log?.Info("MemoEditModal.UpdateAlphaButtonLabels: " + comps.Count() + " ClickableText components found under modal");
                foreach (var btn in comps)
                {
                    try
                    {
                        var stored = btn.text.Trim().Replace("　", "");
                        var label = isShift ? stored.ToLowerInvariant() : stored.ToUpperInvariant();
                        btn.text = EditLabel(label);
                    }
                    catch { /* ignore per-button failures */ }
                }
            }
            catch (Exception ex)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModalHelper.UpdateAlphaButtonLabels: {ex.Message}");
            }
        }

        public static void RepositionModalToLeftHalf(ModalView modal)
        {
            if (modal == null) return;
            try
            {
                var rt = modal.gameObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float offsetX = 0f;
                    var parentCanvas = modal.gameObject.GetComponentInParent<Canvas>();
                    if (parentCanvas != null)
                    {
                        var canvasRt = parentCanvas.GetComponent<RectTransform>();
                        if (canvasRt != null)
                        {
                            offsetX = -1f * (canvasRt.rect.width * 0.5f);
                        }
                    }
                    if (offsetX == 0f)
                    {
                        offsetX = -1f * (UnityEngine.Screen.width * 0.5f);
                    }
                    var current = rt.anchoredPosition;
                    rt.anchoredPosition = new Vector2(current.x + offsetX, current.y);
                    MapMemo.Plugin.Log?.Info($"MemoEditModal.RepositionModalToLeftHalf: shifted modal anchoredPosition by {offsetX} (newX={rt.anchoredPosition.x})");
                }
            }
            catch (Exception ex)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.RepositionModalToLeftHalf: exception {ex}");
            }
        }

        public static string EditLabel(string label)
        {
            return "  " + label + "  ";
        }

        public static string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm:ss}";
        }
    }
}

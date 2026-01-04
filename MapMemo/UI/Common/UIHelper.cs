using System;
using System.Collections;
using TMPro;
using UnityEngine;


namespace MapMemo.UI.Common
{
    /// <summary>
    /// UI 関連のユーティリティメソッドを提供します。
    /// </summary>
    public class UIHelper : MonoBehaviour
    {
        /// <summary> シングルトンインスタンス。</summary>
        public static UIHelper Instance { get; private set; }

        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれ、シングルトン登録
        /// と DontDestroyOnLoad を実行します。
        /// </summary>
        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("UIHelper Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// 指定した TextMeshProUGUI に一時的にメッセージを表示します。
        /// </summary>
        /// <param name="text">表示先の TextMeshProUGUI。</param>
        /// <param name="message">表示するメッセージ。</param>
        /// <param name="duration">表示時間（秒）。デフォルトは 2 秒。</param>
        /// </summary>
        public void ShowTemporaryMessage(
            TextMeshProUGUI text, string message, float duration = 2f)
        {
            StartCoroutine(
                ShowTemporaryMessageCoroutine(text, message, duration));
        }

        /// <summary>
        /// 指定した TextMeshProUGUI に一時的にメッセージを表示するコルーチン。
        /// </summary>
        /// <param name="text">表示先の TextMeshProUGUI。</param>
        /// <param name="message">表示するメッセージ。</param>
        /// <param name="duration">表示時間（秒）。</param>
        /// </summary>
        private IEnumerator ShowTemporaryMessageCoroutine(TextMeshProUGUI text,
                                                          string message,
                                                          float duration)
        {
            text.text = message;
            text.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            text.gameObject.SetActive(false);
        }

        private float oneLineHeight = -1f;

        /// <summary>
        /// 指定した TextMeshProUGUI コンポーネントのテキスト
        /// が最大行数を超えるかどうかを判定します。
        /// </summary>
        /// <param name="textComponent">対象の TextMeshProUGUI コンポーネント。</param>
        /// <param name="maxLines">最大行数。</param>
        /// <param name="s">追加予定のテキスト。</param>
        public Boolean IsOverMaxLine(TextMeshProUGUI textComponent, int maxLines, string s)
        {
            float boxWidth = textComponent.rectTransform.rect.width;
            if (oneLineHeight < 0f)
            {
                // 1行分の高さを測る（初回だけでOK）
                string sample = "AgjÉÅあ漢字";
                oneLineHeight = textComponent.GetPreferredValues(sample, boxWidth, 0).y;
            }
            string currentText = textComponent.text;

            // 入力を受け取る前に、仮に1文字追加する
            string simulatedText = currentText + s;  // s は入力予定の文字

            // 高さを測る
            float newHeight = textComponent.GetPreferredValues(simulatedText, boxWidth, 0).y;
            int estimatedLines = Mathf.CeilToInt(newHeight / oneLineHeight);
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.Append: oneLineHeight={oneLineHeight} "
                                                   + $"newHeight={newHeight} estimatedLines={estimatedLines}"
                                                   + $"({(newHeight / oneLineHeight).ToString("F1")})");

            return estimatedLines > maxLines;
        }

        /// <summary>
        /// スコアに応じたマルチグラデーションカラーコードを取得します。
        /// </summary>
        /// <param name="score">スコア（0〜100）。</param>
        /// <returns>カラーコード（例: #FF0000）。</returns>
        public string GetMultiGradientColor(float score)
        {
            // スコアを0〜100に制限
            score = Mathf.Clamp(score, 0f, 100f);

            int r = 0, g = 0, b = 0;
            if (score <= 50)
            {
                // 赤 (#FF0000) → 黄 (#FFFF00)
                double ratio = score / 50.0;
                r = 255;
                g = (int)(255 * ratio);
                b = 0;
            }
            else if (score <= 80)
            {
                // 黄 (#FFFF00) → 緑 (#00FF00)
                double ratio = (score - 50) / 30.0;
                r = (int)(255 * (1 - ratio));
                g = 255;
                b = 0;
            }
            else if (score <= 90)
            {
                // 緑 (#00FF00) → シアン (#00FFFF)
                double ratio = (score - 80) / 10.0;
                r = 0;
                g = 255;
                b = (int)(255 * ratio);
            }
            else
            {
                // シアン (#00FFFF) → 紫 (#FF00FF)
                double ratio = (score - 90) / 10.0;
                r = (int)(255 * ratio);
                g = (int)(255 * (1 - ratio));
                b = 255;
            }

            return $"#{r:X2}{g:X2}{b:X2}";
        }
        public string GetHighlightColor(string hexColor, double factor = 1.5)
        {
            // #を除去してRGBを取得
            int r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

            r += 70;
            g += 70;
            b += 70;

            // 明るさを調整（factor > 1で明るく、<1で暗く）
            r = Math.Min(255, (int)(r * factor));
            g = Math.Min(255, (int)(g * factor));
            b = Math.Min(255, (int)(b * factor));

            return $"#{r:X2}{g:X2}{b:X2}";
        }
        public string ToColorHex(Color color)
        {
            string hex = $"#{color.r:X2}{color.g:X2}{color.b:X2}";
            return hex;
        }
        public Color ToColor(string hex)
        {
            hex = hex.Replace("#", "");

            if (hex.Length != 6)
            {
                Debug.LogWarning("Hex color must be 6 characters long.");
                return Color.magenta; // エラー時の目立つ色
            }

            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r / 255f, g / 255f, b / 255f);
        }
    }
}
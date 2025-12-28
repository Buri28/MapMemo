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
    }
}
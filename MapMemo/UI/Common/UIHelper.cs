using System;
using System.Collections;
using System.Collections.Generic;
using MapMemo.Core;
using TMPro;
using UnityEngine;


namespace MapMemo.UI.Common
{
    /// <summary>
    /// UI 関連のユーティリティメソッドを提供します。
    /// </summary>
    public class UIHelper : MonoBehaviour
    {
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
    }
}
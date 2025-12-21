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

        private void Awake()
        {
            Plugin.Log?.Info("UIHelper Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }


        public void ShowTemporaryMessage(
            TextMeshProUGUI text, string message, float duration = 2f)
        {
            InputHistoryManager.DeleteHistory();
            StartCoroutine(
                ShowTemporaryMessageCoroutine(text, message, duration));
        }

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
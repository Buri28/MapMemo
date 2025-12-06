using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

namespace MapMemo.UI
{
    // Harmonyパッチの雛形。実際のクラス/メソッド名は環境に合わせて設定すること。
    public static class HarmonyPatches
    {
        // 候補メソッド名（バージョン差分に対応するため複数）
        private static readonly List<CandidateInfo> Candidates = new List<CandidateInfo>
        {
            new CandidateInfo("StandardLevelDetailView", new[]{"RefreshContent", "SetData", "Setup"}),
            new CandidateInfo("LevelSelectionNavigationController", new[]{"DidSelectLevel", "HandleLevelSelection"}),
            new CandidateInfo("StandardLevelDetailViewController", new[]{"RefreshContent", "SetData", "Setup", "HandleDifficultyBeatmapSelected", "HandleDifficultyBeatmapChange"}),
            new CandidateInfo("LevelCollectionNavigationController", new[]{"HandleLevelCollectionViewControllerDidSelectLevel", "HandleLevelCollectionViewControllerDidChangeLevelSelection", "DidSelectLevel"})
        };

        private class CandidateInfo
        {
            public string TypeName { get; set; }
            public string[] MethodNames { get; set; }

            public CandidateInfo(string typeName, string[] methodNames)
            {
                TypeName = typeName;
                MethodNames = methodNames;
            }
        }

        public static void TryApplyPatches(Harmony harmony)
        {
            foreach (var candidate in Candidates)
            {
                var t = FindType(candidate.TypeName);
                if (t == null) continue;
                foreach (var mName in candidate.MethodNames)
                {
                    var mb = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              .FirstOrDefault(mi => mi.Name == mName);
                    if (mb == null) continue;
                    try
                    {
                        var postfix = new HarmonyMethod(typeof(HarmonyPatches).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
                        harmony.Patch(mb, postfix: postfix);
                        MapMemo.Plugin.Log?.Info($"Patched {t.FullName}.{mName} Postfix");
                        // 複数パッチを許可（バージョン差分でどれかが走るようにする）
                    }
                    catch (Exception e)
                    {
                        MapMemo.Plugin.Log?.Error($"Patch failed {t.FullName}.{mName}: {e}");
                    }
                }

                // 追加: 難易度/レベルを引数に取るメソッドを包括的にパッチ
                try
                {
                    var dynTargets = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(mi => mi.GetParameters().Any(p => p.ParameterType.Name.IndexOf("Beatmap", StringComparison.OrdinalIgnoreCase) >= 0 || p.ParameterType.Name.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0))
                        .ToArray();
                    foreach (var mb in dynTargets)
                    {
                        try
                        {
                            var postfix = new HarmonyMethod(typeof(HarmonyPatches).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
                            harmony.Patch(mb, postfix: postfix);
                            MapMemo.Plugin.Log?.Info($"Patched dynamic {t.FullName}.{mb.Name} Postfix (beatmap/level param)");
                        }
                        catch (Exception e)
                        {
                            MapMemo.Plugin.Log?.Warn($"Dynamic patch failed {t.FullName}.{mb.Name}: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    MapMemo.Plugin.Log?.Warn($"Dynamic target scan failed on {t.FullName}: {e.Message}");
                }
            }
            MapMemo.Plugin.Log?.Warn("No target method found for MapMemo patches. Running without auto-attach.");
        }

        private static Type FindType(string simpleName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetTypes().FirstOrDefault(x => x.Name == simpleName);
                if (t != null) return t;
            }
            return null;
        }

        // Postfix本体：選曲後の右パネル更新直後に呼ばれる想定
        private static void Postfix(object __instance)
        {
            try
            {
                MapMemo.Plugin.Log?.Info("MapMemo: Postfix entered");
                var inst = __instance as Component;
                Transform root = inst != null ? inst.transform : null;
                if (root == null) return;

                // 曲情報の推測取得（まず Level オブジェクトを探す）
                var levelObj = TryGetLevel(__instance);
                if (levelObj != null)
                {
                    MapMemo.Plugin.Log?.Info($"MapMemo: Level object type='{levelObj.GetType().FullName}' found via TryGetLevel");
                }
                // 重要: __instance(=StandardLevelDetailView)からの'name'誤検出を避けるため、
                // まずはlevelObjからのみ直接プロパティ取得する。levelObjがnullなら空のままコントローラ/UI推論へ。
                string songName = levelObj != null ? TryGetStringProp(levelObj, new[] { "_songName", "songName", "SongName", "Name" }) : null;
                string songAuthor = levelObj != null ? TryGetStringProp(levelObj, new[] { "_songAuthorName", "songAuthorName", "songAuthor", "AuthorName", "_songAuthor" }) : null;
                string levelId = levelObj != null ? TryGetStringProp(levelObj, new[] { "_levelID", "levelID", "levelId", "LevelID", "LevelId" }) : null;
                string hash = levelObj != null ? TryGetStringProp(levelObj, new[] { "_levelHash", "levelHash", "hash", "LevelHash" }) : null;
                songName = NormalizeUnknown(songName);
                songAuthor = NormalizeUnknown(songAuthor);
                levelId = NormalizeUnknown(levelId);
                hash = NormalizeUnknown(hash);

                if (levelObj == null)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: levelObj is null; enumerating string fields/properties on instance for diagnostics");
                    try
                    {
                        var t = __instance.GetType();
                        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                      .Where(f => f.FieldType == typeof(string))
                                      .Select(f => $"F:{f.Name}='{(f.GetValue(__instance) as string) ?? "<null>"}'");
                        var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                      .Where(p => p.PropertyType == typeof(string))
                                      .Select(p =>
                                      {
                                          string val = "<ex>";
                                          try { val = p.GetValue(__instance) as string ?? "<null>"; } catch { }
                                          return $"P:{p.Name}='{val}'";
                                      });
                        if (MapMemo.Plugin.VerboseLogs)
                        {
                            foreach (var s in fields.Concat(props)) MapMemo.Plugin.Log?.Info($"MapMemo: {s}");
                        }
                    }
                    catch { }
                }

                // まず、以前の選曲キャッシュから補完（SongPlayHistory的な状態キャッシュ）
                if ((string.IsNullOrEmpty(songName) && string.IsNullOrEmpty(songAuthor)) || (string.IsNullOrEmpty(levelId) && string.IsNullOrEmpty(hash)))
                {
                    if (SelectedLevelState.TryGet(out var st))
                    {
                        songName = string.IsNullOrEmpty(songName) ? st.SongName : songName;
                        songAuthor = string.IsNullOrEmpty(songAuthor) ? st.SongAuthor : songAuthor;
                        levelId = string.IsNullOrEmpty(levelId) ? st.LevelId : levelId;
                        hash = string.IsNullOrEmpty(hash) ? st.Hash : hash;
                        MapMemo.Plugin.Log?.Info($"MapMemo: Filled from SelectedLevelState => name='{songName}' author='{songAuthor}' levelId='{levelId}' hash='{hash}'");
                    }
                }

                // 説明文直下の親Transformを探索（曲名/著者をヒントに右パネルコンテナを選ぶ）
                var parent = FindDescriptionParent(root, songName, songAuthor);
                if (parent == null) return;

                // 追加推測: UIからタイトル/作者を補完
                if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songAuthor))
                {
                    TryInferTitleAuthor(root, ref songName, ref songAuthor);
                }
                // 強化: LevelBarBig と BeatmapParamsPanel からの推論
                if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songAuthor))
                {
                    TryInferFromLevelBarAndParams(root, ref songName, ref songAuthor);
                }
                // さらに強いフォールバック: 画面内のTextから上位2つの非空テキストを採用
                if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songAuthor))
                {
                    try
                    {
                        var textsAll = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
                                           .Where(x => !string.IsNullOrWhiteSpace(x.text))
                                           .OrderByDescending(x => x.fontSize).ToList();
                        if (string.IsNullOrEmpty(songName) && textsAll.Count > 0)
                        {
                            songName = textsAll[0].text.Trim();
                        }
                        if (string.IsNullOrEmpty(songAuthor))
                        {
                            var authorCandidate = textsAll.Skip(1).FirstOrDefault(x => (x.name?.IndexOf("Author", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 || (x.text?.IndexOf("by ", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) ?? textsAll.ElementAtOrDefault(1);
                            if (authorCandidate != null)
                            {
                                var txt = authorCandidate.text.Trim();
                                var idx = txt.IndexOf("by ", StringComparison.OrdinalIgnoreCase);
                                songAuthor = idx >= 0 ? txt.Substring(idx + 3).Trim() : txt;
                            }
                        }
                    }
                    catch { }
                }
                // StandardLevelDetailViewController.selectedDifficultyBeatmap?.level からの直接取得を試みる（DIなし）
                if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songAuthor) || string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(hash))
                {
                    TryGetFromDetailViewController(root, ref songName, ref songAuthor, ref levelId, ref hash);
                }
                // LevelSelectionNavigationController.selectedBeatmapLevel 経路も試す
                if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songAuthor) || string.IsNullOrEmpty(levelId))
                {
                    TryGetFromNavigationController(root, ref songName, ref songAuthor, ref levelId, ref hash);
                }
                // LevelCollectionNavigationController.beatmapLevel/beatmapKey 経路も試す
                if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songAuthor) || string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(hash))
                {
                    TryGetFromLevelCollectionController(root, ref songName, ref songAuthor, ref levelId, ref hash);
                }

                // 取得できたらキャッシュを更新
                if (!string.IsNullOrEmpty(songName) || !string.IsNullOrEmpty(songAuthor) || !string.IsNullOrEmpty(levelId) || !string.IsNullOrEmpty(hash))
                {
                    SelectedLevelState.Update(NormalizeUnknown(songName), NormalizeUnknown(songAuthor), NormalizeUnknown(levelId), NormalizeUnknown(hash));
                }
                MapMemo.Plugin.Log?.Info($"MapMemo: Resolved song info name='{songName}' author='{songAuthor}' levelId='{levelId}' hash='{hash}'");
                string key = MapMemo.KeyResolver.Resolve(hash, levelId);
                // StandardLevelDetailView のフィールドから直接取得を試みる
                if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songAuthor))
                {
                    TryGetFromDetailViewFields(root, ref songName, ref songAuthor, ref levelId, ref hash);
                }
                // パック/カテゴリなどプレイ不可コンテキストを除外（levelId/hashどちらも欠落ならスキップ）
                bool hasPlayableId = !string.IsNullOrEmpty(levelId) && !levelId.Equals("unknown", StringComparison.OrdinalIgnoreCase);
                bool hasPlayableHash = !string.IsNullOrEmpty(hash) && !hash.Equals("unknown", StringComparison.OrdinalIgnoreCase);
                if (!hasPlayableId && !hasPlayableHash)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: Skipping non-playable context (no levelId/hash)");
                    return;
                }
                if (string.IsNullOrEmpty(key))
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: Key resolve failed; proceeding with fallback key");
                    key = $"{NormalizeUnknown(songName)}|{NormalizeUnknown(songAuthor)}";
                }

                // 無意味なキーは呼び出さない（unknownファイルの氾濫防止）
                if (key.Equals("unknown", StringComparison.OrdinalIgnoreCase) || key.Equals("unknown|unknown", StringComparison.OrdinalIgnoreCase))
                {
                    MapMemo.Plugin.Log?.Warn($"MapMemo: Suppressing SelectionHook due to non-meaningful key='{key}'");
                    return;
                }

                MapMemo.Plugin.Log?.Info($"MapMemo: Calling SelectionHook with parent='{parent.name}', key='{key}'");
                MapMemo.UI.SelectionHook.OnSongSelected(parent, key, NormalizeUnknown(songName), NormalizeUnknown(songAuthor)).ConfigureAwait(false);
                MapMemo.Plugin.Log?.Info("MapMemo: Postfix finished");
            }
            catch (Exception e)
            {
                MapMemo.Plugin.Log?.Error($"Harmony Postfix error: {e}");
            }
        }

        // Level オブジェクト（IPreviewBeatmapLevel/IBeatmapLevel相当）を __instance から探索
        private static object TryGetLevel(object obj)
        {
            var t = obj.GetType();
            // フィールドを探索
            var f = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(x => x.FieldType.Name.IndexOf("BeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.FieldType.Name.IndexOf("IPreviewBeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.FieldType.Name.IndexOf("IBeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.Name.IndexOf("previewBeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.Name.IndexOf("selectedLevel", StringComparison.OrdinalIgnoreCase) >= 0);
            if (f != null)
            {
                var v = f.GetValue(obj);
                if (v != null && v.GetType().Name.IndexOf("BeatmapLevelsModel", StringComparison.OrdinalIgnoreCase) < 0) return v;
            }
            // プロパティを探索
            var p = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(x => x.PropertyType.Name.IndexOf("BeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.PropertyType.Name.IndexOf("IPreviewBeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.PropertyType.Name.IndexOf("IBeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.Name.IndexOf("previewBeatmapLevel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     x.Name.IndexOf("selectedLevel", StringComparison.OrdinalIgnoreCase) >= 0);
            if (p != null)
            {
                var v = p.GetValue(obj);
                if (v != null && v.GetType().Name.IndexOf("BeatmapLevelsModel", StringComparison.OrdinalIgnoreCase) < 0) return v;
            }
            // BeatmapLevelsModel が来た場合は無視し、上位コントローラ経路へ誘導
            if (t.Name.IndexOf("BeatmapLevelsModel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                MapMemo.Plugin.Log?.Info("MapMemo: TryGetLevel found BeatmapLevelsModel; skipping to controller-based extraction");
            }
            return null;
        }

        private static string NormalizeUnknown(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            var trimmed = s.Trim();
            if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "unknown";
            if (trimmed.Equals("!Not Defined!", StringComparison.OrdinalIgnoreCase)) return "unknown";
            return trimmed;
        }

        // タイトル/作者の推測（UIテキストから）
        private static void TryInferTitleAuthor(Transform root, ref string songName, ref string songAuthor)
        {
            try
            {
                var texts = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                if (string.IsNullOrEmpty(songName))
                {
                    var titleCandidate = texts.FirstOrDefault(x => (x.name?.IndexOf("SongName", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) ??
                                          texts.OrderByDescending(x => x.fontSize).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.text));
                    if (titleCandidate != null) songName = titleCandidate.text;
                }
                if (string.IsNullOrEmpty(songAuthor))
                {
                    var authorCandidate = texts.FirstOrDefault(x => (x.name?.IndexOf("Author", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                                                                     (x.text?.IndexOf("by ", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
                    if (authorCandidate != null)
                    {
                        var txt = authorCandidate.text;
                        var idx = txt.IndexOf("by ", StringComparison.OrdinalIgnoreCase);
                        songAuthor = idx >= 0 ? txt.Substring(idx + 3).Trim() : txt;
                    }
                }
            }
            catch { }
        }

        // StandardLevelDetailViewの非公開フィールドからTextを直接引く
        private static void TryGetFromDetailViewFields(Transform root, ref string songName, ref string songAuthor, ref string levelId, ref string hash)
        {
            try
            {
                var view = root.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "StandardLevelDetailView");
                if (view == null) return;
                var fields = view.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    object val = null;
                    try { val = f.GetValue(view); } catch { }
                    if (val == null) continue;
                    // TextMeshProUGUI フィールドからテキスト抽出
                    if (val is TMPro.TextMeshProUGUI tmp)
                    {
                        var nameLower = f.Name.ToLowerInvariant();
                        if (string.IsNullOrEmpty(songName) && (nameLower.Contains("songname") || nameLower.Contains("levelname") || nameLower.Contains("title")))
                        {
                            songName = tmp.text;
                        }
                        if (string.IsNullOrEmpty(songAuthor) && (nameLower.Contains("author") || nameLower.Contains("mapper") || nameLower.Contains("creator")))
                        {
                            songAuthor = tmp.text;
                        }
                    }
                    // 文字列フィールドからID/ハッシュを抽出
                    else if (val is string s)
                    {
                        var nameLower = f.Name.ToLowerInvariant();
                        if (string.IsNullOrEmpty(levelId) && nameLower.Contains("levelid")) levelId = s;
                        if (string.IsNullOrEmpty(hash) && (nameLower.Contains("hash") || nameLower.Contains("levelhash"))) hash = s;
                    }
                }
            }
            catch { }
        }

        // StandardLevelDetailViewController.selectedDifficultyBeatmap?.level から取得（Zenject未使用のためリフレクション）
        private static void TryGetFromDetailViewController(Transform root, ref string songName, ref string songAuthor, ref string levelId, ref string hash)
        {
            try
            {
                var ctrl = root.GetComponentsInParent<Component>(true).FirstOrDefault(c => c.GetType().Name == "StandardLevelDetailViewController") ??
                           root.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "StandardLevelDetailViewController") ??
                           Resources.FindObjectsOfTypeAll<Component>().FirstOrDefault(c => c.GetType().Name == "StandardLevelDetailViewController");
                if (ctrl == null)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: DetailViewController not found in parents or Resources");
                    return;
                }
                var t = ctrl.GetType();
                object sdb = null;
                var p = t.GetProperty("selectedDifficultyBeatmap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null) { try { sdb = p.GetValue(ctrl); } catch { } }
                if (sdb == null)
                {
                    var f = t.GetField("selectedDifficultyBeatmap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null) { try { sdb = f.GetValue(ctrl); } catch { } }
                }
                if (sdb == null)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: selectedDifficultyBeatmap was null on DetailViewController");
                    return;
                }
                var sdbType = sdb.GetType();
                object level = null;
                var levelProp = sdbType.GetProperty("level", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (levelProp != null) { try { level = levelProp.GetValue(sdb); } catch { } }
                if (level == null)
                {
                    var levelField = sdbType.GetField("level", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (levelField != null) { try { level = levelField.GetValue(sdb); } catch { } }
                }
                if (level == null)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: level was null on selectedDifficultyBeatmap");
                    return;
                }
                songName = string.IsNullOrEmpty(songName) ? TryGetStringProp(level, new[] { "songName", "_songName", "Name" }) ?? songName : songName;
                songAuthor = string.IsNullOrEmpty(songAuthor) ? TryGetStringProp(level, new[] { "songAuthorName", "_songAuthorName", "AuthorName" }) ?? songAuthor : songAuthor;
                levelId = string.IsNullOrEmpty(levelId) ? TryGetStringProp(level, new[] { "levelID", "_levelID", "LevelID" }) ?? levelId : levelId;
                hash = string.IsNullOrEmpty(hash) ? TryGetStringProp(level, new[] { "levelHash", "_levelHash", "hash", "LevelHash" }) ?? hash : hash;
                MapMemo.Plugin.Log?.Info($"MapMemo: DetailViewController extraction name='{songName}' author='{songAuthor}' levelId='{levelId}' hash='{hash}'");
            }
            catch { }
        }

        // LevelSelectionNavigationController.selectedBeatmapLevel から取得
        private static void TryGetFromNavigationController(Transform root, ref string songName, ref string songAuthor, ref string levelId, ref string hash)
        {
            try
            {
                var ctrl = root.GetComponentsInParent<Component>(true).FirstOrDefault(c => c.GetType().Name == "LevelSelectionNavigationController") ??
                           Resources.FindObjectsOfTypeAll<Component>().FirstOrDefault(c => c.GetType().Name == "LevelSelectionNavigationController");
                if (ctrl == null) return;
                var t = ctrl.GetType();
                object beatmap = null;
                // プロパティ候補
                var p = t.GetProperty("selectedBeatmapLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        t.GetProperty("selectedPreviewBeatmapLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        t.GetProperty("selectedLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        t.GetProperty("currentLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null) { try { beatmap = p.GetValue(ctrl); } catch { } }
                if (beatmap == null)
                {
                    // フィールド候補
                    var f = t.GetField("selectedBeatmapLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                            t.GetField("selectedPreviewBeatmapLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                            t.GetField("selectedLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                            t.GetField("currentLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null) { try { beatmap = f.GetValue(ctrl); } catch { } }
                }
                if (beatmap == null)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: selectedBeatmapLevel/selectedLevel was null on LevelSelectionNavigationController");
                    // 診断用にフィールド/プロパティ名を列挙
                    try
                    {
                        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name);
                        var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name);
                        MapMemo.Plugin.Log?.Info($"MapMemo: LevelSelectionNavigationController fields: {string.Join(", ", fields)}");
                        MapMemo.Plugin.Log?.Info($"MapMemo: LevelSelectionNavigationController props: {string.Join(", ", props)}");
                    }
                    catch { }
                    return;
                }
                // 候補名で取得
                songName = string.IsNullOrEmpty(songName) ? TryGetStringProp(beatmap, new[] { "songName", "_songName", "Name", "SongName" }) ?? songName : songName;
                songAuthor = string.IsNullOrEmpty(songAuthor) ? TryGetStringProp(beatmap, new[] { "songAuthorName", "_songAuthorName", "AuthorName", "songAuthor" }) ?? songAuthor : songAuthor;
                levelId = string.IsNullOrEmpty(levelId) ? TryGetStringProp(beatmap, new[] { "levelID", "_levelID", "LevelID", "levelId" }) ?? levelId : levelId;
                hash = string.IsNullOrEmpty(hash) ? TryGetStringProp(beatmap, new[] { "levelHash", "_levelHash", "hash", "LevelHash" }) ?? hash : hash;
                MapMemo.Plugin.Log?.Info($"MapMemo: NavigationController extraction name='{songName}' author='{songAuthor}' levelId='{levelId}' hash='{hash}'");
            }
            catch { }
        }

        // LevelCollectionNavigationController.beatmapLevel/beatmapKey から取得
        private static void TryGetFromLevelCollectionController(Transform root, ref string songName, ref string songAuthor, ref string levelId, ref string hash)
        {
            try
            {
                var ctrl = Resources.FindObjectsOfTypeAll<Component>().FirstOrDefault(c => c.GetType().Name == "LevelCollectionNavigationController") ??
                           root.GetComponentsInParent<Component>(true).FirstOrDefault(c => c.GetType().Name == "LevelCollectionNavigationController");
                if (ctrl == null)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: LevelCollectionNavigationController not found");
                    return;
                }
                var t = ctrl.GetType();
                object beatmapLevel = null;
                object beatmapKey = null;
                // まずプロパティを試す
                var pLevel = t.GetProperty("beatmapLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var pKey = t.GetProperty("beatmapKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pLevel != null) { try { beatmapLevel = pLevel.GetValue(ctrl); } catch { } }
                if (pKey != null) { try { beatmapKey = pKey.GetValue(ctrl); } catch { } }
                // フィールドのフォールバック
                if (beatmapLevel == null)
                {
                    var fLevel = t.GetField("beatmapLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fLevel != null) { try { beatmapLevel = fLevel.GetValue(ctrl); } catch { } }
                }
                if (beatmapKey == null)
                {
                    var fKey = t.GetField("beatmapKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fKey != null) { try { beatmapKey = fKey.GetValue(ctrl); } catch { } }
                }
                if (beatmapLevel == null)
                {
                    MapMemo.Plugin.Log?.Warn("MapMemo: LevelCollectionNavigationController.beatmapLevel is null");
                    // 診断用にフィールド/プロパティ名を列挙
                    try
                    {
                        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name);
                        var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name);
                        MapMemo.Plugin.Log?.Info($"MapMemo: LevelCollectionNavigationController fields: {string.Join(", ", fields)}");
                        MapMemo.Plugin.Log?.Info($"MapMemo: LevelCollectionNavigationController props: {string.Join(", ", props)}");
                    }
                    catch { }
                    return;
                }
                // BeatmapLevel から直接取得
                songName = string.IsNullOrEmpty(songName) ? TryGetStringProp(beatmapLevel, new[] { "songName" }) ?? songName : songName;
                songAuthor = string.IsNullOrEmpty(songAuthor) ? TryGetStringProp(beatmapLevel, new[] { "songAuthorName" }) ?? songAuthor : songAuthor;
                levelId = string.IsNullOrEmpty(levelId) ? TryGetStringProp(beatmapLevel, new[] { "levelID", "levelId", "LevelID" }) ?? levelId : levelId;
                // BeatmapKeyからhashは直接は取れないことが多いが、文字列化や拡張メンバーがあれば拾う
                if (beatmapKey != null && string.IsNullOrEmpty(hash))
                {
                    hash = TryGetStringProp(beatmapKey, new[] { "hash", "levelHash" }) ?? hash;
                }
                MapMemo.Plugin.Log?.Info($"MapMemo: LevelCollectionController extraction name='{songName}' author='{songAuthor}' levelId='{levelId}' hash='{hash}'");
            }
            catch { }
        }

        // UIの特定コンポーネントからタイトル/作者を推論
        private static void TryInferFromLevelBarAndParams(Transform root, ref string songName, ref string songAuthor)
        {
            try
            {
                var levelBar = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("LevelBarBig", StringComparison.OrdinalIgnoreCase));
                if (levelBar != null && string.IsNullOrEmpty(songName))
                {
                    var texts = levelBar.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(x => !string.IsNullOrWhiteSpace(x.text)).ToList();
                    var title = texts.OrderByDescending(x => x.fontSize).FirstOrDefault();
                    if (title != null)
                    {
                        songName = title.text.Trim();
                        MapMemo.Plugin.Log?.Info($"MapMemo: Inferred title from LevelBarBig => '{songName}'");
                    }
                }
                var paramsPanel = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.IndexOf("BeatmapParamsPanel", StringComparison.OrdinalIgnoreCase) >= 0);
                if (paramsPanel != null && string.IsNullOrEmpty(songAuthor))
                {
                    var texts = paramsPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(x => !string.IsNullOrWhiteSpace(x.text)).ToList();
                    var authorCandidate = texts.FirstOrDefault(x => (x.name?.IndexOf("Author", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 || (x.text?.IndexOf("Mapper", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 || (x.text?.IndexOf("by ", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
                    if (authorCandidate != null)
                    {
                        var txt = authorCandidate.text.Trim();
                        var idx = txt.IndexOf("by ", StringComparison.OrdinalIgnoreCase);
                        songAuthor = idx >= 0 ? txt.Substring(idx + 3).Trim() : txt;
                        MapMemo.Plugin.Log?.Info($"MapMemo: Inferred author from BeatmapParamsPanel => '{songAuthor}'");
                    }
                }
            }
            catch { }
        }

        private static Transform FindDescriptionParent(Transform root, string songNameHint, string songAuthorHint)
        {
            // まずは StandardLevelDetailView 内の説明テキストをリフレクションで特定して、その親を優先
            try
            {
                var view = root.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "StandardLevelDetailView");
                if (view != null)
                {
                    var tmpField = view.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                        .FirstOrDefault(f => typeof(TMPro.TextMeshProUGUI).IsAssignableFrom(f.FieldType) && f.Name.IndexOf("description", StringComparison.OrdinalIgnoreCase) >= 0);
                    var tmp = tmpField?.GetValue(view) as TMPro.TextMeshProUGUI;
                    if (tmp != null)
                    {
                        var p = tmp.transform.parent ?? root;
                        if ((p.name.Equals("BeatmapLevelVersions", StringComparison.OrdinalIgnoreCase) || p.name.Equals("SongArtwork", StringComparison.OrdinalIgnoreCase)) && p.parent != null)
                        {
                            MapMemo.Plugin.Log?.Info($"MapMemo: Reflection parent is '{p.name}'; using parent '{p.parent.name}'");
                            return p.parent;
                        }
                        MapMemo.Plugin.Log?.Info($"MapMemo: Using reflection text parent '{p.name}'");
                        return p;
                    }
                }
            }
            catch (Exception e)
            {
                MapMemo.Plugin.Log?.Warn($"MapMemo: Reflection parent failed {e.Message}");
            }
            // 強化: 右側詳細ビュー内の典型ノード名を広く探索
            string[] names = {
                "Description",
                "LevelDescription",
                "Info",
                "Details",
                "RightPanel",
                "LevelDetails",
                "LevelDetailView",
                "StandardLevelDetailView",
                "LevelInfo",
                "BeatmapInfo",
                "DetailsText",
                "SongDetails"
            };
            foreach (var n in names)
            {
                var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
                if (t != null)
                {
                    // BeatmapLevelVersions はバージョンリストのコンテナで、UIが隠れることがあるため一つ上を使う
                    if (t.name.Equals("BeatmapLevelVersions", StringComparison.OrdinalIgnoreCase) && t.parent != null)
                    {
                        MapMemo.Plugin.Log?.Info($"MapMemo: Found 'BeatmapLevelVersions'; using parent '{t.parent.name}'");
                        return t.parent;
                    }
                    MapMemo.Plugin.Log?.Info($"MapMemo: Found parent by name '{t.name}'");
                    return t;
                }
            }

            // 説明テキスト候補を優先して親を取得
            var texts = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            var descText = texts.FirstOrDefault(x =>
                (x.name?.IndexOf("Description", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (!string.IsNullOrEmpty(songNameHint) && (x.text?.IndexOf(songNameHint, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) ||
                (!string.IsNullOrEmpty(songAuthorHint) && (x.text?.IndexOf(songAuthorHint, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
            );
            if (descText != null)
            {
                var p = descText.transform.parent ?? root;
                // NG: 'BeatmapLevelVersions' や 'SongArtwork' は右側の説明領域ではない
                if ((p.name.Equals("BeatmapLevelVersions", StringComparison.OrdinalIgnoreCase) || p.name.Equals("SongArtwork", StringComparison.OrdinalIgnoreCase)) && p.parent != null)
                {
                    MapMemo.Plugin.Log?.Info($"MapMemo: Text parent is '{p.name}'; using parent '{p.parent.name}'");
                    p = p.parent;
                }
                MapMemo.Plugin.Log?.Info($"MapMemo: Using text parent '{p.name}'");
                return p;
            }

            // 最終フォールバック: 右側にあるRectTransformの深い方を選ぶ
            // 右パネルでよく使われる VerticalLayoutGroup を持つコンテナを選ぶ
            var rects = root.GetComponentsInChildren<RectTransform>(true);
            var layoutCandidates = rects.Where(r => r.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null).ToList();
            var withText = layoutCandidates.FirstOrDefault(r => r.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Length > 0 && !r.name.Equals("SongArtwork", StringComparison.OrdinalIgnoreCase));
            if (withText != null)
            {
                MapMemo.Plugin.Log?.Info($"MapMemo: Fallback layout parent '{withText.name}'");
                return withText;
            }
            var deepRight = rects.OrderByDescending(r => r.GetComponentsInParent<Transform>(true).Length).FirstOrDefault(r => !r.name.Equals("SongArtwork", StringComparison.OrdinalIgnoreCase));
            if (deepRight != null)
            {
                MapMemo.Plugin.Log?.Info($"MapMemo: Fallback parent '{deepRight.name}'");
                return deepRight;
            }

            MapMemo.Plugin.Log?.Warn("MapMemo: Could not find description parent; using root");
            return root;
        }

        private static string TryGetStringProp(object obj, string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            // まずは完全一致（ケースそのまま）
            foreach (var n in names)
            {
                var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(string))
                {
                    var val = f.GetValue(obj) as string;
                    MapMemo.Plugin.Log?.Info($"MapMemo: Matched field '{f.Name}' on '{t.FullName}' => '{val ?? "<null>"}'");
                    return val;
                }
                var p = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(string))
                {
                    string val = null;
                    try { val = p.GetValue(obj) as string; } catch { }
                    MapMemo.Plugin.Log?.Info($"MapMemo: Matched property '{p.Name}' on '{t.FullName}' => '{val ?? "<null>"}'");
                    return val;
                }
            }
            // ケースインセンシティブの部分一致も試す
            try
            {
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              .Where(f => f.FieldType == typeof(string)).ToList();
                var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              .Where(p => p.PropertyType == typeof(string)).ToList();
                foreach (var f in fields)
                {
                    var nameLower = f.Name.ToLowerInvariant();
                    if (names.Any(n => nameLower.Contains(n.ToLowerInvariant())))
                    {
                        var val = f.GetValue(obj) as string;
                        MapMemo.Plugin.Log?.Info($"MapMemo: Partial field match '{f.Name}' on '{t.FullName}' => '{val ?? "<null>"}'");
                        return val;
                    }
                }
                foreach (var p in props)
                {
                    var nameLower = p.Name.ToLowerInvariant();
                    if (names.Any(n => nameLower.Contains(n.ToLowerInvariant())))
                    {
                        string val = null;
                        try { val = p.GetValue(obj) as string; } catch { }
                        MapMemo.Plugin.Log?.Info($"MapMemo: Partial property match '{p.Name}' on '{t.FullName}' => '{val ?? "<null>"}'");
                        return val;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
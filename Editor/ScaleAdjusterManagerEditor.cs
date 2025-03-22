using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;

namespace jp.illusive_isc
{
    [CustomEditor(typeof(IllScaleAdjusterManager))]
    public class ScaleAdjusterManagerEditor : Editor
    {
        private IllScaleAdjusterManager script;
        private Transform avatarRoot;
        private Transform armature;

        private void OnEnable()
        {
            script = (IllScaleAdjusterManager)target;
            if (!script.transform.root.Equals(avatarRoot))
            {
                script.scaleAdjusterSetupList = new();
                script.scaleAdjusterMainList = new();
                script.mergeArmatureObjList = new();
                avatarRoot = script.transform.root;
                script.scaleAdjusterSetupList = SetupScaleList();
            }

            armature = avatarRoot
                .GetComponent<Animator>()
                .GetBoneTransform(HumanBodyBones.Hips)
                .parent;
            RefreshLists();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Scale Adjuster List", EditorStyles.boldLabel);
            if (!(script.scaleAdjusterMainList.Count() > 0))
            {
                if (GUILayout.Button("スケール設定"))
                {
                    SetupScaleAdjuster();
                    RefreshLists();
                }
                foreach (
                    KeyValuePair<
                        string,
                        IllScaleAdjusterManager.MAScaleAdjusterSetupInfo
                    > v in script.scaleAdjusterSetupList
                )
                {
                    var Info = v.Value;
                    EditorGUI.BeginChangeCheck();
                    bool newToggle = EditorGUILayout.Toggle(
                        v.Key,
                        Info.setUpFlg,
                        GUILayout.Width(20)
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(script, "Toggle変更");
                        Info.setUpFlg = newToggle;
                        EditorUtility.SetDirty(script);
                    }
                }

                return;
            }
            foreach (
                KeyValuePair<string, ModularAvatarScaleAdjuster> v in script.scaleAdjusterMainList
            )
            {
                var scaleAdjuster = v.Value;
                scaleAdjuster.Scale = EditorGUILayout.Vector3Field(v.Key, scaleAdjuster.Scale);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", EditorStyles.boldLabel);
            foreach (var mergeInfo in script.mergeArmatureObjList)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"{mergeInfo.Value.MAMergeArmature.transform.parent.name}"
                );
                if (GUILayout.Button("体のScale Adjusterをコピー"))
                    CopyScaleAdjuster(mergeInfo.Value);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Scale Adjuster List");
                SyncScaleAdjuster(mergeInfo.Value);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void SetupScaleAdjuster()
        {
            foreach (
                KeyValuePair<
                    string,
                    IllScaleAdjusterManager.MAScaleAdjusterSetupInfo
                > v in script.scaleAdjusterSetupList
            )
            {
                if (v.Value.setUpFlg)
                {
                    var scaleAdjuster =
                        v.Value.setUpTransform.gameObject.AddComponent<ModularAvatarScaleAdjuster>();
                    scaleAdjuster.Scale = Vector3.one;
                }
            }
        }

        private Dictionary<
            string,
            IllScaleAdjusterManager.MAScaleAdjusterSetupInfo
        > SetupScaleList()
        {
            return avatarRoot
                .GetComponent<Animator>()
                .GetBoneTransform(HumanBodyBones.Hips)
                .GetComponentsInChildren<Transform>()
                .ToDictionary(
                    adjuster => adjuster.name,
                    adjuster => new IllScaleAdjusterManager.MAScaleAdjusterSetupInfo(
                        adjuster,
                        false
                    )
                );
        }

        private void RefreshLists()
        {
            script.scaleAdjusterMainList = armature
                .GetComponentsInChildren<ModularAvatarScaleAdjuster>()
                .ToDictionary(
                    adjuster => RuntimeUtil.RelativePath(armature.gameObject, adjuster.gameObject),
                    adjuster => adjuster
                );

            var mergeArmature = avatarRoot.GetComponentsInChildren<ModularAvatarMergeArmature>();
            var tmpMergeArmatureObjList =
                script.mergeArmatureObjList
                ?? new Dictionary<string, IllScaleAdjusterManager.MergeArmatureInfo>();

            var newMergeArmatureObjList = mergeArmature.ToDictionary(
                component => component.transform.parent.name,
                component => new IllScaleAdjusterManager.MergeArmatureInfo(
                    component,
                    new Dictionary<string, IllScaleAdjusterManager.MAScaleAdjusterInfo>()
                )
            );

            foreach (var kvp in newMergeArmatureObjList)
            {
                if (!tmpMergeArmatureObjList.ContainsKey(kvp.Key))
                    tmpMergeArmatureObjList.Add(kvp.Key, kvp.Value);
            }

            script.mergeArmatureObjList = tmpMergeArmatureObjList
                .Where(pair => pair.Value.MAMergeArmature != null)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private void CopyScaleAdjuster(IllScaleAdjusterManager.MergeArmatureInfo info)
        {
            var outfitArmature = info.MAMergeArmature.gameObject;
            var avatarArmature = armature.gameObject;
            Undo.SetCurrentGroupName("Copy Scale Adjuster");
            var idx = Undo.GetCurrentGroup();
            foreach (var avatarAdjuster in script.scaleAdjusterMainList)
            {
                var relativePath = RuntimeUtil.RelativePath(
                    avatarArmature,
                    avatarAdjuster.Value.gameObject
                );
                var outfitTransform = outfitArmature.transform.Find(relativePath);

                if (outfitTransform != null)
                {
                    if (
                        outfitTransform.TryGetComponent(
                            out ModularAvatarScaleAdjuster copyComponent
                        )
                    )
                    {
                        Undo.RecordObject(copyComponent, "Copy Serialized");
                    }
                    else
                    {
                        copyComponent = Undo.AddComponent<ModularAvatarScaleAdjuster>(
                            outfitTransform.gameObject
                        );
                    }
                    EditorUtility.CopySerializedIfDifferent(avatarAdjuster.Value, copyComponent);
                }
            }
            Undo.CollapseUndoOperations(idx);
        }

        private void SyncScaleAdjuster(IllScaleAdjusterManager.MergeArmatureInfo info)
        {
            var outfitArmature = info.MAMergeArmature.gameObject;
            var avatarArmature = armature.gameObject;
            Undo.SetCurrentGroupName("Sync Scale Adjuster");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var avatarAdjuster in script.scaleAdjusterMainList)
            {
                var relativePath = RuntimeUtil.RelativePath(
                    avatarArmature,
                    avatarAdjuster.Value.gameObject
                );
                var outfitTransform = outfitArmature.transform.Find(relativePath);
                if (outfitTransform == null)
                    continue;

                if (
                    !outfitTransform.TryGetComponent<ModularAvatarScaleAdjuster>(
                        out var scaleAdjuster
                    )
                )
                    continue;

                if (!info.scaleAdjuster.ContainsKey(avatarAdjuster.Key))
                {
                    info.scaleAdjuster.Add(
                        avatarAdjuster.Key,
                        new IllScaleAdjusterManager.MAScaleAdjusterInfo(
                            null,
                            !avatarAdjuster.Value.Scale.Equals(scaleAdjuster.Scale)
                        )
                    );
                }
                Undo.RecordObject(scaleAdjuster, "Sync Scale Adjuster");

                EditorGUILayout.BeginHorizontal();
                var overrideFlg = EditorGUILayout.Toggle(
                    info.scaleAdjuster[avatarAdjuster.Key].overrideFlg,
                    GUILayout.Width(20)
                );
                info.scaleAdjuster[avatarAdjuster.Key].overrideFlg = overrideFlg;

                GUI.enabled = overrideFlg;
                if (!overrideFlg)
                {
                    EditorUtility.CopySerializedIfDifferent(avatarAdjuster.Value, scaleAdjuster);
                }
                Undo.RecordObject(scaleAdjuster, "Sync Scale Adjuster Scale Edit");

                scaleAdjuster.Scale = EditorGUILayout.Vector3Field(
                    relativePath,
                    scaleAdjuster.Scale
                );

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        // 右クリック時に表示されるメニュー項目を定義
        [MenuItem("GameObject/illusive_tools/Add ScaleAdjusterManager", false, 10)]
        static void AttachObject(MenuCommand menuCommand)
        {
            // 右クリックした対象のGameObjectを取得
            GameObject selectedObject = menuCommand.context as GameObject;
            if (selectedObject == null)
            {
                Debug.LogError("対象のGameObjectが選択されていません");
                return;
            }

            // パッケージ配下のPrefabのパス（必要に応じてパスを修正してください）
            string prefabPath =
                "Packages/jp.illusive-isc.scale-adjuster-manager/ScaleAdjusterManager.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Prefabが以下のパスに見つかりません: " + prefabPath);
                return;
            }

            // Prefabを選択されたGameObjectの子としてインスタンス化
            GameObject instance = (GameObject)
                PrefabUtility.InstantiatePrefab(prefab, selectedObject.transform);
            // ローカルのTransformをリセット（必要に応じて変更してください）
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Undo登録（エディター上での操作をUndoできるように）
            Undo.RegisterCreatedObjectUndo(instance, "illusive_tools/Add ScaleAdjusterManager");

            // 新たに生成したインスタンスを選択状態にする
            Selection.activeGameObject = instance;
        }

        // メニュー項目を有効にするかのチェック
        [MenuItem("GameObject/illusive_tools/Add ScaleAdjusterManager", true)]
        static bool ValidateAttachObject()
        {
            return Selection.activeGameObject != null;
        }
    }
}

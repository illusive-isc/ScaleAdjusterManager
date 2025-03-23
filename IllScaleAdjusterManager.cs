using System;
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using VRC.SDKBase;

namespace jp.illusive_isc
{
    [AddComponentMenu("ScaleAdjusterManager")]
    public class IllScaleAdjusterManager : MonoBehaviour, IEditorOnly
    {
        public Dictionary<string, MAScaleAdjusterSetupInfo> scaleAdjusterSetupList;
        public Dictionary<string, ModularAvatarScaleAdjuster> scaleAdjusterMainList;
        public Dictionary<string, MergeArmatureInfo> mergeArmatureObjList;

        [Serializable]
        public class MergeArmatureInfo
        {
            public ModularAvatarMergeArmature MAMergeArmature;
            public Dictionary<string, MAScaleAdjusterInfo> scaleAdjuster;

            public MergeArmatureInfo(
                ModularAvatarMergeArmature armatureObject,
                Dictionary<string, MAScaleAdjusterInfo> scaleAdjuster
            )
            {
                MAMergeArmature = armatureObject;
                this.scaleAdjuster = scaleAdjuster;
            }
        }

        [Serializable]
        public class MAScaleAdjusterInfo
        {
            public ModularAvatarScaleAdjuster scaleAdjuster;
            public bool overrideFlg;

            public MAScaleAdjusterInfo(ModularAvatarScaleAdjuster scaleAdjuster, bool overrideFlg)
            {
                this.scaleAdjuster = scaleAdjuster;
                this.overrideFlg = overrideFlg;
            }
        }

        [Serializable]
        public class MAScaleAdjusterSetupInfo
        {
            public bool setUpFlg;
            public Transform setUpTransform;

            public MAScaleAdjusterSetupInfo(Transform setUpTransform, bool setUpFlg)
            {
                this.setUpTransform = setUpTransform;
                this.setUpFlg = setUpFlg;
            }
        }
    }
}

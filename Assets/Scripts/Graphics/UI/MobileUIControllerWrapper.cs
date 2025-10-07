using UnityEngine;

namespace DLS.Graphics
{
    /// <summary>
    /// Wrapper class to safely access MobileUIController across platforms
    /// </summary>
    public static class MobileUIControllerWrapper
    {
        public static bool IsWrenchToolActive
        {
            get
            {
                #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
                return MobileUIController.Instance != null && MobileUIController.Instance.isWrenchToolActive;
                #else
                return false;
                #endif
            }
        }
        
        public static bool IsBoxSelectToolActive
        {
            get
            {
                #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
                return MobileUIController.Instance != null && MobileUIController.Instance.isBoxSelectToolActive;
                #else
                return false;
                #endif
            }
        }
        
        public static bool IsHintToolActive
        {
            get
            {
                #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
                return MobileUIController.Instance != null && MobileUIController.Instance.isHintToolActive;
                #else
                return false;
                #endif
            }
        }
        
        public static void OnBoxSelectToolPress()
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (MobileUIController.Instance != null)
                MobileUIController.Instance.OnBoxSelectToolPress();
            #endif
        }
        
        public static void HidePlacementButtons()
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (MobileUIController.Instance != null)
                MobileUIController.Instance.HidePlacementButtons();
            #endif
        }
        
        public static void ShowPlacementButtons(System.Action onConfirm, System.Action onCancel)
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (MobileUIController.Instance != null)
                MobileUIController.Instance.ShowPlacementButtons(onConfirm, onCancel);
            #endif
        }
        
        public static void ShowAddWireButtons(System.Action onAddWirePoint, System.Action onCancel)
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (MobileUIController.Instance != null)
                MobileUIController.Instance.ShowAddWireButtons(onAddWirePoint, onCancel);
            #endif
        }
        
        public static void OnWrenchButtonPress()
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (MobileUIController.Instance != null)
                MobileUIController.Instance.OnWrenchButtonPress();
            #endif
        }
    }
}

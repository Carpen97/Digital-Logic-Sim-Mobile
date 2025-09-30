using Firebase;
using Firebase.Auth;
using UnityEngine;

public class FirebaseProbe : MonoBehaviour {
    async void Awake() {
        // Skip Firebase initialization in Editor to avoid DllNotFoundException
        #if UNITY_EDITOR
        Debug.Log("[Firebase] Editor mode - skipping Firebase initialization to avoid DllNotFoundException");
        return;
        #else
        FirebaseApp.LogLevel = LogLevel.Debug; // verbose client logs

        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        Debug.Log($"[Firebase] Dependencies: {status}");
        if (status != DependencyStatus.Available) {
            Debug.LogError("[Firebase] Not Available after fix; check resolver output.");
            return;
        }

        #if UNITY_ANDROID || UNITY_IOS
        try {
            var auth = FirebaseAuth.DefaultInstance;
            Debug.Log("[Firebase] About to sign in anonymously...");
            var res = await auth.SignInAnonymouslyAsync();
            Debug.Log($"[Firebase] Signed in. UID={res.User?.UserId}");
        }
        catch (FirebaseException fe) {
            // FirebaseException often wraps an AuthError code
            Debug.LogError($"[Firebase] Auth error: {fe.Message}");
        }
        catch (System.Exception e) {
            Debug.LogError($"[Firebase] Unexpected sign-in error: {e}");
        }
        #endif
        Debug.Log("[Firebase] Skipping auth in Editor/desktop build.");
        #endif
    }
}

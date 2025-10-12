using Firebase;
using Firebase.Auth;
using UnityEngine;

public class FirebaseProbe : MonoBehaviour {
    async void Awake() {
        // FirebaseProbe is now deprecated in favor of FirebaseBootstrap
        // FirebaseBootstrap handles initialization for all platforms including Editor
        Debug.Log("[Firebase] FirebaseProbe - initialization is now handled by FirebaseBootstrap");
        await System.Threading.Tasks.Task.CompletedTask;
    }
}

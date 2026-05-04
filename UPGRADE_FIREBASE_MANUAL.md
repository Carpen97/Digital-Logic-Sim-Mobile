# Firebase 13.7.0 Upgrade – Manual Steps

**Why:** Fixes the Windows crash (uWS::HttpSocket::upgrade).

1. **Close Unity.**

2. **Backup:** Rename these folders in `Assets/`:
   - `Firebase` → `Firebase_backup`
   - `ExternalDependencyManager` → `ExternalDependencyManager_backup`

3. **Import in Unity:**
   - Re-open Unity.
   - `Assets` → `Import Package` → `Custom Package...`
   - Browse to `firebase_unity_sdk_13.7.0/firebase_unity_sdk/`
   - Import: **FirebaseAuth.unitypackage**, **FirebaseFirestore.unitypackage**, **FirebaseStorage.unitypackage**
   - (Firebase App comes as a dependency)

4. **Resolve dependencies:** EDM4U will prompt; allow it to run.

5. **Re-run CopyGoogleServices** (if you use it): `Assets` → `Google` → `Firebase` → `Copy google-services.json`

6. **Build and test.**

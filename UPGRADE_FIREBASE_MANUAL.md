# Firebase 13.7.0 Upgrade – Manual Steps

**Repo:** `main` ships **Firebase 13.2** Unity plugins (managed DLLs + Android/iOS artifacts) so clones compile on every OS. GitHub rejects the 13.7 desktop native blobs (>100 MB), so **13.7 is optional** and done only on machines that need the crash fix below.

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

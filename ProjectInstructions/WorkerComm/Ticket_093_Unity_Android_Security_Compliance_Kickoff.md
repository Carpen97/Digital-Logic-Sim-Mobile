# Ticket 093: Unity Android security fix – Google Play compliance

You are working on the Digital Logic Sim Mobile Unity project. **Google Play has flagged the app for a critical security vulnerability** affecting Unity 2017.1+ Android builds. The app is currently **non-compliant** with the Device and Network Abuse policy. **Deadline: February 26, 2026** – after that, submissions may be rejected.

## Context

- **Source:** Google Play policy violation email to carpen97@gmail.com (app: Digital Logic Sim Mobile, `com.DavidCarpenfelt.DigitalLogicSimMobile`, version code 15).
- **Issue:** "Security vulnerabilities which can expose user information or damage a user's device" – violation of Device and Network Abuse policy. Linked to a **Unity vulnerability** (command-line arguments allowing loading/execution of native and managed extensions; no evidence of exploitation or user impact).
- **Unity remediation:** Official guide: https://unity.com/security/sept-2025-01/remediation  
  Options: **(1) Rebuild with a patched Unity Editor (recommended)** or **(2) Patch existing builds** with the [Unity Application Patcher](https://on.unity.com/3ISvuri).
- **Project Unity version:** **6000.2.7f2** (see `ProjectSettings/ProjectVersion.txt`). This is Unity 6.

## What to do

1. **Confirm remediation path**
   - Check Unity Download Archive / Hub for a **patched Unity 6 release** that matches or is compatible with 6000.2.x (e.g. latest 6000.2.x patch). Unity states patched editor releases are available from 2019.1 onward.
   - If a patched 6000.2.x (or suitable 6000.x LTS) is available: **recommended** is to install that editor and **rebuild the Android app** (AAB/APK), then re-upload to Play Console.
   - If you cannot rebuild (e.g. no patched editor for this exact version yet): use the **Unity Application Patcher** on the existing Android build (AAB/APK), then re-sign and re-upload. Patcher modifies `libunity.so` and `boot.config` to block the vulnerable code path. See Unity’s “Patch built applications” and “Unity Application Patcher for Android” sections in the remediation guide.

2. **Apply the fix**
   - **Option A (rebuild):** Install patched Unity Editor → open project → build Android (AAB for Play). Ensure version code is incremented (e.g. 16) and version name aligns with project (e.g. 2.1.6.12). Test the build before uploading.
   - **Option B (patcher):** Download Unity Application Patcher from the link above. Run it on the **existing** Android AAB (or APK) that was submitted as version code 15. Follow Unity’s instructions; re-sign the output and upload to Play Console. Optionally bump version code for the patched submission.

3. **Update all release tracks**
   - Google requires: "Update all release types in addition to your production release (for example, the open, closed, and internal test track releases)." So whichever track(s) currently have the vulnerable build (version code 15) must receive the patched/rebuild build.

4. **Document**
   - Note which approach was used (rebuild with patched editor vs Application Patcher), which Unity version/build was used, and that all relevant Play tracks were updated.

## Success criteria

- The vulnerability is addressed by either rebuilding with a patched Unity Editor or patching the existing Android build with the Unity Application Patcher.
- A new Android build (replacing version code 15) is produced and uploaded to Google Play Console.
- All release tracks that had the vulnerable build are updated (production and any test tracks).
- By February 26, 2026, the app is compliant so future submissions are not rejected.

## References

- Unity remediation guide: https://unity.com/security/sept-2025-01/remediation  
- Unity Application Patcher: https://on.unity.com/3ISvuri  
- Unity Download Archive: https://unity.com/releases/editor/archive  
- Affected versions list (if needed): https://security-patches.unity.com/bc0977e0-21a9-4f6e-9414-4f44b242110a/affected_versions.txt  

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_093_Unity_Android_Security_Compliance_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.

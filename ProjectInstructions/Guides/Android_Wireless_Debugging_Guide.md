# Android Wireless Debugging Guide

**Purpose:** Connect to your Android phone over Wi‑Fi for testing builds without a USB cable.  
**Requirements:** Android 11+ with Wireless debugging, PC and phone on the same Wi‑Fi network.

---

## ADB Location

ADB is typically at:
```
%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe
```
On this machine: `C:\Users\David\AppData\Local\Android\Sdk\platform-tools\adb.exe`

---

## First-Time Setup (on phone)

1. **Enable Developer options**
   - Settings → About phone → tap **Build number** 7 times

2. **Enable Wireless debugging**
   - Settings → Developer options → **Wireless debugging** → turn ON

---

## Pair & Connect (each session or when ports change)

Ports change when Wireless debugging is toggled or after a restart. Run these from a terminal:

### Step 1: Pair (one-time per session)

1. On phone: **Wireless debugging** → tap **Pair device with pairing code**
2. Note the **IP:port** and **6-digit code** shown
3. Run (replace with your values):
   ```powershell
   & "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" pair 192.168.1.5:35059
   ```
4. When prompted, enter the 6-digit pairing code

   Or pipe the code directly:
   ```powershell
   echo 123456 | & "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" pair 192.168.1.5:35059
   ```

### Step 2: Connect

1. On phone: In **Wireless debugging**, note the **IP address & port** (often different from the pairing port)
2. Run:
   ```powershell
   & "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" connect 192.168.1.5:46401
   ```

### Step 3: Verify

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices
```

You should see your device listed with status `device`.

---

## Reconnecting Later

If you've already paired and the ports haven't changed, you can usually just run:

```powershell
adb connect 192.168.1.5:46401
```

If it fails, repeat the pair + connect steps with the current values from the phone.

---

## Quick Reference

| Action | Command |
|--------|---------|
| Pair | `adb pair <IP>:<pair_port>` then enter code |
| Connect | `adb connect <IP>:<connect_port>` |
| List devices | `adb devices` |
| Disconnect | `adb disconnect 192.168.1.5:46401` |

**Note:** The pairing port (e.g. 35059) and connect port (e.g. 46401) are different. Both are shown in the Wireless debugging screen.

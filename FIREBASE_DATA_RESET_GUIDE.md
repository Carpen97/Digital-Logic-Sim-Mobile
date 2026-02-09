# Firebase Data Reset Guide

## Overview
This guide explains how to reset/clear all Firebase data for Digital Logic Sim, including leaderboards, usernames, and user profiles.

## Firebase Collections Used

Your project uses these Firestore collections:

1. **`scores`** - Leaderboard scores (levelId, userId, userName, score, etc.)
2. **`completeSolutions`** - Complete solutions shared by users
3. **`users`** - User profiles (userId, username, deviceId, timestamps)
4. **`usernames`** - Username reservations (for fast lookup and uniqueness)

## Method 1: Firebase Console (Recommended for Small Datasets)

### Step-by-Step Instructions:

1. **Access Firebase Console**
   - Go to: https://console.firebase.google.com/
   - Select your project (Digital Logic Sim)

2. **Navigate to Firestore Database**
   - Click on **"Firestore Database"** in the left sidebar
   - You'll see all your collections listed

3. **Delete Each Collection**

   **For `scores` collection:**
   - Click on the `scores` collection
   - Click the three dots menu (⋮) next to the collection name
   - Select **"Delete collection"**
   - Confirm the deletion
   - ⚠️ This will delete ALL leaderboard scores for ALL levels

   **For `completeSolutions` collection:**
   - Click on the `completeSolutions` collection
   - Click the three dots menu (⋮)
   - Select **"Delete collection"**
   - Confirm the deletion

   **For `users` collection:**
   - Click on the `users` collection
   - Click the three dots menu (⋮)
   - Select **"Delete collection"**
   - Confirm the deletion

   **For `usernames` collection:**
   - Click on the `usernames` collection
   - Click the three dots menu (⋮)
   - Select **"Delete collection"**
   - Confirm the deletion
   - ⚠️ This will free up all usernames

4. **Verify Deletion**
   - Refresh the Firestore Database page
   - Confirm that all collections are gone

### Important Notes:
- ⚠️ **This action is IRREVERSIBLE** - make a backup first if you might need the data
- Collections will be automatically recreated when users submit new scores/usernames
- Firebase security rules will remain in place

## Method 2: Firebase CLI Script (For Large Datasets or Automation)

If you have a lot of data or want to automate the process:

### 1. Install Firebase CLI
```bash
npm install -g firebase-tools
```

### 2. Login to Firebase
```bash
firebase login
```

### 3. Create a Node.js Script

Create a file called `reset-firestore.js`:

```javascript
const admin = require('firebase-admin');

// Initialize Firebase Admin with your service account
const serviceAccount = require('./path-to-your-service-account-key.json');

admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

const db = admin.firestore();

async function deleteCollection(collectionName) {
  const collectionRef = db.collection(collectionName);
  const query = collectionRef.limit(500);

  return new Promise((resolve, reject) => {
    deleteQueryBatch(db, query, resolve).catch(reject);
  });
}

async function deleteQueryBatch(db, query, resolve) {
  const snapshot = await query.get();

  const batchSize = snapshot.size;
  if (batchSize === 0) {
    // All documents deleted
    resolve();
    return;
  }

  // Delete documents in a batch
  const batch = db.batch();
  snapshot.docs.forEach((doc) => {
    batch.delete(doc.ref);
  });
  await batch.commit();

  // Recurse on the next process tick to avoid exploding the stack
  process.nextTick(() => {
    deleteQueryBatch(db, query, resolve);
  });
}

async function resetAllData() {
  console.log('Starting Firebase data reset...');
  
  try {
    console.log('Deleting scores collection...');
    await deleteCollection('scores');
    console.log('✓ Scores deleted');

    console.log('Deleting completeSolutions collection...');
    await deleteCollection('completeSolutions');
    console.log('✓ Complete solutions deleted');

    console.log('Deleting users collection...');
    await deleteCollection('users');
    console.log('✓ Users deleted');

    console.log('Deleting usernames collection...');
    await deleteCollection('usernames');
    console.log('✓ Usernames deleted');

    console.log('\n✅ All Firebase data has been reset!');
  } catch (error) {
    console.error('Error resetting data:', error);
  }
}

resetAllData().then(() => {
  console.log('Done!');
  process.exit(0);
});
```

### 4. Get Service Account Key

1. In Firebase Console, go to **Project Settings** (gear icon)
2. Click **"Service accounts"** tab
3. Click **"Generate new private key"**
4. Save the JSON file and reference it in the script above

### 5. Install Dependencies and Run

```bash
npm install firebase-admin
node reset-firestore.js
```

## Method 3: Partial Reset (Selective Deletion)

If you only want to reset specific parts:

### Reset Only Leaderboards (Keep Usernames)
- Delete only the `scores` and `completeSolutions` collections
- Keep `users` and `usernames` collections

### Reset Only Usernames (Keep Leaderboards)
- Delete only the `users` and `usernames` collections
- Keep `scores` and `completeSolutions` collections
- ⚠️ Note: Existing scores will have orphaned usernames

### Reset Specific Level Leaderboard
1. Go to Firebase Console → Firestore Database
2. Click on `scores` collection
3. Use the filter: `levelId == "lvl.not.1"` (replace with your level ID)
4. Select all filtered documents
5. Delete them

## After Reset: What Happens?

### For Users:
- ✅ All usernames become available again
- ✅ Users can claim their old username again (or a new one)
- ✅ Leaderboards will be empty
- ✅ Users can submit new scores immediately

### For Your App:
- ✅ No code changes needed
- ✅ Collections will be automatically recreated on first write
- ✅ Firebase security rules remain active
- ✅ All functionality continues to work

## Backup Before Reset (Recommended)

### Option 1: Firebase Console Export
1. Go to Firestore Database
2. Click on a collection
3. Click **"Export"** at the top
4. Choose a Cloud Storage bucket
5. Repeat for each collection

### Option 2: Download All Data via Script

```javascript
async function backupCollection(collectionName) {
  const snapshot = await db.collection(collectionName).get();
  const data = [];
  snapshot.forEach(doc => {
    data.push({ id: doc.id, ...doc.data() });
  });
  
  const fs = require('fs');
  fs.writeFileSync(
    `backup-${collectionName}-${Date.now()}.json`,
    JSON.stringify(data, null, 2)
  );
  console.log(`✓ Backed up ${collectionName} (${data.length} documents)`);
}
```

## Troubleshooting

### "Permission Denied" Error
- Check your Firebase security rules
- Make sure you're logged in as project owner
- Verify service account has correct permissions

### Collection Won't Delete
- Might have subcollections - check for nested data
- Try deleting in smaller batches
- Check for active security rule locks

### Data Reappears After Deletion
- Check for any background sync processes
- Verify you deleted from the correct Firebase project
- Clear app cache/local storage on client devices

## Security Considerations

⚠️ **Before resetting in production:**

1. Announce to users that data will be reset (if applicable)
2. Consider archiving data instead of deleting
3. Test the reset process on a staging/development project first
4. Verify security rules are still working after reset
5. Monitor for any errors after reset

## Quick Reset Checklist

- [ ] Decide which collections to delete
- [ ] Backup data (if needed)
- [ ] Access Firebase Console
- [ ] Delete `scores` collection
- [ ] Delete `completeSolutions` collection
- [ ] Delete `users` collection
- [ ] Delete `usernames` collection
- [ ] Verify deletion
- [ ] Test app functionality
- [ ] Monitor for errors

---

**Need help?** Check the Firebase documentation: https://firebase.google.com/docs/firestore/manage-data/delete-data


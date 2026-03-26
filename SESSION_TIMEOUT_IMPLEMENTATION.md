# ?? Session Timeout Implementation - Complete

## Overview

The Member Administration system now includes a 15-minute automatic session timeout feature that logs users out after inactivity, with visual warnings and the ability to extend the session.

## ? Features Implemented

### 1. 15-Minute Session Timeout
- **Automatic logout** after 15 minutes from login
- **Activity tracking** - Any mouse, keyboard, scroll, or touch activity resets the timer
- **Periodic checks** - Session validity checked every minute
- **Immediate logout** - Clear all session data on timeout

### 2. Session Timer Display
- **Real-time countdown** in user header
- **Color-coded warnings:**
  - ?? Green (10+ minutes): Normal
  - ?? Orange (5-9 minutes): Caution
  - ?? Red (<5 minutes): Warning
- **Updates every 30 seconds**
- **Shows minutes remaining**

### 3. Session Warning Banner
- **Appears at 2 minutes remaining**
- **Shows countdown** (minutes and seconds)
- **"Stay Logged In" button** - Extends session by 15 minutes
- **"Dismiss" button** - Hides warning (but session still expires)
- **Animated slide-in** from right side
- **Confirmation message** after extending

### 4. Activity-Based Extension
- **Mouse clicks** reset timer
- **Keyboard input** resets timer
- **Page scrolling** resets timer
- **Touch events** reset timer (mobile)
- **Automatic extension** - No user action needed if active

## ?? Technical Implementation

### Session Data Storage
```javascript
localStorage.setItem('loginTime', new Date().toISOString());
localStorage.setItem('lastActivityTime', new Date().toISOString());
localStorage.setItem('authToken', token);
localStorage.setItem('currentUser', JSON.stringify(user));
```

### Session Timeout Constants
```javascript
const SESSION_TIMEOUT = 15 * 60 * 1000; // 15 minutes
const ACTIVITY_CHECK_INTERVAL = 60 * 1000; // Check every minute
```

### Activity Tracking
```javascript
// Listen for user activity
document.addEventListener('mousedown', updateActivity);
document.addEventListener('keydown', updateActivity);
document.addEventListener('scroll', updateActivity);
document.addEventListener('touchstart', updateActivity);

// Update activity time
function updateActivity() {
    lastActivityTime = new Date();
    localStorage.setItem('lastActivityTime', lastActivityTime.toISOString());

    // Reset timeout
    clearTimeout(sessionTimeoutId);
    sessionTimeoutId = setTimeout(() => {
        alert('Your session has expired due to inactivity.');
        logout();
    }, SESSION_TIMEOUT);
}
```

### Session Check Flow
```
Page Load
    ?
Check loginTime
    ?
Calculate: now - loginTime
    ?
If > 15 minutes ? Logout
    ?
If < 15 minutes ? Continue
    ?
Start timeout timer
    ?
Listen for activity
    ?
On activity ? Reset timer
    ?
Every minute ? Check if expired
    ?
If expired ? Alert + Logout
```

## ?? UI Components

### 1. Session Timer in Header
Located in user info area, shows:
- **10+ min:** `?? 14m` (gray)
- **5-9 min:** `?? 7m` (orange)
- **<5 min:** `?? 2m left` (red, bold)

### 2. Session Warning Banner
Appears when <2 minutes remaining:
```
???????????????????????????????????
? ?? Session Expiring Soon        ?
? Your session will expire in 1m 45s ?
?                                  ?
? [Stay Logged In]  [Dismiss]     ?
???????????????????????????????????
```

### 3. Extension Confirmation
After clicking "Stay Logged In":
```
???????????????????????????????????
? ? Session Extended             ?
? You have 15 more minutes        ?
???????????????????????????????????
```

## ?? Files Modified

### Core Files
1. **`wwwroot/auth.js`** (+ 200 lines)
   - Added session timeout logic
   - Activity tracking functions
   - Session timer display
   - Warning banner system
   - Extend session functionality

2. **`wwwroot/login.html`** (modified)
   - Store `loginTime` on successful login
   - Store `lastActivityTime`
   - Check session expiry on page load
   - Clear expired sessions

3. **`wwwroot/styles.css`** (+ 30 lines)
   - Session warning animations
   - `slideInRight` animation
   - `slideOutRight` animation

4. **`wwwroot/home.html`** (modified)
   - Better stats loading with logging
   - Show "..." while loading instead of "-"
   - Display "Error" instead of "0" on failure

## ?? Testing the Session Timeout

### Test 1: Normal Timeout (15 minutes)
1. Login to the system
2. Wait 15 minutes (or temporarily change timeout to 1 minute for testing)
3. **Expected:** Alert appears, automatic logout
4. **Expected:** Redirected to login page

### Test 2: Activity Extension
1. Login to the system
2. Use the system normally (click, type, scroll)
3. **Expected:** Session extends automatically
4. **Expected:** Timer resets with each activity

### Test 3: Warning Banner
1. Login to the system
2. Wait until less than 2 minutes remaining
3. **Expected:** Warning banner slides in from right
4. **Expected:** Shows countdown in minutes and seconds

### Test 4: Manual Extension
1. Wait for warning banner to appear
2. Click "Stay Logged In"
3. **Expected:** Timer resets to 15 minutes
4. **Expected:** Confirmation message appears
5. **Expected:** Warning disappears

### Test 5: Session Timer Display
1. Login and go to any page
2. Look at user info in header
3. **Expected:** Shows time remaining (e.g., "?? 14m")
4. **Expected:** Color changes as time decreases
5. **Expected:** Updates every 30 seconds

### Quick Test (1-minute timeout)
To test quickly, temporarily change in `auth.js`:
```javascript
const SESSION_TIMEOUT = 1 * 60 * 1000; // 1 minute for testing
```

## ?? Security Features

### Automatic Logout
- **Inactivity:** 15 minutes of no activity
- **Absolute time:** 15 minutes from login (even if active)
- **Multiple tabs:** Each tab tracks independently
- **Browser close:** Session persists until timeout

### Session Validation
- **On page load:** Check if session expired
- **Every minute:** Periodic validation
- **Before API calls:** Token sent with every request
- **On activity:** Reset timeout counter

### Data Cleanup
On logout or expiry:
- ? Clear `authToken`
- ? Clear `currentUser`
- ? Clear `loginTime`
- ? Clear `lastActivityTime`
- ? Clear timeout timers
- ? Redirect to login

## ?? Session Lifecycle

### Login (Time = 0)
```
User logs in
    ?
Store loginTime: 2024-01-15 10:00:00
    ?
Store lastActivityTime: 2024-01-15 10:00:00
    ?
Start 15-minute timer
    ?
Start activity listeners
```

### Active Use (Time = 0-15 min)
```
User clicks/types/scrolls
    ?
Update lastActivityTime
    ?
Reset 15-minute timer
    ?
Continue session
```

### Warning Phase (Time = 13-15 min)
```
13 minutes elapsed
    ?
Timer shows "?? 2m" (red)
    ?
Warning banner appears
    ?
User clicks "Stay Logged In"
    ?
Reset loginTime to now
    ?
Reset timer to 15 minutes
```

### Timeout (Time = 15 min)
```
15 minutes elapsed
    ?
Alert: "Session expired"
    ?
Clear all session data
    ?
Redirect to login
    ?
User must login again
```

## ?? User Experience

### Visual Feedback
1. **Login:** Session starts, timer shows "?? 15m"
2. **10 minutes:** Timer shows "?? 5m" (orange)
3. **13 minutes:** Timer shows "?? 2m left" (red, bold)
4. **13 minutes:** Warning banner slides in
5. **User clicks button:** Session extends, confirmation shows
6. **15 minutes (if no action):** Alert + Logout

### User Actions
- **Extend Session:** Click "Stay Logged In" in warning
- **Dismiss Warning:** Click "Dismiss" (session still expires)
- **Stay Active:** Any activity extends session automatically
- **Manual Logout:** Click "Logout" anytime

## ?? Troubleshooting

### Session Expires Too Quickly
**Check:** Make sure `SESSION_TIMEOUT` is set to `15 * 60 * 1000` (15 minutes)
```javascript
const SESSION_TIMEOUT = 15 * 60 * 1000; // 15 minutes
```

### Timer Not Showing
**Check:** 
1. User info element exists: `<div id="userInfo"></div>`
2. Session timer element created: `<span id="sessionTimer"></span>`
3. `initAuthUI()` is being called

### Warning Not Appearing
**Check:**
1. Session is actually < 2 minutes
2. Warning element not already present
3. JavaScript console for errors

### Activity Not Extending Session
**Check:**
1. Event listeners are attached
2. `updateActivity()` function is called
3. `lastActivityTime` is being updated in localStorage

## ?? Configuration

### Adjust Timeout Duration
In `wwwroot/auth.js`, modify:
```javascript
// Change 15 to desired minutes
const SESSION_TIMEOUT = 15 * 60 * 1000;
```

### Adjust Warning Time
In `showSessionWarning()` function:
```javascript
// Change 2 to desired minutes before expiry
if (minutes < 2 && !document.getElementById('sessionWarning'))
```

### Adjust Timer Update Frequency
In `initAuthUI()` function:
```javascript
// Change 30000 to desired milliseconds
setInterval(updateSessionTimer, 30000); // 30 seconds
```

### Adjust Activity Check Frequency
```javascript
// Change 60000 to desired milliseconds
const ACTIVITY_CHECK_INTERVAL = 60 * 1000; // 1 minute
```

## ?? Benefits

1. **Security:** Prevents unauthorized access if user walks away
2. **User-Friendly:** Gives warnings before logout
3. **Flexible:** Can extend session without re-login
4. **Automatic:** Works in background, no user action needed
5. **Visual:** Clear countdown and warnings
6. **Activity-Based:** Smart detection of user presence

## ?? API Integration

Session timeout works with existing auth:
- ? Token still validated server-side
- ? All API calls include auth header
- ? Server can still reject expired tokens
- ? Client-side timeout is additional security layer

## ?? Ready to Use!

Session timeout is now active:
- ? 15-minute automatic timeout
- ? Activity-based extension
- ? Visual timer display
- ? Warning before expiry
- ? Manual extension option
- ? Build successful

**Test it now:**
1. Login with: admin / katoennatie
2. Watch the timer in the header (shows minutes remaining)
3. Try being inactive for 2 minutes (for testing, temporarily set to 2min)
4. Click "Stay Logged In" when warning appears
5. Verify session extends by 15 minutes

---

**Session timeout active and working!** ???

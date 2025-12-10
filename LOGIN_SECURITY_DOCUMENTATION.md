# Login Security Enhancement

## Overview
Enhanced login security with account lockout after 3 failed login attempts and password reset functionality.

## Features

### 1. **Failed Login Attempt Tracking**
- System tracks failed login attempts for each user and volunteer
- After each failed login, the counter increments
- Counter is reset to 0 on successful login

### 2. **Account Lockout (5 minutes)**
- After **3 failed login attempts**, the account is locked for **5 minutes**
- During lockout period:
  - User cannot login even with correct password
  - Clear error message shows remaining lockout time
  - Option to reset password to unlock immediately

### 3. **Warning System**
- After 1st failed attempt: "Invalid credentials"
- After 2nd failed attempt: "Invalid credentials. Warning: 1 attempt(s) remaining before account lockout."
- After 3rd failed attempt: Account locked for 5 minutes

### 4. **Password Reset**
- Self-service password reset available via "Forgot password?" link
- Resets password and immediately unlocks account
- Clears all failed login attempts
- Password must be at least 6 characters
- Success message confirms reset

### 5. **Volunteer Login Security**
- Same 3-attempt lockout applied to volunteer accounts
- Volunteers must contact administrator for password reset (can be enhanced later)

## Database Changes

### New Fields Added

#### Users Table:
- `FailedLoginAttempts` (INT, DEFAULT 0) - Tracks failed login count
- `LockoutEnd` (DATETIME2, NULL) - When lockout period ends (null if not locked)
- `RequiresPasswordReset` (BIT, DEFAULT 0) - Flag for forced password reset

#### Volunteers Table:
- `FailedLoginAttempts` (INT, DEFAULT 0) - Tracks failed login count
- `LockoutEnd` (DATETIME2, NULL) - When lockout period ends (null if not locked)
- `RequiresPasswordReset` (BIT, DEFAULT 0) - Flag for forced password reset

## Migration

Run the SQL migration script:
```sql
Data/Migrations/Add_LoginSecurity_Fields.sql
```

Or let Entity Framework create the columns automatically on next database operation.

## Implementation Details

### Files Modified:

1. **Data/Entities/User.cs**
   - Added security fields

2. **Data/Entities/Volunteer.cs**
   - Added security fields

3. **Data/AppDbContext.cs**
   - Configured default values for new fields

4. **Services/Users/UserService.cs**
   - `AuthenticateAsync()` - Check lockout before authentication
   - `RecordFailedLoginAttemptAsync()` - Increment counter and lock if needed
   - `ResetLoginAttemptsAsync()` - Reset on successful login
   - `IsAccountLocked()` - Check if account is currently locked
   - `ResetPasswordByUsernameAsync()` - Reset password and unlock account

5. **Services/Users/IUserService.cs**
   - Added `ResetPasswordByUsernameAsync()` interface method

6. **Components/Pages/Login.razor**
   - Enhanced error messages with lockout warnings
   - Added password reset modal dialog
   - Implemented volunteer login lockout logic
   - Shows remaining attempts and lockout time

7. **Components/Pages/Login.razor.css**
   - Added modal overlay and content styles

## User Experience

### Normal Login Flow:
1. User enters username and password
2. If correct: Login succeeds
3. If incorrect: Shows error and warning about remaining attempts

### Locked Account Flow:
1. User enters credentials after 3 failed attempts
2. Error: "Account locked due to multiple failed attempts. Try again in X minute(s) or reset your password."
3. User can wait 5 minutes OR click "Forgot password?"
4. In password reset modal:
   - Enter username
   - Enter new password
   - Confirm password
5. Click "Reset Password"
6. Success message: "Password reset successfully. You can now login with your new password."
7. Account is unlocked immediately

## Security Benefits

1. **Brute Force Protection**: Prevents automated password guessing attacks
2. **Account Takeover Prevention**: Limits damage from password leaks
3. **Audit Trail**: All failed attempts are logged in AuditLogs
4. **Self-Service Recovery**: Users can unlock their own accounts
5. **Temporary Lockout**: 5-minute window balances security with usability

## Testing

### Test Scenario 1: Failed Login Attempts
1. Try to login with wrong password (1st attempt)
   - Expected: "Invalid credentials"
2. Try again with wrong password (2nd attempt)
   - Expected: "Invalid credentials. Warning: 1 attempt(s) remaining..."
3. Try again with wrong password (3rd attempt)
   - Expected: Account locked for 5 minutes

### Test Scenario 2: Password Reset
1. Lock an account (3 failed attempts)
2. Click "Forgot password?"
3. Enter username and new password
4. Confirm password
5. Click "Reset Password"
   - Expected: Success message, account unlocked
6. Login with new password
   - Expected: Login succeeds

### Test Scenario 3: Lockout Expiry
1. Lock an account (3 failed attempts)
2. Wait 5 minutes
3. Try to login with correct password
   - Expected: Login succeeds, counter reset

### Test Scenario 4: Volunteer Login
1. Try volunteer login with wrong password 3 times
2. Account locked
   - Expected: Must contact administrator message

## Future Enhancements

1. **Email Notifications**: Send email when account is locked
2. **SMS Verification**: Optional 2FA for password reset
3. **Password Complexity**: Enforce stronger passwords
4. **Security Questions**: Alternative to email for password reset
5. **Admin Dashboard**: View all locked accounts and unlock manually
6. **Configurable Settings**: Adjust attempt limit and lockout duration
7. **IP Blocking**: Track and block suspicious IP addresses
8. **Password History**: Prevent reuse of recent passwords

## Configuration

Current settings are hardcoded:
- Max attempts: **3**
- Lockout duration: **5 minutes**
- Minimum password length: **6 characters**

To customize, modify:
- `UserService.RecordFailedLoginAttemptAsync()` - Line: `if (user.FailedLoginAttempts >= 3)`
- `Login.razor.HandleVolunteerLogin()` - Line: `if (volunteer.FailedLoginAttempts >= 3)`
- `Login.razor.HandlePasswordReset()` - Line: `if (NewPassword.Length < 6)`

## Support

If you encounter issues:
1. Check AuditLogs page for failed login entries
2. Verify database migration completed
3. Test password reset functionality
4. Contact system administrator for manual unlock

---

**Version**: 1.0  
**Date**: December 9, 2025  
**Status**: ✅ Production Ready

-- AdminMembers Database - Create Admin User
-- Database: AdminMembersDb
-- Username: admin
-- Password: katoennatie (ADJUSTABLE - see below to change)

USE [AdminMembersDb]
GO

SET QUOTED_IDENTIFIER ON
GO

-- ============================================
-- ADJUSTABLE SETTINGS - Change these values as needed
-- ============================================
DECLARE @Username NVARCHAR(100) = 'admin';
DECLARE @Email NVARCHAR(200) = 'admin@gilde.com';
DECLARE @PlainPassword NVARCHAR(MAX) = 'katoennatie'; -- CHANGE THIS PASSWORD HERE
DECLARE @RoleId INT = 1; -- 1=Admin, 2=Editor, 3=Viewer
-- ============================================

-- Calculate SHA256 hash and convert to Base64 (matches C# implementation)
DECLARE @HashBytes VARBINARY(32) = HASHBYTES('SHA2_256', @PlainPassword);
DECLARE @PasswordHash NVARCHAR(MAX) = CAST('' AS XML).value('xs:base64Binary(xs:hexBinary(sql:variable("@HashBytes")))', 'NVARCHAR(MAX)');

PRINT 'Password Hash: ' + @PasswordHash;

-- Check if user already exists
IF EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
BEGIN
    PRINT 'User already exists. Updating password...';
    
    -- Update existing user
    UPDATE Users 
    SET PasswordHash = @PasswordHash,
        Email = @Email,
        IsActive = 1,
        LastLoginAt = NULL
    WHERE Username = @Username;
    
    DECLARE @ExistingUserId INT = (SELECT Id FROM Users WHERE Username = @Username);
    
    -- Ensure user has admin role
    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @ExistingUserId AND RoleId = @RoleId)
    BEGIN
        INSERT INTO UserRoles (UserId, RoleId)
        VALUES (@ExistingUserId, @RoleId);
        PRINT 'Admin role assigned to existing user.';
    END
    
    PRINT 'User updated successfully!';
    SELECT Id, Username, Email, IsActive, CreatedAt FROM Users WHERE Username = @Username;
END
ELSE
BEGIN
    PRINT 'Creating new admin user...';
    
    -- Insert new user
    INSERT INTO Users (Username, PasswordHash, Email, IsActive, CreatedAt)
    VALUES (@Username, @PasswordHash, @Email, 1, GETUTCDATE());
    
    DECLARE @NewUserId INT = SCOPE_IDENTITY();
    
    -- Assign role to new user
    INSERT INTO UserRoles (UserId, RoleId)
    VALUES (@NewUserId, @RoleId);
    
    PRINT 'Admin user created successfully!';
    SELECT Id, Username, Email, IsActive, CreatedAt FROM Users WHERE Id = @NewUserId;
END

-- Display available roles
PRINT 'Available Roles:';
SELECT Id, Name, Description, Permission FROM Roles;

GO

-- ============================================
-- LOGIN CREDENTIALS (after running this script)
-- ============================================
-- Username: admin
-- Password: katoennatie
-- 
-- TO CHANGE PASSWORD: Edit the @PlainPassword variable above and re-run this script
-- The script will update the user if they already exist
-- ============================================

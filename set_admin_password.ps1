# Quick Admin User Creator/Password Changer for AdminMembers
# This script creates or updates the admin user with adjustable password

param(
    [string]$Username = "admin",
    [string]$Password = "katoennatie",
    [string]$Email = "admin@gilde.com",
    [int]$RoleId = 1  # 1=Admin, 2=Editor, 3=Viewer
)

Write-Host "=== AdminMembers User Management ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Username: $Username" -ForegroundColor Yellow
Write-Host "Email: $Email" -ForegroundColor Yellow
Write-Host "Password: $Password" -ForegroundColor Yellow
Write-Host ""

# Hash password using SHA256 (same as C# AuthService)
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$passwordBytes = [System.Text.Encoding]::UTF8.GetBytes($Password)
$hashBytes = $sha256.ComputeHash($passwordBytes)
$passwordHash = [Convert]::ToBase64String($hashBytes)

Write-Host "Password Hash: $passwordHash" -ForegroundColor Gray
Write-Host ""

# SQL Script
$sql = @"
USE [AdminMembersDb]
GO

SET QUOTED_IDENTIFIER ON
GO

DECLARE @Username NVARCHAR(100) = '$Username';
DECLARE @Email NVARCHAR(200) = '$Email';
DECLARE @PasswordHash NVARCHAR(MAX) = '$passwordHash';
DECLARE @RoleId INT = $RoleId;

IF EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
BEGIN
    PRINT 'User exists. Updating password...';

    UPDATE Users 
    SET PasswordHash = @PasswordHash,
        Email = @Email,
        IsActive = 1
    WHERE Username = @Username;

    DECLARE @UserId INT = (SELECT Id FROM Users WHERE Username = @Username);

    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    BEGIN
        INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
    END

    PRINT 'User updated successfully!';
END
ELSE
BEGIN
    PRINT 'Creating new user...';

    INSERT INTO Users (Username, PasswordHash, Email, IsActive, CreatedAt)
    VALUES (@Username, @PasswordHash, @Email, 1, GETUTCDATE());

    DECLARE @NewUserId INT = SCOPE_IDENTITY();
    INSERT INTO UserRoles (UserId, RoleId) VALUES (@NewUserId, @RoleId);

    PRINT 'User created successfully!';
END

SELECT Id, Username, Email, IsActive FROM Users WHERE Username = @Username;
GO
"@

# Save SQL to temp file
$tempSqlFile = "temp_create_user.sql"
$sql | Out-File -FilePath $tempSqlFile -Encoding UTF8

Write-Host "Executing SQL..." -ForegroundColor Green

# Execute SQL
try {
    $result = sqlcmd -S "(localdb)\mssqllocaldb" -d AdminMembersDb -i $tempSqlFile
    Write-Host $result -ForegroundColor White
    Write-Host ""
    Write-Host "? SUCCESS!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now login with:" -ForegroundColor Cyan
    Write-Host "  Username: $Username" -ForegroundColor White
    Write-Host "  Password: $Password" -ForegroundColor White
}
catch {
    Write-Host "? ERROR: $_" -ForegroundColor Red
}
finally {
    # Clean up temp file
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile
    }
}

Write-Host ""
Write-Host "=== Usage Examples ===" -ForegroundColor Yellow
Write-Host "Change password:" -ForegroundColor White
Write-Host '  .\set_admin_password.ps1 -Password "newpassword"' -ForegroundColor Gray
Write-Host ""
Write-Host "Create different user:" -ForegroundColor White
Write-Host '  .\set_admin_password.ps1 -Username "john" -Password "secret" -Email "john@gilde.com" -RoleId 2' -ForegroundColor Gray
Write-Host ""

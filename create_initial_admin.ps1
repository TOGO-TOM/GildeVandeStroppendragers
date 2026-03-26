# Create Initial Admin User Script
# Run this script after database migration to create your first admin user

# Configuration
$apiUrl = "https://localhost:7223"  # Update with your API URL if different

Write-Host "=== AdminMembers Initial User Setup ===" -ForegroundColor Cyan
Write-Host ""

# Since we don't have a user yet, we'll directly insert into the database
# This is a one-time setup script

$username = Read-Host "Enter admin username"
$password = Read-Host "Enter admin password" -AsSecureString
$passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))
$email = Read-Host "Enter admin email"

# Hash password using SHA256 (same as AuthService)
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$passwordBytes = [System.Text.Encoding]::UTF8.GetBytes($passwordPlain)
$hashBytes = $sha256.ComputeHash($passwordBytes)
$passwordHash = [Convert]::ToBase64String($hashBytes)

# SQL to insert user
$sql = @"
USE [AdminMembers]
GO

-- Insert User
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [Email], [IsActive], [CreatedAt])
VALUES ('$username', '$passwordHash', '$email', 1, GETUTCDATE());

-- Get the user ID
DECLARE @UserId INT = SCOPE_IDENTITY();

-- Assign Admin role (RoleId = 1)
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
VALUES (@UserId, 1);

SELECT @UserId as NewUserId, '$username' as Username, 'Admin' as Role;
GO
"@

Write-Host ""
Write-Host "SQL Script generated. You can:" -ForegroundColor Yellow
Write-Host "1. Run it directly using SQL Server Management Studio" -ForegroundColor Yellow
Write-Host "2. Save it to a file and execute it" -ForegroundColor Yellow
Write-Host ""
Write-Host "=== SQL Script ===" -ForegroundColor Green
Write-Host $sql -ForegroundColor White
Write-Host ""

# Optionally save to file
$saveFile = Read-Host "Save SQL script to file? (y/n)"
if ($saveFile -eq 'y') {
    $sql | Out-File -FilePath "create_admin_user.sql" -Encoding UTF8
    Write-Host "Saved to: create_admin_user.sql" -ForegroundColor Green
}

Write-Host ""
Write-Host "After running the SQL script, you can login with:" -ForegroundColor Cyan
Write-Host "Username: $username" -ForegroundColor White
Write-Host "Password: [the password you entered]" -ForegroundColor White

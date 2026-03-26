$password = 'katoennatie'
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$passwordBytes = [System.Text.Encoding]::UTF8.GetBytes($password)
$hashBytes = $sha256.ComputeHash($passwordBytes)
$passwordHash = [Convert]::ToBase64String($hashBytes)
Write-Host "Password: $password"
Write-Host "Hash: $passwordHash"

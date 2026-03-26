using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

// Configuration - CHANGE PASSWORD HERE
var username = "admin";
var password = "katoennatie";  // ADJUSTABLE - Change this to any password you want
var email = "admin@gilde.com";
var roleId = 1; // 1=Admin, 2=Editor, 3=Viewer

// Connection string
var connectionString = "Server=(localdb)\\mssqllocaldb;Database=AdminMembersDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// Hash password using same method as AuthService
using var sha256 = SHA256.Create();
var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
var passwordHash = Convert.ToBase64String(hashedBytes);

Console.WriteLine("=== Creating Admin User ===");
Console.WriteLine($"Username: {username}");
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Email: {email}");
Console.WriteLine($"Password Hash: {passwordHash}");
Console.WriteLine();

try
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    // Check if user exists
    var checkUserSql = "SELECT Id FROM Users WHERE Username = @Username";
    using var checkCmd = new SqlCommand(checkUserSql, connection);
    checkCmd.Parameters.AddWithValue("@Username", username);

    var existingUserId = await checkCmd.ExecuteScalarAsync();

    if (existingUserId != null)
    {
        Console.WriteLine("User already exists. Updating password...");

        // Update existing user
        var updateSql = @"
            UPDATE Users 
            SET PasswordHash = @PasswordHash,
                Email = @Email,
                IsActive = 1
            WHERE Username = @Username";

        using var updateCmd = new SqlCommand(updateSql, connection);
        updateCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        updateCmd.Parameters.AddWithValue("@Email", email);
        updateCmd.Parameters.AddWithValue("@Username", username);
        await updateCmd.ExecuteNonQueryAsync();

        // Ensure user has admin role
        var checkRoleSql = "SELECT COUNT(*) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
        using var checkRoleCmd = new SqlCommand(checkRoleSql, connection);
        checkRoleCmd.Parameters.AddWithValue("@UserId", existingUserId);
        checkRoleCmd.Parameters.AddWithValue("@RoleId", roleId);

        var roleExists = (int)(await checkRoleCmd.ExecuteScalarAsync() ?? 0);

        if (roleExists == 0)
        {
            var insertRoleSql = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
            using var insertRoleCmd = new SqlCommand(insertRoleSql, connection);
            insertRoleCmd.Parameters.AddWithValue("@UserId", existingUserId);
            insertRoleCmd.Parameters.AddWithValue("@RoleId", roleId);
            await insertRoleCmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("? User updated successfully!");
    }
    else
    {
        Console.WriteLine("Creating new admin user...");

        // Insert new user
        var insertUserSql = @"
            INSERT INTO Users (Username, PasswordHash, Email, IsActive, CreatedAt)
            VALUES (@Username, @PasswordHash, @Email, 1, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() as int)";

        using var insertUserCmd = new SqlCommand(insertUserSql, connection);
        insertUserCmd.Parameters.AddWithValue("@Username", username);
        insertUserCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        insertUserCmd.Parameters.AddWithValue("@Email", email);

        var newUserId = (int)(await insertUserCmd.ExecuteScalarAsync() ?? 0);

        // Assign role
        var insertRoleSql = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
        using var insertRoleCmd = new SqlCommand(insertRoleSql, connection);
        insertRoleCmd.Parameters.AddWithValue("@UserId", newUserId);
        insertRoleCmd.Parameters.AddWithValue("@RoleId", roleId);
        await insertRoleCmd.ExecuteNonQueryAsync();

        Console.WriteLine("? Admin user created successfully!");
        Console.WriteLine($"User ID: {newUserId}");
    }

    // Verify user
    var verifySql = @"
        SELECT u.Id, u.Username, u.Email, u.IsActive, u.CreatedAt, r.Name as RoleName
        FROM Users u
        LEFT JOIN UserRoles ur ON u.Id = ur.UserId
        LEFT JOIN Roles r ON ur.RoleId = r.Id
        WHERE u.Username = @Username";

    using var verifyCmd = new SqlCommand(verifySql, connection);
    verifyCmd.Parameters.AddWithValue("@Username", username);

    using var reader = await verifyCmd.ExecuteReaderAsync();
    Console.WriteLine();
    Console.WriteLine("=== User Details ===");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"ID: {reader["Id"]}");
        Console.WriteLine($"Username: {reader["Username"]}");
        Console.WriteLine($"Email: {reader["Email"]}");
        Console.WriteLine($"Active: {reader["IsActive"]}");
        Console.WriteLine($"Created: {reader["CreatedAt"]}");
        Console.WriteLine($"Role: {reader["RoleName"]}");
    }

    Console.WriteLine();
    Console.WriteLine("? SUCCESS! You can now login with:");
    Console.WriteLine($"   Username: {username}");
    Console.WriteLine($"   Password: {password}");
    Console.WriteLine();
    Console.WriteLine("Test at: https://localhost:7223/auth-test.html");
}
catch (Exception ex)
{
    Console.WriteLine("? ERROR: " + ex.Message);
    Console.WriteLine(ex.StackTrace);
    return 1;
}

return 0;

using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Tests;

[TestClass]
public class DatabaseTests
{
    [TestMethod]
    public async Task DbContext_CanCreateAndRetrieveMember()
    {
        using var context = DbContextFactory.Create();

        var member = new Member
        {
            FirstName = "Jan",
            LastName = "De Smit",
            Email = "jan@test.be",
            MemberNumber = 1,
            IsAlive = true,
            BirthDate = new DateTime(1990, 5, 15),
            PhoneNumber = "0471234567"
        };

        context.Members.Add(member);
        await context.SaveChangesAsync();

        var retrieved = await context.Members.FirstOrDefaultAsync(m => m.MemberNumber == 1);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Jan", retrieved.FirstName);
        Assert.AreEqual("De Smit", retrieved.LastName);
        Assert.AreEqual("jan@test.be", retrieved.Email);
        Assert.IsTrue(retrieved.IsAlive);
    }

    [TestMethod]
    public async Task DbContext_MemberWithAddress_NavigationWorks()
    {
        using var context = DbContextFactory.Create();

        var member = new Member
        {
            FirstName = "Piet",
            LastName = "Janssen",
            Email = "piet@test.be",
            MemberNumber = 2,
            IsAlive = true,
            Address = new Address
            {
                Street = "Kerkstraat",
                HouseNumber = "10",
                City = "Antwerpen",
                PostalCode = "2000",
                Country = "Belgium"
            }
        };

        context.Members.Add(member);
        await context.SaveChangesAsync();

        var retrieved = await context.Members
            .Include(m => m.Address)
            .FirstOrDefaultAsync(m => m.MemberNumber == 2);

        Assert.IsNotNull(retrieved);
        Assert.IsNotNull(retrieved.Address);
        Assert.AreEqual("Kerkstraat", retrieved.Address.Street);
        Assert.AreEqual("Antwerpen", retrieved.Address.City);
    }

    [TestMethod]
    public async Task DbContext_UserWithRoles_NavigationWorks()
    {
        using var context = DbContextFactory.Create();

        var role = new Role { Name = "Admin", Permission = Permission.ReadWrite };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var user = new User
        {
            Username = "admin",
            Email = "admin@test.be",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await context.SaveChangesAsync();

        var retrieved = await context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == "admin");

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(1, retrieved.UserRoles.Count);
        Assert.AreEqual("Admin", retrieved.UserRoles.First().Role.Name);
        Assert.AreEqual(Permission.ReadWrite, retrieved.UserRoles.First().Role.Permission);
    }

    [TestMethod]
    public async Task DbContext_StockItemWithMovements()
    {
        using var context = DbContextFactory.Create();

        var item = new StockItem
        {
            Name = "Bier",
            CurrentStock = 100,
            MinimumStock = 20
        };
        context.StockItems.Add(item);
        await context.SaveChangesAsync();

        context.StockMovements.Add(new StockMovement
        {
            StockItemId = item.Id,
            Type = StockMovementType.In,
            Quantity = 50,
            Note = "Delivery",
            MovementDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var retrieved = await context.StockItems
            .Include(s => s.Movements)
            .FirstOrDefaultAsync(s => s.Name == "Bier");

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(1, retrieved.Movements.Count);
        Assert.AreEqual(StockMovementType.In, retrieved.Movements.First().Type);
        Assert.AreEqual(50, retrieved.Movements.First().Quantity);
    }

    [TestMethod]
    public async Task DbContext_AgendaEvent_CanCreateAndRetrieve()
    {
        using var context = DbContextFactory.Create();

        var evt = new AgendaEvent
        {
            Title = "Vergadering",
            StartDate = new DateTime(2025, 8, 1, 19, 0, 0),
            EndDate = new DateTime(2025, 8, 1, 21, 0, 0),
            Location = "Gildelokaal",
            IsAllDay = false,
            Color = "#FF5733"
        };
        context.AgendaEvents.Add(evt);
        await context.SaveChangesAsync();

        var retrieved = await context.AgendaEvents.FirstOrDefaultAsync(e => e.Title == "Vergadering");
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Gildelokaal", retrieved.Location);
        Assert.IsFalse(retrieved.IsAllDay);
    }

    [TestMethod]
    public async Task DbContext_AuditLog_CanCreate()
    {
        using var context = DbContextFactory.Create();

        var log = new AuditLog
        {
            UserId = 1,
            Username = "testuser",
            Action = "Login",
            EntityType = "User",
            EntityId = 1,
            Details = "Test login",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        };
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();

        var retrieved = await context.AuditLogs.FirstOrDefaultAsync(a => a.Username == "testuser");
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Login", retrieved.Action);
    }

    [TestMethod]
    public async Task DbContext_DeleteMember_CascadesToAddress()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = DbContextFactory.Create(dbName))
        {
            var member = new Member
            {
                FirstName = "Delete",
                LastName = "Test",
                Email = "delete@test.be",
                MemberNumber = 999,
                IsAlive = true,
                Address = new Address
                {
                    Street = "Teststraat",
                    HouseNumber = "1",
                    City = "TestStad",
                    PostalCode = "1000",
                    Country = "Belgium"
                }
            };
            context.Members.Add(member);
            await context.SaveChangesAsync();
        }

        using (var context = DbContextFactory.Create(dbName))
        {
            var member = await context.Members
                .Include(m => m.Address)
                .FirstAsync(m => m.MemberNumber == 999);

            context.Members.Remove(member);
            await context.SaveChangesAsync();

            Assert.AreEqual(0, await context.Members.CountAsync());
            Assert.AreEqual(0, await context.Addresses.CountAsync());
        }
    }
}

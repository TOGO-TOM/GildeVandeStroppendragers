using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Tests;

[TestClass]
public class PublicApiQueryTests
{
    private static ApplicationDbContext SeedMembers(string dbName, int count = 5)
    {
        var context = DbContextFactory.Create(dbName);

        // Seed a custom field
        var customField = new CustomField
        {
            Id = 1,
            FieldName = "Nickname",
            FieldLabel = "Nickname",
            FieldType = "Text",
            IsActive = true
        };
        context.CustomFields.Add(customField);

        var inactiveField = new CustomField
        {
            Id = 2,
            FieldName = "OldField",
            FieldLabel = "Old Field",
            FieldType = "Text",
            IsActive = false
        };
        context.CustomFields.Add(inactiveField);

        for (int i = 1; i <= count; i++)
        {
            var member = new Member
            {
                Id = i,
                MemberNumber = i,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Role = i % 2 == 0 ? "Admin" : "Stappend Lid",
                PhotoBase64 = "SHOULD_NOT_APPEAR_IN_PROJECTION",
                Email = $"test{i}@example.com",
                Address = new Address
                {
                    Street = $"Street {i}",
                    City = $"City {i}",
                    PostalCode = $"{1000 + i}"
                }
            };
            context.Members.Add(member);
        }
        context.SaveChanges();

        // Add custom field values (active + inactive)
        context.MemberCustomFields.Add(new MemberCustomField
        {
            MemberId = 1,
            CustomFieldId = 1,
            Value = "Johnny"
        });
        context.MemberCustomFields.Add(new MemberCustomField
        {
            MemberId = 1,
            CustomFieldId = 2,
            Value = "OldValue"
        });
        context.SaveChanges();

        return context;
    }

    [TestMethod]
    public async Task Query_ProjectionExcludesPhotoBase64()
    {
        var context = SeedMembers("projection_test");

        var result = await context.Members
            .AsNoTracking()
            .Where(m => m.Id == 1)
            .Select(m => new { m.FirstName, m.PhotoBase64 })
            .FirstOrDefaultAsync();

        // PhotoBase64 is loaded when explicitly selected, but our projection doesn't select it
        // This test verifies the DB has it
        Assert.IsNotNull(result);
        Assert.AreEqual("SHOULD_NOT_APPEAR_IN_PROJECTION", result.PhotoBase64);

        // Now test the actual projection pattern (no PhotoBase64)
        var projected = await context.Members
            .AsNoTracking()
            .Where(m => m.Id == 1)
            .Select(m => new
            {
                m.Id,
                m.FirstName,
                m.LastName,
                AddressCity = m.Address != null ? m.Address.City : null
            })
            .FirstOrDefaultAsync();

        Assert.IsNotNull(projected);
        Assert.AreEqual("First1", projected.FirstName);
        Assert.AreEqual("City 1", projected.AddressCity);
    }

    [TestMethod]
    public async Task Query_PaginationWorks()
    {
        var context = SeedMembers("pagination_test", 10);

        var page1 = await context.Members
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Skip(0).Take(3)
            .ToListAsync();

        var page2 = await context.Members
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Skip(3).Take(3)
            .ToListAsync();

        Assert.AreEqual(3, page1.Count);
        Assert.AreEqual(3, page2.Count);
        Assert.AreEqual(1, page1[0].Id);
        Assert.AreEqual(4, page2[0].Id);
    }

    [TestMethod]
    public async Task Query_FilterByRole()
    {
        var context = SeedMembers("role_filter_test", 6);

        var admins = await context.Members
            .AsNoTracking()
            .Where(m => m.Role == "Admin")
            .ToListAsync();

        Assert.AreEqual(3, admins.Count); // IDs 2, 4, 6
        Assert.IsTrue(admins.All(m => m.Role == "Admin"));
    }

    [TestMethod]
    public async Task Query_ActiveCustomFieldsOnly()
    {
        var context = SeedMembers("custom_field_filter_test");

        var customFields = await context.Members
            .AsNoTracking()
            .Where(m => m.Id == 1)
            .SelectMany(m => m.CustomFieldValues
                .Where(cf => cf.CustomField != null && cf.CustomField.IsActive)
                .Select(cf => new { cf.CustomField!.FieldName, cf.Value }))
            .ToListAsync();

        Assert.AreEqual(1, customFields.Count);
        Assert.AreEqual("Nickname", customFields[0].FieldName);
        Assert.AreEqual("Johnny", customFields[0].Value);
    }

    [TestMethod]
    public async Task Query_TotalCountIndependentOfPagination()
    {
        var context = SeedMembers("count_test", 10);

        var total = await context.Members.AsNoTracking().CountAsync();
        var page = await context.Members.AsNoTracking().OrderBy(m => m.Id).Skip(0).Take(3).ToListAsync();

        Assert.AreEqual(10, total);
        Assert.AreEqual(3, page.Count);
    }
}

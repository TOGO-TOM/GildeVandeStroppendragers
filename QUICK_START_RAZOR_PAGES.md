# Quick Start: Razor Pages Frontend

## What Changed?

Your application now uses **ASP.NET Core Razor Pages** for the frontend instead of vanilla JavaScript/HTML.

## How to Run

### Development
```bash
dotnet run
```
or press **F5** in Visual Studio

### Access the Application
Navigate to: `https://localhost:7223`

## New Routes

| Old Route | New Route | Type |
|-----------|-----------|------|
| `/login.html` | `/Login` | Razor Page |
| `/home.html` | `/Home` | Razor Page |
| `/members.html` | `/members.html` | Still HTML (for now) |

## How Authentication Works Now

### Before (JavaScript)
```javascript
// Client-side only
localStorage.setItem('authToken', token);
localStorage.setItem('currentUser', JSON.stringify(user));
```

### Now (Razor Pages)
```csharp
// Server-side session
HttpContext.Session.SetString("AuthToken", token);
HttpContext.Session.SetString("CurrentUser", userJson);
```

**Benefits:**
- ? More secure (HttpOnly cookies)
- ? Server-side validation
- ? Automatic session timeout

## Creating New Razor Pages

### 1. Create PageModel Class

Create `Pages/MyPage.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;

namespace AdminMembers.Pages
{
    public class MyPageModel : AuthenticatedPageModel
    {
        public IActionResult OnGet()
        {
            // Check if user is logged in
            if (!CheckAuthentication())
            {
                return RedirectToLoginWithReturnUrl();
            }

            // Your page logic here
            return Page();
        }
    }
}
```

### 2. Create Razor View

Create `Pages/MyPage.cshtml`:
```cshtml
@page
@model AdminMembers.Pages.MyPageModel
@{
    ViewData["Title"] = "My Page";
}

<h1>Hello, @Model.CurrentUser?.Username!</h1>
<p>You have the following roles: @string.Join(", ", Model.CurrentUser?.Roles ?? new List<string>())</p>
```

### 3. Access Your Page
Navigate to: `https://localhost:7223/MyPage`

## Using Forms

### Simple Form Example

```cshtml
@page
@model MyPageModel

<form method="post">
    <input asp-for="Name" />
    <button type="submit">Submit</button>
</form>
```

```csharp
public class MyPageModel : AuthenticatedPageModel
{
    [BindProperty]
    public string? Name { get; set; }

    public IActionResult OnPost()
    {
        // Handle form submission
        return RedirectToPage("/Success");
    }
}
```

## Mixing Razor Pages with JavaScript

You can still use JavaScript for dynamic features!

```cshtml
@page
@model MyPageModel

<div id="data-container"></div>

@section Scripts {
    <script>
        // Call your existing APIs
        fetch('/api/members')
            .then(r => r.json())
            .then(data => {
                document.getElementById('data-container').innerHTML = 
                    data.map(m => `<p>${m.firstName} ${m.lastName}</p>`).join('');
            });
    </script>
}
```

## Project Structure

```
AdminMembers/
??? Pages/                          # Razor Pages
?   ??? Login.cshtml                # Login view
?   ??? Login.cshtml.cs             # Login logic
?   ??? Home.cshtml                 # Home view
?   ??? Home.cshtml.cs              # Home logic
?   ??? AuthenticatedPageModel.cs   # Base class for auth
?   ??? Shared/
?       ??? _Layout.cshtml          # Shared layout
??? Controllers/                    # API Controllers (unchanged)
?   ??? MembersController.cs        
??? Services/                       # Business logic (unchanged)
?   ??? AuthService.cs
?   ??? ...
??? Models/                         # Data models (unchanged)
??? wwwroot/                        # Static files
?   ??? styles.css                  # Your CSS
?   ??? app.js                      # Your JavaScript (still works!)
?   ??? members.html                # Old HTML files (still work!)
??? Program.cs                      # App configuration
```

## Common Tasks

### Require Admin Permission

```csharp
public IActionResult OnGet()
{
    if (!CheckAuthentication("ReadWrite"))
    {
        return RedirectToLoginWithReturnUrl();
    }
    return Page();
}
```

### Pass Data to View

```csharp
public class MyPageModel : AuthenticatedPageModel
{
    public List<Member> Members { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();

        Members = await _context.Members.ToListAsync();
        return Page();
    }
}
```

```cshtml
@foreach(var member in Model.Members)
{
    <p>@member.FirstName @member.LastName</p>
}
```

### Handle Form Validation

```csharp
[BindProperty]
[Required]
[StringLength(50)]
public string? Name { get; set; }

public IActionResult OnPost()
{
    if (!ModelState.IsValid)
    {
        return Page(); // Show validation errors
    }

    // Process valid data
    return RedirectToPage("/Success");
}
```

## Debugging Tips

### View Session Data
```csharp
var token = HttpContext.Session.GetString("AuthToken");
var user = HttpContext.Session.GetString("CurrentUser");
Console.WriteLine($"Token: {token}, User: {user}");
```

### Check Authentication
```csharp
if (Model.IsAuthenticated)
{
    <p>Logged in as: @Model.CurrentUser?.Username</p>
}
else
{
    <p>Not logged in</p>
}
```

### Clear Session (Logout)
```csharp
public IActionResult OnPostLogout()
{
    HttpContext.Session.Clear();
    return RedirectToPage("/Login");
}
```

## FAQ

**Q: Can I still use the old HTML files?**  
A: Yes! They work alongside Razor Pages. Gradually migrate as needed.

**Q: Do my API controllers still work?**  
A: Yes! All `/api/*` endpoints remain functional.

**Q: Can I use JavaScript with Razor Pages?**  
A: Absolutely! Use Razor for structure, JavaScript for interactivity.

**Q: How do I access the API from Razor Pages?**  
A: You can inject services directly into PageModel classes or use JavaScript fetch().

**Q: Is this faster than vanilla JavaScript?**  
A: Yes for initial load (server-rendered). JavaScript is faster for updates.

## Resources

- [ASP.NET Core Razor Pages Docs](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/)
- [Razor Syntax Reference](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor)
- [Tag Helpers](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro)

## Need Help?

Check `RAZOR_PAGES_MIGRATION.md` for the full migration plan.

using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Components.Authorization;
using ShiftManagement.Auth;
using ShiftManagement.Components;
using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization();

// Persist the Windows identity captured during SSR into the interactive Blazor circuit.
// This prevents a blank page caused by Windows Auth not re-negotiating over WebSocket.
builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();
builder.Services.AddScoped<PersistentAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<Application.Common.Interfaces.ICurrentUserService, ShiftManagement.Services.CurrentUserService>();

var app = builder.Build();

// Apply any pending EF Core migrations / create the database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
    db.Database.Migrate();

    // ── Seed reference data (runs only when the DB is empty) ──────────────
    if (!db.Departments.Any())
    {
        var departments = new[]
        {
            ("Operations",  new[] { "Main Office", "Warehouse A", "Warehouse B" }),
            ("IT",          new[] { "HQ Data Center", "Support Desk" }),
            ("Security",    new[] { "Gate North", "Gate South", "Control Room" }),
        };

        var sampleEmployees = new[]
        {
            ("Alice Johnson", "alice.johnson"),
            ("Bob Smith",     "bob.smith"),
            ("Carol White",   "carol.white"),
            ("David Brown",   "david.brown"),
            ("Eva Martinez",  "eva.martinez"),
            ("Frank Lee",     "frank.lee"),
        };

        int empIdx = 0;
        foreach (var (deptName, locations) in departments)
        {
            var dept = Domain.Entities.Department.Create(deptName);
            db.Departments.Add(dept);

            foreach (var locName in locations)
            {
                var loc = Domain.Entities.Location.Create(locName, dept.Id);
                db.Locations.Add(loc);

                // Assign two sample employees to each location (cycling through the list)
                for (int i = 0; i < 2; i++)
                {
                    var (eName, eUser) = sampleEmployees[empIdx % sampleEmployees.Length];
                    var emp = Domain.Entities.Employee.Create(
                        $"{eName} ({locName})",
                        $"{eUser}.{locName.ToLower().Replace(" ", "")}{empIdx}",
                        loc.Id);
                    db.Employees.Add(emp);
                    empIdx++;
                }
            }
        }

        db.SaveChanges();
    }

    if (!db.Roles.Any())
    {
        // 1. Create Permissions
        var viewEmployeesPerm = Domain.Entities.Permission.Create("Employee.View");
        var manageEmployeesPerm = Domain.Entities.Permission.Create("Employee.Manage");
        var viewShiftsPerm = Domain.Entities.Permission.Create("Shift.View");
        var manageShiftsPerm = Domain.Entities.Permission.Create("Shift.Manage");
        var manageRolesPerm = Domain.Entities.Permission.Create("Role.Manage");

        db.Permissions.AddRange(viewEmployeesPerm, manageEmployeesPerm, viewShiftsPerm, manageShiftsPerm, manageRolesPerm);

        // 2. Create Roles
        var adminRole = Domain.Entities.Role.Create("Admin");
        adminRole.AddPermission(viewEmployeesPerm);
        adminRole.AddPermission(manageEmployeesPerm);
        adminRole.AddPermission(viewShiftsPerm);
        adminRole.AddPermission(manageShiftsPerm);
        adminRole.AddPermission(manageRolesPerm);

        var managerRole = Domain.Entities.Role.Create("Manager");
        managerRole.AddPermission(viewEmployeesPerm);
        managerRole.AddPermission(viewShiftsPerm);
        managerRole.AddPermission(manageShiftsPerm);

        var employeeRole = Domain.Entities.Role.Create("Employee");
        employeeRole.AddPermission(viewEmployeesPerm);
        employeeRole.AddPermission(viewShiftsPerm);

        db.Roles.AddRange(adminRole, managerRole, employeeRole);
        db.SaveChanges(); // Save roles and permissions first so they are tracked

        // 3. Assign roles to existing employees
        var allEmployees = db.Employees.ToList();
        foreach (var emp in allEmployees)
        {
            if (emp.Username.StartsWith("alice.johnson"))
            {
                emp.AddRole(adminRole);
            }
            else if (emp.Username.StartsWith("bob.smith"))
            {
                emp.AddRole(managerRole);
            }
            else
            {
                emp.AddRole(employeeRole);
            }
        }
        db.SaveChanges();
    }

    // Ensure "Role.Manage" permission is assigned to Admin role in case database already exists
    var dbRoleManagePerm = db.Permissions.FirstOrDefault(p => p.Name == "Role.Manage");
    if (dbRoleManagePerm == null)
    {
        dbRoleManagePerm = Domain.Entities.Permission.Create("Role.Manage");
        db.Permissions.Add(dbRoleManagePerm);
        db.SaveChanges();
    }

    var dbAdminRoleForPerm = db.Roles.Include(r => r.Permissions).FirstOrDefault(r => r.Name == "Admin");
    if (dbAdminRoleForPerm != null && !dbAdminRoleForPerm.Permissions.Any(p => p.Name == "Role.Manage"))
    {
        dbAdminRoleForPerm.AddPermission(dbRoleManagePerm);
        db.SaveChanges();
    }

    // Ensure the local developer and "user" have the Admin role for local debugging
    var localDevUsername = Environment.UserName.ToLowerInvariant();
    var adminUsernames = new List<string> { localDevUsername, "user" };
    var dbAdminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
    if (dbAdminRole != null)
    {
        foreach (var adminUsername in adminUsernames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var devEmp = db.Employees.Include(e => e.Roles).FirstOrDefault(e => e.Username == adminUsername);
            if (devEmp is null)
            {
                devEmp = Domain.Entities.Employee.Create($"Local Developer ({adminUsername})", adminUsername);
                db.Employees.Add(devEmp);
                db.SaveChanges();
            }

            if (!devEmp.Roles.Any(r => r.Name == "Admin"))
            {
                devEmp.AddRole(dbAdminRole);
                db.SaveChanges();
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

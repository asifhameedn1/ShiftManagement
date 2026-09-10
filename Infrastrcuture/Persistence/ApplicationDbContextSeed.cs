using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        // Populated while seeding departments/locations/employees below; used to assign each
        // seeded employee's initial role directly in the department they were created under.
        var employeeDepartments = new List<(Employee Employee, Department Department)>();

        // AssignRole returns the newly created join entity (or null if it already existed) so it
        // can be explicitly tracked as Added — without this, EF's graph fix-up can mistake a new
        // entity with a client-generated key for an existing row when the Employee is already
        // tracked as Unchanged, producing a failing UPDATE instead of an INSERT.
        void AssignRole(Employee employee, Department department, Role role)
        {
            var departmentRole = employee.AssignRole(department, role);
            if (departmentRole is not null)
            {
                db.EmployeeDepartmentRoles.Add(departmentRole);
            }
        }

        // ── Seed reference data (runs only when the DB is empty) ──────────────
        if (!await db.Departments.AnyAsync())
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
                var dept = Department.Create(deptName);
                db.Departments.Add(dept);

                foreach (var locName in locations)
                {
                    var loc = Location.Create(locName, dept.Id);
                    db.Locations.Add(loc);

                    // Assign two sample employees to each location (cycling through the list)
                    for (int i = 0; i < 2; i++)
                    {
                        var (eName, eUser) = sampleEmployees[empIdx % sampleEmployees.Length];
                        var emp = Employee.Create(
                            $"{eName} ({locName})",
                            $"{eUser}.{locName.ToLower().Replace(" ", "")}{empIdx}",
                            loc.Id);
                        db.Employees.Add(emp);
                        employeeDepartments.Add((emp, dept));
                        empIdx++;
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        if (!await db.Roles.AnyAsync())
        {
            // 1. Create Permissions
            var viewEmployeesPerm = Permission.Create("Employee.View");
            var manageEmployeesPerm = Permission.Create("Employee.Manage");
            var viewShiftsPerm = Permission.Create("Shift.View");
            var manageShiftsPerm = Permission.Create("Shift.Manage");
            var manageRolesPerm = Permission.Create("Role.Manage");

            db.Permissions.AddRange(viewEmployeesPerm, manageEmployeesPerm, viewShiftsPerm, manageShiftsPerm, manageRolesPerm);

            // 2. Create Roles
            var adminRole = Role.Create("Admin");
            adminRole.AddPermission(viewEmployeesPerm);
            adminRole.AddPermission(manageEmployeesPerm);
            adminRole.AddPermission(viewShiftsPerm);
            adminRole.AddPermission(manageShiftsPerm);
            adminRole.AddPermission(manageRolesPerm);

            var managerRole = Role.Create("Manager");
            managerRole.AddPermission(viewEmployeesPerm);
            managerRole.AddPermission(viewShiftsPerm);
            managerRole.AddPermission(manageShiftsPerm);

            var employeeRole = Role.Create("Employee");
            employeeRole.AddPermission(viewEmployeesPerm);
            employeeRole.AddPermission(viewShiftsPerm);

            db.Roles.AddRange(adminRole, managerRole, employeeRole);
            await db.SaveChangesAsync(); // Save roles and permissions first so they are tracked

            // 3. Assign roles to seeded employees, scoped to the department each was created under
            foreach (var (emp, dept) in employeeDepartments)
            {
                if (emp.Username.StartsWith("alice.johnson"))
                {
                    AssignRole(emp, dept, adminRole);
                }
                else if (emp.Username.StartsWith("bob.smith"))
                {
                    AssignRole(emp, dept, managerRole);
                }
                else
                {
                    AssignRole(emp, dept, employeeRole);
                }
            }
            await db.SaveChangesAsync();
        }

        // Ensure "Role.Manage" permission is assigned to Admin role in case database already exists
        var dbRoleManagePerm = await db.Permissions.FirstOrDefaultAsync(p => p.Name == "Role.Manage");
        if (dbRoleManagePerm == null)
        {
            dbRoleManagePerm = Permission.Create("Role.Manage");
            db.Permissions.Add(dbRoleManagePerm);
            await db.SaveChangesAsync();
        }

        var dbAdminRoleForPerm = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == "Admin");
        if (dbAdminRoleForPerm != null && !dbAdminRoleForPerm.Permissions.Any(p => p.Name == "Role.Manage"))
        {
            dbAdminRoleForPerm.AddPermission(dbRoleManagePerm);
            await db.SaveChangesAsync();
        }

        // Ensure the local developer and "user" have the Admin role for local debugging
        var localDevUsername = Environment.UserName.ToLowerInvariant();
        var adminUsernames = new List<string> { localDevUsername, "user" };
        var dbAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (dbAdminRole != null)
        {
            var allDepartments = await db.Departments.ToListAsync();

            foreach (var adminUsername in adminUsernames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var devEmp = await db.Employees
                    .Include(e => e.DepartmentRoles)
                        .ThenInclude(dr => dr.Role)
                    .FirstOrDefaultAsync(e => e.Username == adminUsername);
                if (devEmp is null)
                {
                    devEmp = Employee.Create($"Local Developer ({adminUsername})", adminUsername);
                    db.Employees.Add(devEmp);
                    await db.SaveChangesAsync();
                }

                // Grant Admin in every department for full local-debugging access
                foreach (var dept in allDepartments)
                {
                    if (!devEmp.DepartmentRoles.Any(dr => dr.DepartmentId == dept.Id && dr.Role.Name == "Admin"))
                    {
                        AssignRole(devEmp, dept, dbAdminRole);
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}

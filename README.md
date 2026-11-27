# MediCare Clinic Management and Appointment System

MediCare is an ASP.NET Core MVC application for clinic operations. The public demo lets visitors browse fictional doctors, book or cancel a test appointment, and view the temporary patient profile for their browser session.

## Run the demo in Visual Studio

Open `MediCare.slnx` in Visual Studio 2026 with ASP.NET and web development installed. Set `MediCare.Web` as the startup project and select the **Public Demo (local)** launch profile. Start without debugging to open http://localhost:5284.

This profile uses a separate SQLite database under `MediCare.Web/App_Data/demo`. Run the demo with fictional data only.

## Build the demo package

```shell
dotnet publish MediCare.Web/MediCare.Web.csproj -c Release -p:UseAppHost=false -o publish
```

Configure the host with `ASPNETCORE_ENVIRONMENT=Demo`, HTTPS, and a persistent writable demo directory (`Demo__DataDirectory`). Run the published application from its publish folder. Use one app instance with a private upstream port and an HTTPS reverse proxy on the same host. The local HTTP override is for loopback previews.

## SQL Server environments

Outside the demo, configure `ConnectionStrings__DefaultConnection` on the host and apply the reviewed EF Core migrations. The booking migration adds uniqueness constraints for active doctor and patient slots. Before applying it to an existing SQL Server database, check for duplicate active slots, appointment dates with time components, and notes longer than 500 characters. The release package and local demo use a separate database and will not run SQL Server migrations automatically. Provision privileged accounts through a trusted administrator process. Review role memberships in databases created before the registration fix.

## Doctor and administrator workflows

After preparing the database in a non-demo environment, configure `BootstrapAdmin__Email` and `BootstrapAdmin__Password` through the host's protected settings for the first administrator. Remove the bootstrap settings after successful setup. An existing non-admin account is never promoted by bootstrap.

An administrator can add a specialization and create a doctor account from **Admin → Users**. A signed-in doctor opens **My Schedule** to set weekly hours and vacation days, review the daily or weekly agenda, and confirm or cancel appointments. The public demo provisions temporary patient sessions.

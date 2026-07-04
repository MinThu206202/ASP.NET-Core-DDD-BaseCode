Squashed migrations — README

This repository has consolidated EF Core migrations into a single baseline migration named `Initial`.

Important for teammates:

1. Delete or back up your local database (drop the `userapp` database) or run the commands below to reset.

2. After pulling the changes, run the following to recreate the database schema:

```bash
cd /path/to/ASP.NET-Core-BaseCode
dotnet build UserApp.sln
cd src/UserApp.Web
dotnet ef database update --project ../UserApp.Infrastructure --startup-project .
```

Notes:
- This operation removes prior migration history. Ensure you have backed up any important data before resetting local databases.
- If you run into permission/connection issues, verify the connection string in `src/UserApp.Web/appsettings.json` or environment variables used by your Docker setup.

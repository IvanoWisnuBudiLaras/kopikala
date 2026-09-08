var builder = DistributedApplication.CreateBuilder(args);

// Container PostgreSQL (Docker) with persistent volume
var postgres = builder.AddPostgres("kopikala-postgres", port: 5432)
    .WithDataVolume("kopikala_pgdata")
    .WithPgAdmin();

var db = postgres.AddDatabase("DefaultConnection", "kopikala_db");

// Blazor Web App referencing the PostgreSQL database
builder.AddProject("kopikala-web", "../KopiKala/KopiKala.csproj")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();

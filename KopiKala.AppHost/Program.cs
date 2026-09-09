var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("pg-user", "kopikala_user");
var pgPassword = builder.AddParameter("pg-password", "kopikala_password123", secret: true);

// Container PostgreSQL (Docker) with persistent volume & explicit credentials matching docker-compose
var postgres = builder.AddPostgres("kopikala-postgres", userName: pgUser, password: pgPassword, port: 5432)
    .WithDataVolume("kopikala_pgdata")
    .WithPgAdmin();

var db = postgres.AddDatabase("DefaultConnection", "kopikala_db");

// Blazor Web App referencing the PostgreSQL database
builder.AddProject("kopikala-web", "../KopiKala/KopiKala.csproj")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();

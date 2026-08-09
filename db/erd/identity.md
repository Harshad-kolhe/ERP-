<!-- Generated from the EF Core model by Erp.ArchitectureTests. Do not edit. -->

# Database schema `identity`

7 tables. Regenerate with:

```
dotnet test backend/tests/Erp.ArchitectureTests --filter FullyQualifiedName~SchemaDiagram
```

```mermaid
erDiagram
    AspNetRoleClaims {
        int Id PK
        nvarchar_max ClaimType "nullable"
        nvarchar_max ClaimValue "nullable"
        uniqueidentifier RoleId FK
    }
    AspNetRoles {
        uniqueidentifier Id PK
        nvarchar_max ConcurrencyStamp "nullable"
        nvarchar_max Description
        bit IsSuperAdministrator
        nvarchar_256 Name "nullable"
        nvarchar_256 NormalizedName "nullable"
    }
    AspNetUserClaims {
        int Id PK
        nvarchar_max ClaimType "nullable"
        nvarchar_max ClaimValue "nullable"
        uniqueidentifier UserId FK
    }
    AspNetUserLogins {
        nvarchar_450 LoginProvider PK
        nvarchar_450 ProviderKey PK
        nvarchar_max ProviderDisplayName "nullable"
        uniqueidentifier UserId FK
    }
    AspNetUserRoles {
        uniqueidentifier UserId PK,FK
        uniqueidentifier RoleId PK,FK
    }
    AspNetUserTokens {
        uniqueidentifier UserId PK,FK
        nvarchar_450 LoginProvider PK
        nvarchar_450 Name PK
        nvarchar_max Value "nullable"
    }
    AspNetUsers {
        uniqueidentifier Id PK
        int AccessFailedCount
        int BusinessUnitId
        bit CanAccessAllBusinessUnits
        nvarchar_max ConcurrencyStamp "nullable"
        nvarchar_max DisplayName
        nvarchar_256 Email "nullable"
        bit EmailConfirmed
        bit LockoutEnabled
        datetimeoffset LockoutEnd "nullable"
        nvarchar_256 NormalizedEmail "nullable"
        nvarchar_256 NormalizedUserName "nullable"
        nvarchar_max PasswordHash "nullable"
        nvarchar_max PhoneNumber "nullable"
        bit PhoneNumberConfirmed
        nvarchar_max SecurityStamp "nullable"
        bit TwoFactorEnabled
        nvarchar_256 UserName "nullable"
    }
    AspNetRoles ||--o{ AspNetRoleClaims : "RoleId"
    AspNetRoles ||--o{ AspNetUserRoles : "RoleId"
    AspNetUsers ||--o{ AspNetUserClaims : "UserId"
    AspNetUsers ||--o{ AspNetUserLogins : "UserId"
    AspNetUsers ||--o{ AspNetUserRoles : "UserId"
    AspNetUsers ||--o{ AspNetUserTokens : "UserId"
```

## Columns that name a relationship the database does not enforce

Found by name (`*Id`, not a primary key, not covered by any foreign key), so the list
is a prompt to look rather than a defect list. Some entries are deliberate: audit
columns and anything pointing into another module's schema cannot be constrained,
because each module owns a separate `DbContext`. See `db/erd/README.md`.

| Table | Column | Type |
|---|---|---|
| AspNetUsers | BusinessUnitId | `int` |

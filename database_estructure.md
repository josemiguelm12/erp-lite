Todas las tablas de negocio tendrán:

TenantId UUID NOT NULL
CreatedAt TIMESTAMP
CreatedBy UUID
UpdatedAt TIMESTAMP
UpdatedBy UUID
IsDeleted BOOLEAN DEFAULT FALSE


### Tenants

CREATE TABLE Tenants (
    Id UUID PRIMARY KEY,
    Name VARCHAR(150) NOT NULL,
    Slug VARCHAR(100) UNIQUE,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW()
);


### Users

CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,
    FullName VARCHAR(150),
    Email VARCHAR(150) UNIQUE NOT NULL,
    PasswordHash TEXT NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,

    CreatedAt TIMESTAMP DEFAULT NOW(),
    CreatedBy UUID,
    UpdatedAt TIMESTAMP,
    UpdatedBy UUID,
    IsDeleted BOOLEAN DEFAULT FALSE,

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

### Roles

CREATE TABLE Roles (
    Id UUID PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    TenantId UUID NOT NULL,

    CreatedAt TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);


### Permissions

CREATE TABLE Permissions (
    Id UUID PRIMARY KEY,
    Name VARCHAR(150) UNIQUE NOT NULL
);


### RolePermissions

CREATE TABLE RolePermissions (
    RoleId UUID,
    PermissionId UUID,
    PRIMARY KEY (RoleId, PermissionId),

    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id)
);


### UserRoles
CREATE TABLE UserRoles (
    UserId UUID,
    RoleId UUID,
    PRIMARY KEY (UserId, RoleId),

    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);


### refresh_tokens

CREATE TABLE RefreshTokens (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL,
    Token TEXT NOT NULL,
    ExpiresAt TIMESTAMP NOT NULL,
    IsRevoked BOOLEAN DEFAULT FALSE,

    FOREIGN KEY (UserId) REFERENCES Users(Id)
);


### customers

CREATE TABLE Customers (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,

    Name VARCHAR(150) NOT NULL,
    Email VARCHAR(150),
    Phone VARCHAR(50),
    Address TEXT,

    CreatedAt TIMESTAMP DEFAULT NOW(),
    CreatedBy UUID,
    UpdatedAt TIMESTAMP,
    UpdatedBy UUID,
    IsDeleted BOOLEAN DEFAULT FALSE,

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);


### products

CREATE TABLE Products (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,

    Name VARCHAR(150) NOT NULL,
    Description TEXT,
    Price DECIMAL(10, 2) NOT NULL,
    Stock INT DEFAULT 0,

    CreatedAt TIMESTAMP DEFAULT NOW(),
    CreatedBy UUID,
    UpdatedAt TIMESTAMP,
    UpdatedBy UUID,
    IsDeleted BOOLEAN DEFAULT FALSE,

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

### invoices

CREATE TABLE Invoices (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,

    CustomerId UUID NOT NULL,
    InvoiceNumber VARCHAR(50),
    Status VARCHAR(50), -- Pending, Paid, Cancelled

    Subtotal DECIMAL(10,2),
    Tax DECIMAL(10,2),
    Total DECIMAL(10,2),

    IssuedDate TIMESTAMP,
    DueDate TIMESTAMP,

    CreatedAt TIMESTAMP DEFAULT NOW(),
    CreatedBy UUID,
    UpdatedAt TIMESTAMP,
    UpdatedBy UUID,
    IsDeleted BOOLEAN DEFAULT FALSE,

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

### invoice_items

CREATE TABLE InvoiceItems (
    Id UUID PRIMARY KEY,

    InvoiceId UUID NOT NULL,
    ProductId UUID,

    Description TEXT,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2),
    Total DECIMAL(10,2),

    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

### payments

CREATE TABLE Payments (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,

    InvoiceId UUID NOT NULL,
    Amount DECIMAL(10,2),
    PaymentDate TIMESTAMP,
    Method VARCHAR(50),

    CreatedAt TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
);

### TenantSettings

CREATE TABLE TenantSettings (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL UNIQUE,

    Currency VARCHAR(10),
    TaxRate DECIMAL(5,2),
    LogoUrl TEXT,

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

### AuditLogs

CREATE TABLE AuditLogs (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,

    UserId UUID,
    Action VARCHAR(100),
    Entity VARCHAR(100),
    EntityId UUID,

    Timestamp TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

### relaciones clave

Tenant → Users
Tenant → Roles
Tenant → Customers
Tenant → Products
Tenant → Invoices
Tenant → Payments

Users ↔ Roles (many-to-many)
Roles ↔ Permissions (many-to-many)

Invoices → Customers
Invoices → InvoiceItems
InvoiceItems → Products
Payments → Invoices

### reglas importantes 

1. Todas las queries deben incluir TenantId
2. Nunca borrar físicamente
Usar IsDeleted
3. Guardar quién creó/modificó
4. No permitir que un tenant acceda a otro

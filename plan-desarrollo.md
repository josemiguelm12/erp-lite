# ERP-Lite Multi-Tenant SaaS - Plan de Desarrollo

## Descripción

Sistema ERP-lite multi-tenant orientado a SaaS empresarial.  
Permite a múltiples empresas (tenants) gestionar clientes, productos, facturación y reportes dentro de un sistema compartido con aislamiento de datos.

---

## Objetivo

Construir un sistema que demuestre:

- Arquitectura backend escalable
- Multi-tenancy real
- Buen diseño de base de datos
- Aplicación de SOLID
- Separación de responsabilidades
- Integración frontend empresarial

---

## Stack Tecnológico

### Backend
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT + Refresh Tokens
- Serilog (logs)
- FluentValidation
- AutoMapper / Mapster

### Frontend
- Angular
- PrimeNG (UI)
- RxJS
- Angular Guards & Interceptors

### Infraestructura
- Azure App Service
- Azure Database (PostgreSQL)

---

## Arquitectura

### Clean Architecture (simplificada)
src/
├── ErpLite.Api
├── ErpLite.Application
├── ErpLite.Domain
└── ErpLite.Infrastructure


### Capa

- **API** → Controllers, Middlewares
- **Application** → UseCases, Services, DTOs
- **Domain** → Entidades, reglas de negocio
- **Infrastructure** → DB, JWT, servicios externos

---

##  Multi-Tenancy

### Estrategia
- Base de datos compartida
- Campo `TenantId` en todas las tablas

### Regla clave
> Ninguna consulta debe ejecutarse sin filtrar por TenantId

---

##  Autenticación y Autorización

- JWT Access Token
- Refresh Token
- RBAC (Roles)
- Permisos granulares

### Roles
- Owner
- Admin
- Manager
- Employee

### Permisos ejemplo

customers.read
customers.create
invoices.create
users.manage
reports.view


---

##  Base de Datos

### Tablas Core

Tenants
Users
Roles
Permissions
RolePermissions
UserRoles
RefreshTokens


### Tablas ERP

Customers
Products
Invoices
InvoiceItems
Payments
TenantSettings
AuditLogs


---

##  Módulos

### Fase 1 - Core

- Registro de empresa (tenant)
- Login
- Usuarios
- Roles
- Permisos

---

### Fase 2 - ERP

- Clientes
- Productos
- Facturación
- Pagos

---

### Fase 3 - Empresarial

- Dashboard
- Reportes
- Auditoría
- Configuración por empresa

---

### Fase 4 - Avanzado

- Rate limiting
- Logs estructurados
- Background jobs
- Exportación PDF/Excel
- Notificaciones

---

## Patrones de Diseño

### 1. Repository Pattern
Acceso a datos desacoplado

### 2. Unit of Work
Transacciones

### 3. Service Layer / Use Cases
Lógica de negocio

### 4. Specification Pattern
Filtros complejos

### 5. Strategy Pattern
Lógica configurable (ej: impuestos)

---

##  SOLID

### Single Responsibility
Separar lógica en servicios independientes

### Open/Closed
Extensible sin modificar código existente

### Dependency Inversion
Uso de interfaces en Application

---

##  Frontend Angular

### Estructura
app/
├── core/
├── shared/
├── features/
│ ├── dashboard/
│ ├── customers/
│ ├── products/
│ ├── invoices/
│ ├── users/
│ └── settings/
└── layout/


### Características
- Guards → protección de rutas
- Interceptors → JWT
- Reactive Forms
- Servicios HTTP

---

##  UI

- PrimeNG (tablas, formularios, modales)
- Opcional: TailwindCSS

---

##  Flujo de Tenant

1. Usuario inicia sesión
2. JWT incluye `TenantId`
3. Backend extrae `TenantId`
4. Queries filtran por TenantId

---

##  Features Clave

- Soft Delete (`IsDeleted`)
- Auditoría (`CreatedAt`, `CreatedBy`)
- Configuración por empresa
- Dashboard con métricas

---

##  Roadmap

### Semana 1
- Auth + Multi-tenant base

### Semana 2
- Clientes + Productos

### Semana 3
- Facturación + Pagos

### Semana 4
- Dashboard + Reportes

### Semana 5
- Features avanzadas

---

##  README (obligatorio)

Debe incluir:

- Descripción
- Arquitectura
- Diagrama
- Cómo correr el proyecto
- Variables de entorno
- Endpoints
- Credenciales demo

---

##  Objetivo final

Construir un sistema que demuestre:

✔ Arquitectura real  
✔ Multi-tenancy  
✔ Backend escalable  
✔ Diseño limpio  
✔ Nivel profesional  

---

##  Resultado esperado

> Multi-tenant ERP-lite SaaS con arquitectura limpia, autenticación robusta, módulos empresariales y frontend estructurado en Angular.
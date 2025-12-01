# KURATOR - Sistema de Identificación y Gestión (SIG)

Sistema de gestión de contactos y relaciones gubernamentales con control de acceso basado en roles y cifrado de datos sensibles.

## Características Principales

- **Gestión de Contactos**: Administración completa de contactos por sectores/bloques
- **Control de Acceso**: Sistema de roles (Admin, Curator, Threat Analyst)
- **Cifrado de Datos**: RSA-2048 + AES-256 para protección de datos sensibles
- **Auditoría Completa**: Registro de todas las acciones del sistema
- **MFA**: Autenticación de dos factores obligatoria
- **Watchlist**: Registro de amenazas potenciales
- **Dashboard Analítico**: Métricas y estadísticas en tiempo real

## Stack Tecnológico

### Backend
- .NET 9 (ASP.NET Core Web API)
- Entity Framework Core 9
- PostgreSQL 16
- JWT Authentication
- Serilog (logging)

### Frontend
- Next.js 14 (App Router)
- React 18
- TypeScript
- TailwindCSS
- React Query

### Infraestructura
- Docker & Docker Compose
- Nginx (opcional)

## Requisitos Previos

### Opción 1: Docker (Recomendado)
- Docker Desktop
- Docker Compose v2+

### Opción 2: Desarrollo Local
- .NET 9 SDK
- Node.js 20+
- PostgreSQL 16
- Git

## Instalación Rápida

### Usando Docker Compose

1. **Clonar el repositorio**
```bash
git clone https://github.com/your-org/kurator.git
cd kurator
```

2. **Configurar variables de entorno**
```bash
cp .env.example .env
# Editar .env con valores seguros
```

3. **Iniciar servicios**
```bash
docker-compose up -d
```

4. **Aplicar migraciones (primer uso)**
```bash
docker-compose exec backend dotnet ef database update --project Kurator.Infrastructure --startup-project Kurator.Api
```

5. **Acceder a la aplicación**
- Frontend: http://localhost:3000
- API: http://localhost:5000/api
- Swagger: http://localhost:5000/swagger

### Usando PowerShell Script (Windows)

```powershell
# Ejecutar script de configuración
.\setup.ps1

# O ejecutar solo servicios (sin setup)
.\setup.ps1 -RunOnly
```

### Instalación Manual

#### Backend
```bash
cd backend
dotnet restore
dotnet build

# Crear y aplicar migraciones
dotnet ef migrations add InitialCreate --project Kurator.Infrastructure --startup-project Kurator.Api
dotnet ef database update --project Kurator.Infrastructure --startup-project Kurator.Api

# Ejecutar
dotnet run --project Kurator.Api
```

#### Frontend
```bash
cd frontend
npm install
npm run build
npm run dev   # Desarrollo
npm start     # Producción
```

## Credenciales por Defecto

- **Usuario**: admin
- **Contraseña**: Admin123!

⚠️ **IMPORTANTE**: Cambiar la contraseña y configurar MFA en el primer inicio de sesión.

## Estructura del Proyecto

```
kurator/
├── backend/                  # API .NET 9
│   ├── Kurator.Api/         # Web API
│   ├── Kurator.Core/        # Dominio y lógica de negocio
│   └── Kurator.Infrastructure/ # Acceso a datos
├── frontend/                 # Next.js 14
│   ├── app/                 # App Router
│   ├── components/          # Componentes React
│   └── services/            # Integración API
├── database/                 # Scripts SQL
├── docker-compose.yml       # Configuración Docker
└── .env                     # Variables de entorno
```

## Arquitectura

### Backend - Clean Architecture

```
Kurator.Api (Presentation)
    ↓
Kurator.Core (Domain)
    ↓
Kurator.Infrastructure (Data Access)
```

### Frontend - Component Architecture

```
Pages (Server Components)
    ↓
Components (Client/Server)
    ↓
Services (API Integration)
    ↓
Types (TypeScript)
```

## Seguridad

### Cifrado de Datos

El sistema implementa un esquema de cifrado de dos capas:

1. **Cliente (RSA-2048)**: Cifrado de campos sensibles antes de enviar al servidor
2. **Servidor (AES-256)**: Cifrado adicional en reposo

Campos cifrados:
- Nombres completos de contactos
- Notas y comentarios confidenciales
- Archivos adjuntos
- Valores en logs de auditoría

### Autenticación y Autorización

- JWT tokens con expiración de 8 horas
- Refresh tokens de 7 días
- MFA obligatorio con TOTP
- Control de acceso basado en roles

## Roles del Sistema

### Administrador
- Acceso completo al sistema
- Gestión de usuarios y bloques
- Configuración de referencias
- Acceso a logs de auditoría

### Curador
- Acceso a bloques asignados
- Gestión de contactos e interacciones
- Visualización de FAQ

### Analista de Amenazas
- Acceso exclusivo a Watchlist
- Sin acceso a contactos principales

## API Endpoints

### Autenticación
- `POST /api/auth/login` - Inicio de sesión
- `POST /api/auth/setup-mfa` - Configurar MFA
- `POST /api/auth/verify-mfa` - Verificar código MFA

### Contactos
- `GET /api/contacts` - Listar contactos (con filtros)
- `POST /api/contacts` - Crear contacto
- `GET /api/contacts/{id}` - Obtener detalles
- `PUT /api/contacts/{id}` - Actualizar contacto
- `DELETE /api/contacts/{id}` - Desactivar contacto

### Interacciones
- `GET /api/interactions` - Listar interacciones
- `POST /api/interactions` - Crear interacción
- `GET /api/interactions/{id}` - Obtener detalles
- `PUT /api/interactions/{id}` - Actualizar
- `DELETE /api/interactions/{id}` - Desactivar

### Bloques
- `GET /api/blocks` - Listar bloques
- `POST /api/blocks` - Crear bloque
- `PUT /api/blocks/{id}` - Actualizar
- `PUT /api/blocks/{id}/archive` - Archivar

### Usuarios
- `GET /api/users` - Listar usuarios
- `POST /api/users` - Crear usuario
- `PUT /api/users/{id}` - Actualizar
- `DELETE /api/users/{id}` - Desactivar

## Comandos Útiles

### Docker
```bash
# Ver logs
docker-compose logs -f [service]

# Reiniciar servicio
docker-compose restart [service]

# Reconstruir
docker-compose up -d --build

# Backup de base de datos
docker-compose exec postgres pg_dump -U kurator_user kurator > backup.sql
```

### Entity Framework
```bash
# Crear migración
dotnet ef migrations add [Name] --project Kurator.Infrastructure --startup-project Kurator.Api

# Aplicar migraciones
dotnet ef database update --project Kurator.Infrastructure --startup-project Kurator.Api

# Revertir migración
dotnet ef database update [PreviousMigration] --project Kurator.Infrastructure --startup-project Kurator.Api
```

### Frontend
```bash
# Desarrollo
npm run dev

# Build producción
npm run build

# Tests
npm test
npm run test:e2e

# Linting
npm run lint
npm run format
```

## Troubleshooting

### Backend no se conecta a la base de datos
- Verificar que PostgreSQL esté ejecutándose
- Verificar connection string en appsettings.json
- Comprobar credenciales en .env

### Frontend no se conecta al API
- Verificar que el backend esté ejecutándose en puerto 5000
- Comprobar NEXT_PUBLIC_API_URL en .env
- Verificar configuración CORS

### Error en migraciones
```bash
# Eliminar migraciones existentes
rm -rf backend/Kurator.Infrastructure/Data/Migrations

# Crear nueva migración inicial
dotnet ef migrations add InitialCreate --project Kurator.Infrastructure --startup-project Kurator.Api
```

## Testing

### Backend
```bash
cd backend
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend
```bash
cd frontend
npm test
npm run test:watch
npm run test:coverage
npm run test:e2e
```

## Despliegue en Producción

### Preparación
1. Configurar variables de entorno seguras
2. Habilitar HTTPS
3. Configurar firewall
4. Configurar backups automáticos

### Docker Compose Producción
```bash
# Usar archivo de producción
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Monitoreo
- Health check: `/health`
- Metrics: Serilog + Application Insights (opcional)
- Logs: Docker logs o Serilog sinks

## Mantenimiento

### Backups
```bash
# Backup completo
./scripts/backup.sh

# Backup solo BD
docker-compose exec postgres pg_dump -U kurator_user kurator > backup_$(date +%Y%m%d).sql
```

### Actualización
```bash
# Pull últimos cambios
git pull

# Reconstruir y reiniciar
docker-compose down
docker-compose up -d --build

# Aplicar nuevas migraciones
docker-compose exec backend dotnet ef database update --project Kurator.Infrastructure --startup-project Kurator.Api
```

## Contribución

1. Fork el proyecto
2. Crear feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

## Licencia

Propietario - Todos los derechos reservados

## Soporte

Para soporte y consultas, contactar al equipo de desarrollo.

## Documentación Adicional

- [Especificación Técnica](./kurator/final.md)
- [Análisis de Negocio](./kurator/business-analysis.md)
- [Guía para Claude Code](./kurator/CLAUDE.md)

---

**Versión**: 1.0.0
**Última actualización**: Noviembre 2024
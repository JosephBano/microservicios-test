# Inventory Management System

Sistema web de gestión de inventarios desarrollado con arquitectura de microservicios.

## Tecnologías

| Capa | Tecnología |
|---|---|
| Frontend | Angular 21 + Angular Material |
| Backend | .NET 8 (ASP.NET Core Web API) |
| Base de datos | PostgreSQL 16 |
| ORM | Entity Framework Core 8 + Npgsql |
| Validaciones | FluentValidation 11 |
| Documentación API | Swagger (Swashbuckle) |
| Contenedores | Docker + Docker Compose |

## Arquitectura

```
Angular 21 (puerto 4200)
  ├ → ProductService    (puerto 5001)
  └ → TransactionService (puerto 5002)
                └ → ProductService (comunicación interna para stock)

PostgreSQL (puerto 5432)
  ├ schema: product_schema
  └ schema: transaction_schema
```

---

## Requisitos

Para ejecutar el proyecto en un entorno local necesita:

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) y npm 9+
- [Angular CLI 21+](https://angular.io/cli): `npm install -g @angular/cli`
- [Docker](https://www.docker.com/) y Docker Compose (para PostgreSQL)

---

## Ejecución del Backend

### 1. Levantar la base de datos (PostgreSQL con Docker)

```bash
docker compose up postgres -d
```

La base de datos se inicializa automáticamente con el script `database/init.sql`.

> Credenciales: Host `localhost:5432` | DB `inventory_db` | User `inventory_user` | Pass `inventory_pass`

### 2. Ejecutar ProductService

```bash
cd backend/ProductService
dotnet run
```

- URL: `http://localhost:5001`
- Swagger UI: `http://localhost:5001/swagger`

### 3. Ejecutar TransactionService

En otra terminal:

```bash
cd backend/TransactionService
dotnet run
```

- URL: `http://localhost:5002`
- Swagger UI: `http://localhost:5002/swagger`

> El TransactionService se comunica con el ProductService internamente. Ambos deben estar corriendo.

### Ejecutar todo con Docker Compose

Para levantar todos los servicios (incluyendo los microservicios .NET compilados en Docker):

```bash
docker compose up --build
```

---

## Ejecución del Frontend

```bash
cd frontend/inventory-app
npm install
ng serve
```

Abrir en el navegador: **http://localhost:4200**

> Asegúrese de que ambos microservicios backend estén ejecutándose antes de usar el frontend.

---

## Endpoints Disponibles

### ProductService (`http://localhost:5001`)

| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/products` | Listar con paginación y filtros |
| GET | `/api/products/{id}` | Obtener por ID |
| POST | `/api/products` | Crear producto |
| PUT | `/api/products/{id}` | Actualizar producto |
| DELETE | `/api/products/{id}` | Eliminar producto |
| PATCH | `/api/products/{id}/stock` | Ajustar stock (uso interno) |

**Filtros disponibles en GET `/api/products`:**
`?name=&category=&minPrice=&maxPrice=&minStock=&page=1&pageSize=10`

### TransactionService (`http://localhost:5002`)

| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/transactions` | Listar con paginación y filtros |
| GET | `/api/transactions/{id}` | Obtener por ID |
| POST | `/api/transactions` | Crear transacción (valida y ajusta stock) |
| PUT | `/api/transactions/{id}` | Editar detalle/fecha |
| DELETE | `/api/transactions/{id}` | Eliminar y revertir stock |
| GET | `/api/transactions/product/{productId}` | Historial de un producto |

**Filtros disponibles en GET `/api/transactions`:**
`?productId=&type=Purchase|Sale&dateFrom=&dateTo=&page=1&pageSize=10`

---

## Flujo de negocio

### Crear una Venta
1. El TransactionService recibe la solicitud.
2. Consulta al ProductService para verificar existencia y stock del producto.
3. Si `quantity > stock disponible` → devuelve `422 Unprocessable Entity`.
4. Ajusta el stock en ProductService (`PATCH /stock`).
5. Persiste la transacción.

### Editar una Transacción
La edición de una transacción permite modificar únicamente el **detalle** y la **fecha**. Los campos `tipo`, `producto`, `cantidad` y `precio unitario` son **inmutables** una vez registrada la transacción.

**Razón:** Modificar la cantidad o el tipo requeriría revertir el ajuste de stock anterior y aplicar uno nuevo, lo que podría dejar el inventario en estado inconsistente si el ProductService no está disponible en ese instante. Mantener las transacciones como registros de auditoría inmutables garantiza la trazabilidad del inventario.

### Eliminar una Transacción
Al eliminar una transacción, el sistema **revierte automáticamente** el ajuste de stock correspondiente.

---

## Evidencias

### Listado de Productos con paginación y filtros
> Tabla dinámica con columnas: imagen, nombre, categoría, precio, stock y acciones. Panel de filtros avanzados por nombre, categoría, precio mínimo/máximo y stock mínimo.

### Creación de Producto
> Formulario con campos: nombre, descripción, categoría, precio, stock, URL de imagen. Validaciones en tiempo real y preview de imagen.

### Edición de Producto
> Mismo formulario pre-cargado con los datos del producto seleccionado.

### Listado de Transacciones con paginación y filtros
> Tabla dinámica con columnas: fecha, tipo (compra/venta), producto, cantidad, precio unitario, total, detalle y acciones. Filtros por tipo, fecha desde y fecha hasta.

### Creación de Transacción
> Formulario con selección de tipo, producto (muestra stock disponible), cantidad, precio unitario. Validación en frontend que impide vender más stock del disponible. Total calculado automáticamente.

### Edición de Transacción
> Permite modificar únicamente el detalle y la fecha (la cantidad y tipo son inmutables para preservar la auditoría).

### Historial de transacciones por producto (pantalla extra)
> Accesible desde el botón **Historial** en el listado de productos (`/products/:id/history`). Muestra el nombre, categoría, precio y stock actual del producto, seguido de una tabla paginada con todas sus transacciones. Incluye filtros por tipo (compra/venta) y rango de fechas.

### Filtros dinámicos
> Panel colapsable con múltiples criterios de búsqueda en ambas secciones.

---

## Navegación

| Ruta | Descripción |
|---|---|
| `/products` | Listado de productos con filtros y paginación |
| `/products/new` | Formulario de creación de producto |
| `/products/:id/edit` | Formulario de edición de producto |
| `/products/:id/history` | Historial de transacciones del producto |
| `/transactions` | Listado de transacciones con filtros y paginación |
| `/transactions/new` | Formulario de creación de transacción |
| `/transactions/:id/edit` | Formulario de edición de transacción |

---

## Estructura del Proyecto

```
microservicios-test/
├ init.sql                      # Script de creación de BD (raíz del proyecto)
├ database/
│   └ init.sql                  # Copia de referencia
├ backend/
│   ├ InventoryManagement.sln
│   ├ ProductService/           # Microservicio de productos (puerto 5001)
│   └ TransactionService/       # Microservicio de transacciones (puerto 5002)
├ frontend/
│   └ inventory-app/            # Aplicación Angular 21
├ docker-compose.yml
└ README.md
```

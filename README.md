# 🎓 Portal Académico USMP - Práctica 2

![.NET Core](https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)
![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap_5-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white)
![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)

Plataforma académica desarrollada en **ASP.NET Core 8 MVC** para la Práctica 2 del curso de **Programación I**. Este sistema permite a los estudiantes visualizar un catálogo de cursos, gestionar sus matrículas y a los coordinadores administrar la oferta académica.

---

## 🚀 Funcionalidades Principales

El proyecto ha sido dividido y desarrollado en base a 6 requerimientos clave:

- **🔐 Autenticación y Autorización (Identity):** Registro de usuarios y control de acceso basado en roles (`Coordinador` y `Estudiante`). Sembrado automático de base de datos (`app.db`).
- **📚 Catálogo de Cursos (UI/UX):** Visualización en tarjetas dinámicas con filtros por nombre, rango de créditos y horario de inicio.
- **✅ Sistema de Matrículas Inteligente:** 
  - Validación de cupos máximos en tiempo real.
  - Validación algorítmica de solapamiento de horarios (evita cruces).
- **⚡ Rendimiento y Caché (Redis):** 
  - Caché distribuida para acelerar la carga del catálogo (expira cada 60s).
  - Manejo del estado de sesión mediante Redis para mantener un historial de navegación (últimos cursos visitados).
- **🛡️ Panel Administrativo:** CRUD completo para la gestión de cursos, activación/desactivación y administrador de matrículas con cambio de estado (Pendiente, Confirmada, Cancelada) exclusivo para coordinadores.
- **🐳 Despliegue en la Nube:** Empaquetado `Dockerfile` optimizado y preparado para desplegarse como Web Service en **Render.com**.

---

## 🛠️ Tecnologías y Arquitectura

*   **Framework:** .NET 8 (ASP.NET Core MVC)
*   **Base de Datos Relacional:** SQLite (vía Entity Framework Core)
*   **Base de Datos en Memoria:** RedisLabs (vía `StackExchange.Redis`) para Caché y Sesiones.
*   **Frontend:** HTML5, CSS3, Bootstrap 5 y Bootstrap Icons.
*   **Contenedores:** Docker

---

## 💻 Ejecución en Entorno Local (Desarrollo)

Para probar la aplicación en tu máquina local, sigue estos pasos:

### 1. Prerrequisitos
*   Tener instalado [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
*   Tener instanciado un clúster de Redis (por ejemplo, en [RedisLabs](https://redis.com/try-free/) o en local) y configurar su string de conexión en el archivo `appsettings.json`.

### 2. Pasos de instalación

1. **Clona el repositorio:**
   ```bash
   git clone https://github.com/CsarG16/Practica-2.git
   cd Practica-2
   ```

2. **Compila la aplicación (restaurará las dependencias de NuGet automáticamente):**
   ```bash
   dotnet build
   ```

3. **Ejecuta la aplicación:**
   ```bash
   dotnet run
   ```
   *Nota: La primera vez que se ejecute, Entity Framework generará automáticamente el archivo `app.db` y sembrará los cursos iniciales y el usuario coordinador.*

### 3. Credenciales de prueba
*   **Coordinador:** `coordinador@usmp.pe` / Password: `Admin123!`
*   **Estudiantes:** Requiere que crees una cuenta nueva desde el portal usando el botón "Register".

---

## ☁️ Despliegue en Render (Producción)

El proyecto incluye un `Dockerfile` multietapa optimizado para ser subido directamente a Render.

### Pasos para desplegar:

1. Entra a tu Dashboard en [Render.com](https://render.com/).
2. Haz clic en **New +** y selecciona **Web Service**.
3. Conecta este repositorio de GitHub asegurándote de elegir la rama `main`.
4. En **Environment**, elige la opción `Docker`.
5. En la sección **Environment Variables** añade las siguientes claves:

| Key | Value | Descripción |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Habilita optimizaciones en ASP.NET |
| `ASPNETCORE_URLS` | `http://0.0.0.0:${PORT}` | Permite que Render asigne el puerto nativo de la máquina al contenedor. |
| `ConnectionStrings__RedisConnection` | *[Tu Cadena de conexión de RedisLabs]* | Obligatorio. En el formato: `URL:PUERTO,password=TU_PASSWORD` |

*(No es necesario declarar la variable de entorno `DefaultConnection` para SQLite en Render de forma obligatoria, ya que en cada nueva instancia se creará la ruta del archivo `app.db` y se poblará en el arranque).*

6. Presiona **Create Web Service**. 

---
> Elaborado para la Universidad de San Martín de Porres (USMP) - Curso de Programación I 🎓

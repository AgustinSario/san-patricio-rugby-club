# Deploy_SelfContained 23/04/2026

Esta carpeta contiene una publicación de tipo **"Self-Contained"** (autónoma) del sistema San Patricio Rugby Club.

Esto significa que incluye:

1.  **La Aplicación**: Tus DLLs (`SanPatricioRugby.Web.dll`, `SanPatricioRugby.BLL.dll`, `SanPatricioRugby.DAL.dll`, etc.) y archivos de configuración como `appsettings.json`.
2.  **Todo el Runtime de .NET 9**: Todas las librerías de sistema (`System.*.dll`, `Microsoft.*.dll`) y el motor de ejecución (`coreclr.dll`, `clrjit.dll`).
3.  **Un archivo comprimido**: Hay un archivo llamado `Deploy_SelfContained.rar` (de unos 62MB) que contiene todo lo de la carpeta listo para descargar y mover al servidor.

### La ventaja de esta publicación:
Si el servidor donde vas a instalar la app no tiene instalado .NET 9, esta versión funcionará igual porque lleva **"su propio motor"** adentro. 

### Instrucciones de Instalación:
1.  Copiar el contenido de la carpeta (o descomprimir el `.rar`) al servidor de destino.
2.  Configurar el sitio en **IIS** (Internet Information Services).
3.  Asegurarse de que la cadena de conexión en `appsettings.json` apunte a la base de datos de producción.

---
*Generado automáticamente para el despliegue del 23 de abril de 2026.*

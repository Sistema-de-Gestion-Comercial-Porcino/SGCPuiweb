# SGCPuiweb

Web responsiva del Sistema de Gestión Comercial Porcino. ASP.NET Core MVC 10, controladores, ViewModels dentro de `Models/ViewModels/`, DTO separados y clientes HTTP tipados. **Sin EF Core ni conexión directa a la base de datos.**

## Equipo

- Dario Haziel Tambo Parra
- Angel Sebastian Eguez Mur
- Sofia Cuellar Roca
- Victor Manuel Manrique Arancivia

## Ejecutar

Primero ejecuta SGCPapi con su base MySQL preparada. En este repositorio:

```bash
dotnet restore
dotnet build --warnaserror
dotnet run --project src/SGCP.Web
```

Abre `http://localhost:5080`. La configuración `Api:UrlBase` apunta a `http://localhost:5081/api/v1/`. Para otra máquina configúrala con la dirección de la API, mediante `Api__UrlBase` o un archivo `appsettings.Local.json` ignorado por Git. El proceso MVC es quien se conecta a la API: la URL debe ser accesible desde el servidor de la web.

## Sprint 1

- Acceso, registro de comprador, recuperación y cierre de sesión.
- Resumen con indicadores reales y filtrados por permisos.
- Usuarios y responsabilidades, clientes, clasificaciones y productos.
- Costos/precios con historial de vigencias y confirmación previa de margen no positivo.
- Cotización con múltiples productos, presentación, aceptación/rechazo y nueva versión.
- Pedido confirmado desde cotización aceptada, importe, margen interno conservado y cancelación con motivo.
- Portal del comprador limitado a su catálogo, cotizaciones y pedidos.

La interfaz sigue la organización de los mockups: barra lateral, gestión comercial, formularios y seguimiento por estado. Se diseñó para escritorio y móvil. Los módulos de otros sprints aparecen pendientes para el personal y no ejecutan operaciones ficticias.

Moneda Bs; cantidades siempre con unidad; fechas mostradas en hora de Bolivia (UTC−4). Al enviar se convierten a UTC. El costo de referencia no se muestra al comprador.

## Sprint 2

Pantallas de producción e inventario (S2-P2), contra la API de P1 sin tocar la base de datos:

- **Sanidad** (`/Sanidad`): animales con su estado y control sanitario con resultado, responsable y observación. Un animal rechazado se ve rechazado, y al intentar faenarlo la pantalla muestra el motivo del rechazo (`ANIMAL_RECHAZADO`).
- **Producción** (`/Produccion`): órdenes de faeneo y fabricación con responsable y tiempos estimado y real, previsiones que no suman disponibilidad, insumos consumidos con referencia única y resultados con **varios productos en lotes distintos**.
- **Pesajes y entradas** (`/Almacen/Pesajes`, `/Almacen/Entradas`): dos pantallas separadas. El pesaje guarda bruto, tara y neto **sin cambiar el stock**; la entrada física suma stock una sola vez por lote y producto, y si ya existe dice cuándo y quién la registró (`ENTRADA_YA_REGISTRADA`).
- **Existencias** (`/Almacen/Existencias`): física apta, reservada y disponible (física − reservada), sin cantidades previstas.
- **Control de frío** (`/Frio`): registro manual de temperatura en °C con evaluación humana; el sistema no mide ni evalúa por umbral.
- **Cargos** (detalle del pedido): cargo por productos y cargo por servicio de faeneo o corte en líneas separadas; el servicio solo se registra cuando se acordó y se prestó.
- **Menú por permiso**: producción no ve pesajes ni entradas; almacén no ve precios de venta. Cobranza, transporte y reportes siguen pendientes.

Todo `[Range]` sobre `decimal` lleva `ParseLimitsInInvariantCulture` y `ConvertValueInInvariantCulture`; probar con `LANG=es_BO.UTF-8`.

## Seguridad y validación

Cookie HttpOnly protegida por ASP.NET Core Data Protection; JWT utilizado por el servidor al consumir la API. Políticas en MVC y comprobación definitiva en la API. Formularios con antiforgery; redirecciones de retorno únicamente locales. Sesión sin renovación deslizante; una sesión revocada redirige al acceso en la siguiente llamada protegida.

En producción usa HTTPS tanto en web como en API y conserva las claves de Data Protection del despliegue. No se incluyen contraseñas, tokens ni datos de conexión a la base de datos en este repositorio.

La recuperación local se explica en el README de SGCPapi. Para entregar correos a compradores reales se requiere configurar SMTP en la API.

## Demostración

1. Entrar como administrador; crear un usuario de ventas y un cliente.
2. Crear clasificación/producto sin existencia, o usar los ejemplos opcionales.
3. Registrar costo Bs 28 y precio Bs 35 por kg.
4. Crear cotización de 4 kg; demostrar que presentada/rechazada no crea pedido.
5. Preparar nueva versión, presentarla y aceptarla; confirmar el pedido.
6. Ver margen Bs 28. Cambiar el precio/costo del catálogo y comprobar que sigue en Bs 28.
7. Entrar como comprador y comprobar que solo ve lo suyo, sin costos ni margen.

No mezclar este escenario con una vigencia de costo Bs 30 antes de confirmar: el resultado sería otro margen. Los importes son ilustrativos.

## Ejecución paso a paso

La API tiene que estar corriendo antes que la web. La guía completa del equipo está en `SGCPdocumentacion/00_Definicion/Como_ejecutar_el_sistema.md`.

El motor pasó de SQL Server a MySQL el 6 de noviembre de 2026. Qué se cambió, por qué el proveedor de Oracle y no Pomelo, y las tres trampas de MySQL: `SGCPdocumentacion/00_Definicion/Cambio_a_MySQL.md`.

## Control de versiones

Formato `vMAYOR.MENOR.PARCHE`, donde **MAYOR es el número de sprint**:

| Posición | Significado | Ejemplo |
|---|---|---|
| MAYOR | número de sprint. `0` es el trabajo anterior a la primera entrega | `v2.4.0` pertenece al Sprint 2 |
| MENOR | número de parte del reparto: P1, P2, P3 o P4 | `v2.1.0` es la parte 1 del Sprint 2 |
| PARCHE | corrección sobre esa misma parte, sin agregar funcionalidad | `v2.1.1` |

Los cuatro repositorios comparten MAYOR y MENOR, porque el sprint y la parte son los mismos para todos; el PARCHE avanza por separado en cada repositorio.

**La etiqueta de cierre de un sprint es `vN.4.0`**, la versión del commit de P4, que es siempre el último. Desde el Sprint 2 no se crea `vN.0.0`: quedaría por detrás de versiones ya entregadas. El Sprint 1 conserva su `v1.0.0` porque se entregó en un solo bloque.

Cada versión de la tabla tiene su etiqueta en git, así que `git show v1.0.0` muestra el código exacto de esa fila. **La etiqueta se crea después del commit**, nunca antes, o apunta al estado anterior:

```bash
git commit -m "v1.0.0 - Sprint 1"
git tag -a v1.0.0 -m "Sprint 1 entregado"
git push origin main --follow-tags
```

En la columna *Cambio*, los prefijos son fijos para que la tabla se pueda leer de un tirón: `Corrección:`, `Documentación:`, `Infraestructura:`, o nada cuando es funcionalidad nueva. Si el cambio llegó por una rama, la fila termina en `(PR #n)`; la rama en sí no se anota, porque git ya la guarda en el commit de fusión.

Desde `v1.0.0` el prefijo es `v` sin punto, que es la convención y lo que admite una etiqueta de git. Las filas anteriores se conservan como se escribieron.

### Historial

Más reciente arriba.

| Versión | Fecha | Sprint | Cambio | Autor |
|---|---|---|---|---|
| v2.2.0 | 2026-11-07 | 2 | Pantallas de sanidad, producción, pesajes, entradas físicas, existencias, control de frío y cargos separados; menú por permiso | Angel Sebastian Eguez Mur |
| v1.0.1 | 2026-11-07 | 1 | Corrección: el esquema de versiones aclara que MENOR es el número de parte y que la etiqueta de cierre del sprint es `vN.4.0` | Dario Haziel Tambo Parra |
| v1.0.0 | 2026-11-06 | 1 | Documentación: convención de versionamiento del repositorio | Dario Haziel Tambo Parra |
| v.0.1.0 | 2026-11-06 | 1 | Pantallas de acceso, usuarios, clientes, clasificaciones, productos, precios, cotizaciones y pedidos, con enlace de decimales independiente de la configuración regional | Dario Haziel Tambo Parra |

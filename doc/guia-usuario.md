# Guia de Usuario - POC Mercado Pago

## Que es esta aplicacion?

Esta es una **tienda online de prueba** (e-commerce) que permite comprar productos y pagar usando **Mercado Pago**. Es una demostracion tecnica que muestra como integrar pagos en una aplicacion web.

---

## Indice

1. [Que puedo hacer con esta aplicacion?](#1-que-puedo-hacer-con-esta-aplicacion)
2. [Como funciona el proceso de compra?](#2-como-funciona-el-proceso-de-compra)
3. [Metodos de pago disponibles](#3-metodos-de-pago-disponibles)
4. [Guia paso a paso](#4-guia-paso-a-paso)
5. [Preguntas frecuentes](#5-preguntas-frecuentes)
6. [Glosario de terminos](#6-glosario-de-terminos)

---

## 1. Que puedo hacer con esta aplicacion?

### Funcionalidades principales

| Funcion | Descripcion |
|---------|-------------|
| Ver productos | Navegar por el catalogo de productos disponibles |
| Agregar al carrito | Seleccionar productos para comprar |
| Modificar carrito | Cambiar cantidades o eliminar productos |
| Pagar con Mercado Pago | Completar la compra usando tu cuenta de MP |
| Pagar con QR | Escanear un codigo QR con la app de Mercado Pago |

### Que NO hace esta aplicacion

- No procesa pagos reales (es solo una demostracion)
- No tiene sistema de usuarios ni login
- No envia productos fisicos
- No guarda historial de compras permanente

---

## 2. Como funciona el proceso de compra?

### Diagrama simplificado

```
  +-------------+     +-------------+     +-------------+     +-------------+
  |   1. Ver    |     |  2. Agregar |     |  3. Revisar |     |   4. Pagar  |
  |  productos  | --> |  al carrito | --> |   carrito   | --> |     con MP  |
  +-------------+     +-------------+     +-------------+     +-------------+
                                                                     |
                                                                     v
                                                              +-------------+
                                                              | 5. Compra   |
                                                              |  completada |
                                                              +-------------+
```

### Explicacion de cada paso

#### Paso 1: Ver productos
- Al abrir la aplicacion, veras un catalogo de productos
- Cada producto muestra su nombre, descripcion y precio
- Los precios estan en pesos argentinos (ARS)

#### Paso 2: Agregar al carrito
- Haz clic en "Agregar al carrito" en el producto que desees
- Puedes agregar multiples productos
- Puedes agregar el mismo producto varias veces

#### Paso 3: Revisar carrito
- Haz clic en el icono del carrito para verlo
- Puedes modificar las cantidades
- Puedes eliminar productos
- Veras el total a pagar

#### Paso 4: Pagar
- Elige como quieres pagar:
  - **Checkout Pro**: Te redirige a la pagina de Mercado Pago
  - **Codigo QR**: Escaneas un QR con la app de Mercado Pago

#### Paso 5: Compra completada
- Despues de pagar, veras una pagina de confirmacion
- Si el pago fue exitoso: mensaje de exito
- Si el pago fallo: mensaje de error

---

## 3. Metodos de pago disponibles

### Opcion 1: Checkout Pro (Redireccion)

**Como funciona:**
1. Haces clic en "Pagar"
2. Se abre la pagina de Mercado Pago
3. Eliges como pagar (tarjeta, dinero en cuenta, etc.)
4. Completas el pago en Mercado Pago
5. Vuelves automaticamente a la tienda

**Ventajas:**
- Puedes usar cualquier medio de pago de MP
- No necesitas tener la app de MP instalada
- Funciona desde cualquier navegador

**Cuando usarlo:**
- Compras desde una computadora
- No tienes la app de Mercado Pago
- Prefieres pagar con tarjeta de credito

### Opcion 2: Pago con QR

**Como funciona:**
1. Haces clic en "Pagar con QR"
2. Aparece un codigo QR en pantalla
3. Abres la app de Mercado Pago en tu celular
4. Escaneas el codigo QR
5. Confirmas el pago en la app
6. La pagina se actualiza automaticamente

**Ventajas:**
- Pago rapido desde el celular
- No necesitas ingresar datos de tarjeta
- Pagas con el saldo de tu cuenta MP

**Cuando usarlo:**
- Tienes la app de Mercado Pago instalada
- Tienes saldo en tu cuenta de MP
- Quieres un pago rapido sin ingresar datos

---

## 4. Guia paso a paso

### 4.1 Realizar una compra con Checkout Pro

```
Paso 1: Abrir la tienda
        - Ingresa a la direccion de la aplicacion en tu navegador

Paso 2: Seleccionar productos
        - Navega por el catalogo
        - Haz clic en "Agregar al carrito" en los productos deseados

Paso 3: Abrir el carrito
        - Haz clic en el icono del carrito (esquina superior)
        - Revisa los productos y cantidades
        - Verifica el total

Paso 4: Iniciar el pago
        - Haz clic en "Pagar con Mercado Pago"
        - Espera a que aparezca el boton de MP

Paso 5: Completar en Mercado Pago
        - Haz clic en el boton de Mercado Pago
        - Inicia sesion si es necesario
        - Selecciona tu metodo de pago
        - Confirma el pago

Paso 6: Verificar resultado
        - Seras redirigido a la tienda
        - Veras un mensaje indicando si el pago fue exitoso
```

### 4.2 Realizar una compra con QR

```
Paso 1: Abrir la tienda
        - Ingresa a la direccion de la aplicacion en tu navegador
        - Puedes usar una computadora o tablet

Paso 2: Seleccionar productos
        - Navega por el catalogo
        - Haz clic en "Agregar al carrito" en los productos deseados

Paso 3: Abrir el carrito
        - Haz clic en el icono del carrito
        - Revisa los productos y el total

Paso 4: Generar el QR
        - Haz clic en "Pagar con QR"
        - Espera a que aparezca el codigo QR en pantalla
        - NO cierres esta ventana

Paso 5: Escanear con la app
        - Abre la app de Mercado Pago en tu celular
        - Toca en "Escanear" o "Pagar con QR"
        - Apunta la camara al codigo QR de la pantalla
        - Verifica el monto y confirma el pago

Paso 6: Esperar confirmacion
        - La pagina se actualizara automaticamente
        - Veras un mensaje de pago exitoso
        - Seras redirigido a la pagina de confirmacion
```

### 4.3 Modificar el carrito

```
Para cambiar la cantidad de un producto:
        - Abre el carrito
        - Usa los botones + y - junto al producto
        - El total se actualiza automaticamente

Para eliminar un producto:
        - Abre el carrito
        - Haz clic en el icono de basura junto al producto

Para vaciar todo el carrito:
        - Abre el carrito
        - Haz clic en "Vaciar carrito"
```

---

## 5. Preguntas frecuentes

### Sobre la aplicacion

**P: Es una tienda real?**
R: No, es una demostracion tecnica. Los productos y pagos son de prueba.

**P: Puedo comprar productos reales?**
R: No, esta aplicacion no vende productos reales ni procesa pagos reales en produccion.

**P: Se guardan mis datos?**
R: No se guardan datos personales. El carrito se borra al cerrar el navegador.

### Sobre el pago

**P: Que pasa si el pago falla?**
R: Veras un mensaje de error. Puedes intentar de nuevo o elegir otro metodo de pago.

**P: Puedo cancelar un pago?**
R: Si estas en la pagina de QR, puedes hacer clic en "Cancelar" para volver al carrito.

**P: Por que no aparece el boton de Mercado Pago?**
R: Puede ser un problema de conexion. Recarga la pagina e intenta de nuevo.

**P: El QR no se escanea, que hago?**
R: Asegurate de tener buena iluminacion y que el QR este completo en pantalla.

### Sobre los metodos de pago

**P: Necesito cuenta de Mercado Pago?**
R: Para Checkout Pro puedes pagar como invitado. Para QR necesitas la app con cuenta.

**P: Puedo pagar con tarjeta de credito?**
R: Si, en Checkout Pro puedes usar tarjetas de credito y debito.

**P: Que moneda se usa?**
R: Todos los precios estan en pesos argentinos (ARS).

---

## 6. Glosario de terminos

### Terminos de la aplicacion

| Termino | Significado |
|---------|-------------|
| Carrito | Lugar donde se guardan los productos antes de pagar |
| Checkout | Proceso de finalizar la compra y pagar |
| Orden | Registro de una compra realizada |
| Total | Suma de todos los productos en el carrito |

### Terminos de Mercado Pago

| Termino | Significado |
|---------|-------------|
| Checkout Pro | Metodo de pago que redirige al sitio de Mercado Pago |
| QR Dinamico | Codigo QR unico generado para cada compra |
| Preferencia | Configuracion de pago creada para cada compra |
| Webhook | Notificacion automatica que MP envia cuando se paga |

### Terminos tecnicos (simplificados)

| Termino | Significado |
|---------|-------------|
| API     | Sistema que permite que dos aplicaciones se comuniquen |
| Backend | La parte del sistema que procesa la informacion (no visible) |
| Frontend| La parte del sistema que ves en pantalla |
| Session | Memoria temporal que guarda tu carrito mientras navegas |

---

## Resumen visual del proceso

```
+------------------+
|   CATALOGO       |
|   [Productos]    |
+--------+---------+
         |
         | Agregar
         v
+--------+---------+
|   CARRITO        |
|   [Items + Total]|
+--------+---------+
         |
         | Pagar
         v
+--------+---------+
| ELEGIR METODO    |
+--+----------+----+
   |          |
   v          v
+--+---+  +---+----+
|Checkout| |  QR   |
| Pro    | |       |
+--+---+  +---+----+
   |          |
   v          v
+--+----------+----+
| PAGO EN MP       |
| (externo o app)  |
+--------+---------+
         |
         | Confirmacion
         v
+--------+---------+
| RESULTADO        |
| Exito / Error    |
+------------------+
```

---

## Soporte

Esta es una aplicacion de demostracion. Si tienes dudas tecnicas sobre la implementacion, consulta los archivos de documentacion tecnica:

- `integracion-checkoutPro.md` - Detalles de Checkout Pro
- `integracion-pagoQr.md` - Detalles de pago con QR
- `implementacion.md` - Documentacion tecnica completa

---

## Notas importantes

1. **Ambiente de pruebas**: Esta aplicacion usa credenciales de prueba de Mercado Pago. Los pagos no son reales.

2. **Test Users**: Para probar pagos con QR, se necesitan usuarios de prueba de Mercado Pago.

3. **Limitaciones**: El carrito se pierde al cerrar el navegador. No hay persistencia de datos entre sesiones.

4. **Proposito educativo**: Esta POC fue creada para demostrar la integracion tecnica con Mercado Pago.

# Especificaciones oficiales de la AEAT

Registro de los documentos normativos usados para implementar el motor fiscal.
Su purpose es la **trazabilidad**: saber qué versión se implementó y contra qué
se verificó.

Fecha de descarga de todos los ficheros: **2026-10-12**.
Página de la AEAT: <https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/informacion-tecnica.html>

---

## 1. Huella o hash — VERIFICADO

| | |
|---|---|
| Documento | Detalle de las especificaciones técnicas para generación de la huella o hash de los registros de facturación |
| Versión | **0.1.2** de 27/08/2024 |
| URL | `https://www.agenciatributaria.es/static_files/AEAT_Desarrolladores/EEDD/IVA/VERI-FACTU/Veri-Factu_especificaciones_huella_hash_registros.pdf` |
| Fichero local | `huella_aeat_v0.1.2_texto_extraido.txt` |
| SHA-256 | `a4f5bab391d7c980a3898a8a2e8e224251b721c3de093751997fb01802196e53` |

Historial de revisiones del propio documento:

| Edic. | Fecha | Descripción |
|---|---|---|
| 0.1.0 | 27/05/2024 | Versión inicial |
| 0.1.1 | 10/06/2024 | Corrección campos ejemplos hash anulación |
| 0.1.2 | 27/08/2024 | Aclaración del tratamiento de valores numéricos; recodificación del ejemplo |

> El texto se extrajo con Word porque el PDF usa fuentes con codificación propia
> y los extractores habituales solo recuperan el índice. Por eso el fichero local
> es texto extraído, no el PDF original, y su SHA-256 **no** es el del PDF.

### Qué se implementó

- Algoritmo SHA-256, el único permitido.
- Cadena canónica: pares `nombreCampo=valor` unidos por `&`, en el orden normativo.
- Alta: 8 campos. Anulación: 5 campos. Nombres tomados del XSD.
- Salida en hexadecimal **mayúsculas**, 64 caracteres.
- Valores recortados; el par vacío se conserva (`Huella=`).
- Codificación UTF-8 antes de aplicar el algoritmo.

### Contraste con vectores oficiales

| Caso | Apartado | Resultado |
|---|---|---|
| Primer alta | 6.1 | `3C464DAF…8F12F60` — coincide |
| Alta encadenada | 6.2 | `F7B94CFD…5802A2B97` — coincide |
| Anulación | 6.3 | `177547C0…88F90C68` — coincide |

Clases: `AeatRecordCanonicalizer`, `Sha256FingerprintCalculator`.
Pruebas: `FingerprintOfficialVectorsTests`.

---

## 2. Esquemas XSD — VERIFICADO (compilan)

Fuente: AEAT, *Esquemas de los servicios web*.
Directorio: `xsd/`.

| Fichero | Bytes | SHA-256 |
|---|---|---|
| `SuministroLR.xsd` | 1573 | `cbdac8d427cc5ab5d77ca48974cab0f35d6bb819c4c66db361681e3710aeba36` |
| `SuministroInformacion.xsd` | 49540 | `ee4c1655175644de44c4c25055ffeb8e5f4bb4bc3834ce8254d4222ef18c8aa1` |
| `RespuestaSuministro.xsd` | 6259 | `82acf80f785643caac13087aae66808ed721a13f08ca5218cf8ae81b695549ef` |
| `ConsultaLR.xsd` | 3886 | `bf2cdb8fc4b95b291757a72b76d8fffca06a6d30d9329122ca2fd6b2d5f8f1b1` |
| `RespuestaConsultaLR.xsd` | 10058 | `de35063acb8d9ba0d6ae51acc6b595de9c2b12333250e95e13108ef5f2670d45` |
| `EventosSIF.xsd` | 30873 | `cc7347c6a9a57a0c8edbc6b9ddcce55176452d0db0e68369477e207e9fbdd7e7` |

### Dependencias

```
SuministroLR.xsd            -> SuministroInformacion.xsd
SuministroInformacion.xsd   -> http://www.w3.org/TR/xmldsig-core/xmldsig-core-schema.xsd
EventosSIF.xsd              -> http://www.w3.org/TR/xmldsig-core/xmldsig-core-schema.xsd
RespuestaSuministro.xsd     -> SuministroInformacion.xsd, SuministroLR.xsd
RespuestaConsultaLR.xsd     -> SuministroInformacion.xsd
ConsultaLR.xsd              -> SuministroInformacion.xsd
```

### `xmldsig-core-schema.xsd` — modificado a propósito

| | |
|---|---|
| Origen | `https://www.w3.org/TR/xmldsig-core/xmldsig-core-schema.xsd` |
| SHA-256 del original W3C | `d102ad3df7664c307e0c2c776ba4a90513b1969974d8a940bae1a77f9f21e15d` |
| SHA-256 del local | `60ca42bebd2ace722f54ced9928b63cbf2b08405c66d91163716fc4ae55ef566` |

**Modificación: se ha eliminado la declaración `<!DOCTYPE>`.** No es un cambio
cosmético: .NET la bloquea por seguridad para impedir entidades XML externas, y
ese es exactamente el riesgo que un esquema no controlable introduce. Habilitar
DTD en el lector habría sido la alternativa fácil y la equivocada.

Al eliminarlo, los SHA-256 **no coinciden** entre el original de W3C y el local.
Debe tenerse en cuenta al comparar con el upstream.

### Elementos del esquema usados por el proyecto

| Campo oficial | Clase que lo usa |
|---|---|
| `IDEmisorFactura` | `CanonicalizationRequest.IssuerNif` |
| `NumSerieFactura` (serie/número juntos) | `BuildInvoiceNumber` en `InvoiceIssuanceService` |
| `FechaExpedicionFactura` | `CanonicalizationRequest.IssueDate` (`dd-MM-yyyy`) |
| `TipoFactura` | `CanonicalizationRequest.InvoiceType` |
| `CuotaTotal` | `CanonicalizationRequest.TaxTotal` |
| `ImporteTotal` | `CanonicalizationRequest.TotalAmount` |
| `Huella` | `CanonicalizationRequest.PreviousHash`, `FiscalRecord.Hash` |
| `FechaHoraHusoGenRegistro` | `CanonicalizationRequest.GeneratedAt` |
| `TipoHuella` (`01`) | `FiscalSystemIdentification.HashType` |
| `IDVersion` (`1.0`) | `FiscalSystemIdentification.IdVersion` |
| `NombreSistemaInformatico` | `FiscalSystemIdentification.SystemName` (máx. 30) |
| `IdSistemaInformatico` | `FiscalSystemIdentification.SystemId` (máx. 2) |
| `NumeroInstalacion` | `FiscalSystemIdentification.InstallationNumber` |
| `NombreRazon` + `NIF`\|`IDOtro` | `FiscalSystemIdentification.Producer*` |

---

## 3. Estructura de remisión

Raíz: `RegFactuSistemaFacturacion`, con `Cabecera` y hasta 1000 `RegistroFactura`,
cada uno un `choice` entre `RegistroAlta` y `RegistroAnulacion`.

Las rutas del PDF de la huella se han contrastado contra el esquema y coinciden:
existen `RegistroAlta/IDFactura/IDEmisorFactura` y
`RegistroAlta/Encadenamiento/RegistroAnterior/Huella`.

---

## 4. Documentos pendientes de descargar

| Documento | Para qué | Estado |
|---|---|---|
| Documento de validaciones y errores | Reglas que un XSD no puede expresar | **no descargado** |
| Descripción de servicios web (PDF) | Contrato SOAP; se cites v1.0.3 | **no descargado** |
| Características del QR | URL de cotejo y contenido | **no descargado** |
| WSDL de servicios web | Contrato de operación | **no descargado** |
| Especificación de firma electrónica | Solo en modalidad NO VERI\*FACTU | **no descargado** |

Los cuatro primeros están pendientes **por documentación**. El quinto depende
además de una decisión funcional: la modalidad elegida.

---

## Estado de los componentes

```
Verifier                        Available
AltaRecordBuilder               Available
AnulacionRecordBuilder          Available
IRecordCanonicalizer            Available_Verified
Sha256FingerprintCalculator     Available_Verified
FiscalChainWalker               Available
IXsdValidator                   pendiente de implementar (esquemas ya compilan)
IRecordXmlBuilder               pendiente de implementar
IQrGenerator                    pendiente por documentación
```

`IsReadyForIssuance` permanece en `false`.
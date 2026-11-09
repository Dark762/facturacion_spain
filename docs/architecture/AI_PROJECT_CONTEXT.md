# Contexto y directrices del proyecto Facturacion Spain

> Documento de contexto portable para asistentes de IA y colaboradores.
> Estas directrices expresan la visión y las restricciones arquitectónicas del producto.
> Antes de implementar, verificar siempre el estado real del código y la normativa vigente.
> Repositorio: Dark762/facturacion_spain. Rama de trabajo: `develop`; no modificar `main` sin autorización explícita.

## 1. Visión del producto

Este proyecto tiene como objetivo convertirse en un **SaaS de facturación comercial para autónomos, pymes y empresas en España**.

**Verifactu es una funcionalidad de cumplimiento normativo, no el producto principal.**

El sistema debe conservar su utilidad comercial y operativa incluso si en el futuro Verifactu se modifica, se retrasa, deja de ser obligatorio o es derogado.

Todas las decisiones arquitectónicas deben tener en cuenta esta perspectiva a largo plazo.

## 2. Arquitectura

El proyecto está desarrollado en C#/.NET y organizado en diferentes componentes, entre ellos:

- Facturacion.Api
- Facturacion.Application
- Facturacion.Domain
- Facturacion.Infrastructure
- Facturacion.Verifactu
- Facturacion.B2B
- Facturacion.Payments.Stripe
- Facturacion.Workers

Antes de proponer cambios, inspecciona la implementación real del repositorio. No presupongas que una funcionalidad está implementada únicamente porque exista su proyecto o carpeta.

### Principios fundamentales

1. **Independencia de Verifactu:** el núcleo de facturación debe funcionar sin depender directamente de Verifactu, siempre dentro de las modalidades legalmente permitidas.
2. **Arquitectura modular:** las integraciones fiscales deben estar desacopladas del dominio y de los procesos generales de facturación.
3. **Preparación para cambios normativos:** los requisitos fiscales deben poder evolucionar sin necesidad de reescribir el sistema completo.
4. **Multi-tenant:** diseñar para múltiples empresas y organizaciones, garantizando el aislamiento seguro de sus datos.
5. **Escalabilidad:** permitir el crecimiento en clientes, facturas y operaciones sin rediseños innecesarios.
6. **Mantenibilidad:** priorizar código limpio, pruebas automatizadas, simplicidad y reutilización.
7. **Compatibilidad:** evitar cambios que rompan contratos públicos, datos históricos o integraciones existentes sin una estrategia de migración.

## 3. Estrategia ante cambios políticos y normativos

El software no debe depender comercialmente de que Verifactu siga siendo obligatorio.

Contemplar los siguientes escenarios:

- Verifactu continúa siendo obligatorio.
- Se modifica su calendario de implantación.
- Se introducen nuevos requisitos técnicos.
- Se establecen modalidades alternativas.
- Se elimina su obligatoriedad.
- Se sustituye por otro sistema de control fiscal.

Estos escenarios son hipótesis de diseño, no predicciones políticas.

**Importante:** la modularidad no autoriza a desactivar controles legales cuando sean obligatorios. Los requisitos de integridad, conservación, trazabilidad, inalterabilidad y demás obligaciones aplicables deben seguir cumpliéndose según la normativa vigente.

## 4. Prioridades del SaaS

Priorizar progresivamente:

1. Motor de facturación fiable y seguro.
2. Gestión de empresas, usuarios, clientes y permisos.
3. Aislamiento multi-tenant.
4. Cumplimiento fiscal verificable.
5. Gestión de suscripciones y planes comerciales.
6. Automatización de procesos y tareas en segundo plano.
7. API para integraciones externas.
8. Experiencia de usuario y funcionalidades comerciales.

No añadir funcionalidades innecesarias ni complejidad prematura.

## 5. Instrucciones para cada tarea de desarrollo

Antes de implementar cualquier cambio:

1. Analiza el código existente y sus dependencias.
2. Determina si la funcionalidad pertenece al núcleo de facturación, a la infraestructura, al SaaS o al cumplimiento fiscal.
3. Evalúa cómo afectaría al sistema una modificación o derogación de Verifactu.
4. Evita introducir dependencias innecesarias con la AEAT en componentes generales.
5. Respeta los patrones y convenciones existentes cuando sean adecuados.
6. Identifica posibles riesgos de seguridad, aislamiento de datos y cumplimiento normativo.
7. Implementa pruebas proporcionadas al riesgo del cambio.
8. Explica las decisiones arquitectónicas relevantes.

No realices refactorizaciones masivas sin justificar su necesidad y alcance.

## 6. Objetivo final

Construir un producto comercial sostenible, seguro, modular y competitivo.

El éxito del proyecto no debe depender exclusivamente de una obligación administrativa concreta.

**Regla principal: Facturacion Spain es un SaaS de facturación con capacidades de cumplimiento fiscal, no una aplicación cuya única razón de existir sea Verifactu.**

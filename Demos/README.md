# Observability Demos — OpenTelemetry Multi-APM

Solución de demostración que muestra cómo instrumentar microservicios .NET con **OpenTelemetry** y enviar métricas, trazas y logs a distintas plataformas APM, todo orquestado con Docker Compose.

## Plataformas soportadas

| Backend | Variable `OTELC_CONFIG` | Trazas | Métricas | Logs |
|---------|------------------------|--------|----------|------|
| [.NET Aspire Dashboard](https://learn.microsoft.com/aspnet/core/diagnostics/dotnet-aspire) | `aspire` | ✓ | ✓ | ✓ |
| [New Relic](https://newrelic.com/) | `newrelic` | ✓ | ✓ | ✓ |
| [OpenObserve](https://openobserve.ai/) | `openobserve` | ✓ | ✓ | ✓ |
| [Elastic APM / ELK](https://www.elastic.co/observability) | `elk` | ✓ | ✓ | ✓ |
| [OpenSearch](https://opensearch.org/) | `opensearch` | ✓ | - | - |
| Todos simultáneamente | `all` | ✓ | ✓ | ✓ |

## Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                        Servicios .NET                        │
│  rating.webapi  │  rating.cli  │  rating.validator.cli       │
│          (OpenTelemetry SDK — OTLP gRPC → :4317)            │
└──────────────────────────┬──────────────────────────────────┘
                           │
                ┌──────────▼──────────┐
                │  OTel Collector     │  :4317 (gRPC)
                │  (otelcol-contrib)  │  :4318 (HTTP)
                └──────┬─────┬───────┘
                       │     │
          ┌────────────┘     └────────────────┐
          │                                   │
   ┌──────▼──────┐                   ┌────────▼────────┐
   │  Prometheus │  :9090            │  APM Backend    │
   │  (métricas) │                   │  (según config) │
   └──────┬──────┘                   └─────────────────┘
          │
   ┌──────▼──────┐
   │   Grafana   │  :3000
   └─────────────┘
```

## Requisitos previos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (con Docker Compose v2)
- .NET 8.0 SDK (solo si compilas localmente)

## Inicio rápido

```bash
# 1. Clona el repositorio y sitúate en la carpeta
cd Demos

# 2. Copia y edita las variables de entorno
cp .env.example .env   # si no existe, edita .env directamente

# 3. Elige el backend APM (ver sección de configuración)
# Edita .env → OTELC_CONFIG=aspire

# 4. Levanta el stack
docker compose up -d

# 5. Accede a Grafana
# http://localhost:3000  (usuario: admin / sin contraseña)
```

---

## Configuración del fichero `.env`

El fichero `.env` en la raíz controla todo el comportamiento del stack:

```dotenv
# ──── Backend APM activo ────────────────────────────────────────
# Valores posibles: aspire | newrelic | openobserve | elk | opensearch | all
OTELC_CONFIG=aspire

# ──── Endpoint del OTel Collector (no cambiar salvo red personalizada) ─
OTELINPUT=http://otelcol:4317

# ──── Prefijo de nombre de servicio ─────────────────────────────
SERVICENAMEPREFIX=des.arc

# ──── Infraestructura ───────────────────────────────────────────
RATING_API=http://rating.webapi:5398
REDISSERVER=redis:6379
RABBIT_SERVER=rabbitmq
```

---

## Configurar New Relic

1. Obtén tu **Ingest License Key** en [one.newrelic.com → API Keys](https://one.newrelic.com/api-keys).

2. Edita [config/apm/newrelic/otelcol-config.yaml](config/apm/newrelic/otelcol-config.yaml) y sustituye el placeholder:

```yaml
exporters:
  otlp/newrelic:
    endpoint: https://otlp.eu01.nr-data.net:4317   # usa otlp.nr-data.net para US
    headers:
      api-key: "<TU_LICENSE_KEY_AQUI>"
```

3. Establece en `.env`:

```dotenv
OTELC_CONFIG=newrelic
```

4. Reinicia el stack:

```bash
docker compose down && docker compose up -d
```

> **Región:** El endpoint `otlp.eu01.nr-data.net` corresponde a la región EU. Para cuentas US usa `otlp.nr-data.net`.

---

## Configurar OpenObserve

1. Arranca OpenObserve (incluido en el perfil `openobserve`) y accede a [http://localhost:5080](http://localhost:5080).  
   Credenciales por defecto: `admin@example.com` / `Complexpass#123`

2. Ve a **Ingestion → OTLP → gRPC** y copia el token de autorización. Tiene el formato:

   ```
   Basic <base64(email:password)>
   ```

   Puedes generarlo en terminal:

   ```bash
   echo -n "admin@example.com:Complexpass#123" | base64
   ```

3. Edita [config/apm/openobserve/otelcol-config.yaml](config/apm/openobserve/otelcol-config.yaml):

```yaml
exporters:
  otlphttp/openobserve:
    endpoint: http://openobserve:5080/api/default
    headers:
      Authorization: "Basic <TOKEN_BASE64>"
      organization: default
      stream-name: default
```

4. Establece en `.env`:

```dotenv
OTELC_CONFIG=openobserve
```

5. Reinicia el stack:

```bash
docker compose down && docker compose up -d
```

---

## Configurar Elastic APM / ELK

Las credenciales por defecto del stack ELK ya están configuradas en `.env`:

```dotenv
ELASTIC_USERNAME=elastic
ELASTIC_PASSWORD=Password1#
```

Para cambiarlas, edita también [config/apm/elk/otelcol-config.yaml](config/apm/elk/otelcol-config.yaml) y el fichero de configuración del APM Server.

Actívalo con:

```dotenv
OTELC_CONFIG=elk
```

---

## Grafana — Cómo añadir un dashboard

### Opción A: Importar desde Grafana.com

1. Abre Grafana en [http://localhost:3000](http://localhost:3000).
2. Ve a **Dashboards → Import**.
3. Introduce el ID del dashboard de [grafana.com/grafana/dashboards](https://grafana.com/grafana/dashboards) o pega el JSON.
4. Selecciona el datasource correspondiente (Prometheus, Elasticsearch, etc.) y haz clic en **Import**.

### Opción B: Provisioning automático (recomendado)

Para que el dashboard se cargue automáticamente al levantar el stack, añade el fichero JSON en:

```
config/stack/grafana/dashboards/
└── mi-dashboard.json
```

Y asegúrate de que existe el fichero de provisioning en [config/stack/grafana-datasources.yaml](config/stack/grafana-datasources.yaml). Si necesitas añadir un provider de dashboards, crea [config/stack/grafana/provisioning/dashboards/default.yaml](config/stack/grafana/provisioning/dashboards/default.yaml):

```yaml
apiVersion: 1
providers:
  - name: default
    type: file
    options:
      path: /var/lib/grafana/dashboards
```

Y en [docker-compose-observability.yml](docker-compose-observability.yml) monta el directorio:

```yaml
grafana:
  volumes:
    - ./config/stack/grafana/dashboards:/var/lib/grafana/dashboards
    - ./config/stack/grafana/provisioning:/etc/grafana/provisioning
```

### Datasources disponibles

| Datasource | URL interna | Uso |
|------------|-------------|-----|
| Prometheus | `http://prometheus:9090` | Métricas |
| Jaeger | `http://jaeger:16686` | Trazas |
| Loki | `http://loki:3100` | Logs |
| New Relic | API externa | Métricas/Trazas (requiere API key) |
| Elasticsearch | `http://elasticsearch:9200` | Logs/Trazas (ELK) |

---

## Puertos expuestos

| Servicio | Puerto | Descripción |
|----------|--------|-------------|
| Grafana | 3000 | Dashboard de visualización |
| Prometheus | 9090 | UI y API de métricas |
| OTel Collector | 4317 | Receptor OTLP gRPC |
| OTel Collector | 4318 | Receptor OTLP HTTP |
| OTel Collector | 8888 | Métricas internas del collector |
| OTel Collector | 9464 | Exportador Prometheus |
| OTel Collector | 13133 | Health check |
| RabbitMQ | 15672 | Management UI (`guest`/`guest`) |
| Redis | 6379 | Cache |
| OpenObserve | 5080 | UI + OTLP HTTP |
| Aspire Dashboard | 18888 | Dashboard Aspire |
| Kibana (ELK) | 5601 | UI de Kibana |

---

## Estructura del proyecto

```
Demos/
├── .env                          # Variables de entorno (no commitear secrets)
├── docker-compose.yml            # Compose principal
├── docker-compose-observability.yml  # Stack de observabilidad base
├── config/
│   ├── apm/                      # Configuración por backend APM
│   │   ├── aspire/
│   │   ├── elk/
│   │   ├── newrelic/
│   │   ├── openobserve/
│   │   └── opensearch/
│   └── stack/                    # Configuración compartida
│       ├── prometheus.yaml
│       └── grafana-datasources.yaml
├── Services/                     # Microservicios .NET
│   ├── Rating.WebApi/
│   ├── Rating.Cli/
│   ├── Rating.Validator.Cli/
│   ├── Rating.BusinessLayer/
│   └── WebOpenObserveDemo/
└── docker_data/                  # Volúmenes persistentes (ignorar en git)
```

---

## Secrets — buenas prácticas

> **Nunca commitees API keys ni contraseñas en el repositorio.**

El fichero `.env` está listado en `.gitignore`. Para entornos de CI/CD, inyecta los secrets como variables de entorno del pipeline o usa un gestor de secretos (Azure Key Vault, HashiCorp Vault, etc.).

Ejemplo de `.env.example` que sí debe estar en el repo:

```dotenv
OTELC_CONFIG=aspire
OTELINPUT=http://otelcol:4317
SERVICENAMEPREFIX=des.arc
# NEW RELIC — reemplaza con tu License Key
NEW_RELIC_API_KEY=
# OPENOBSERVE — token Base64(email:password)
OPENOBSERVE_AUTH_TOKEN=
# ELASTIC
ELASTIC_USERNAME=elastic
ELASTIC_PASSWORD=
```

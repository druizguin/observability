# Observability in Elastic
https://evermight.com/es/servidor-de-flota-de-agentes-docker-elk-2-apm

https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/main/exporter/elasticsearchexporter/README.md

# RUN
docker-compose -p obs_demo_v4 down --remove-orphans --volumes
docker-compose -p obs_demo_v4 up --build -d 


## Extraer datos del certificado:
    en bash:
    ```bash    
    docker cp es01:/usr/share/elasticsearch/config/certs/ca/ca.crt /tmp/.
    openssl x509 -fingerprint -sha256 -noout -in /tmp/ca.crt | awk -F"=" {' print $2 '} | sed s/://g
    cat /tmp/ca.crt
    ```
    **Ejemplo**
    ```bash    
    FINGERTPRINT: 32F007AB281C......XXX
    CERT:
    -----BEGIN CERTIFICATE-----
    MIIDWjCCAkKgAwIBAgIVANQD9+q/YT1lTTrLzxaHcvUFjCqaMA0GCSqGSIb3DQEB
    CwUAMDQxMjAwBgNVBAMTKUVsYXN0aWMgQ2VydGlmaWNhdGUgVG9vbCBBdXRvZ2Vu
    ................................................................
    ................................................................
    ................................................................
    ................................................................
    ................................................................
    ................................................................
    ................................................................
    ................................................................
    ................................................................
    7sequj9FziCZKkf8q+XFOWc8nT5tSmZ6FnA83FWK+iKfHXwxng1FmReGjufoAtgH
    1SIjYrL90Esh8I/kWV7OGMd2hU/kPaRIJfRkoYGv1dPDmKa4QZBUSq3onG2Rt1rL
    aejS1571pDc3VON+KKSpHWTTUwWtqK4PbRGttjeg9NKTrkkGPWVcrXsOyvZ44FMR
    Vbghba0/Jz7XyhR3RNLbTXgIt8GtIn1nqh1VnRnRFxsZWzaaCAtJWVJAA0IN5w==
    -----END CERTIFICATE-----
    ```

## Configurar fleet
Abrir [Elastic](https://localhost:5601/)
    
Configurar en **Fleet / Settings**:

**URL**:  https://es01:9200

**Elasticsearch CA trusted fingerprint (optional)**: 
            32F007AB281C......XXX
    
        Authentication / Server SSL certificate authorities:
            -----BEGIN CERTIFICATE-----
            MIIDWjCCAkKgAwIBAgIVANQD9+q/YT1lTTrLzxaHcvUFjCqaMA0GCSqGSIb3DQEB
            CwUAMDQxMjAwBgNVBAMTKUVsYXN0aWMgQ2VydGlmaWNhdGUgVG9vbCBBdXRvZ2Vu
            ................................................................
            ................................................................
            ................................................................
            ................................................................
            ................................................................
            ................................................................
            ................................................................
            ................................................................
            ................................................................
            7sequj9FziCZKkf8q+XFOWc8nT5tSmZ6FnA83FWK+iKfHXwxng1FmReGjufoAtgH
            1SIjYrL90Esh8I/kWV7OGMd2hU/kPaRIJfRkoYGv1dPDmKa4QZBUSq3onG2Rt1rL
            aejS1571pDc3VON+KKSpHWTTUwWtqK4PbRGttjeg9NKTrkkGPWVcrXsOyvZ44FMR
            Vbghba0/Jz7XyhR3RNLbTXgIt8GtIn1nqh1VnRnRFxsZWzaaCAtJWVJAA0IN5w==
            -----END CERTIFICATE-----

Applicar configuración

## levantar fleet-server

- arrancar el contenedor de fleet-server y esperar sincronización
- en Kibana, Fleet / Agents debe tener un servidoe en: Healhty

    
# Métricas
Se han levantado los siguientes contenedores:

**prometheus**: [http://localhost:9090/](http://localhost:9090/)
Métricas.

**Mimir**: [http://localhost:9009/](http://localhost:9009/)
Almacenamiento a largo plazo de métricas Prometheus.

**Grafana** [http://localhost:3000/](http://localhost:3000/) 
Panel de visualización.



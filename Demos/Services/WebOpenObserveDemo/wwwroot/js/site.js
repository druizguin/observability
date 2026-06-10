// ============================================================================
// Configuración de OpenTelemetry para enviar Logs, Trazas y Métricas a OTEL Collector
// ============================================================================

import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-http';
import { Resource } from '@opentelemetry/resources';
import {
    SEMRESATTRS_SERVICE_NAME,
    SEMRESATTRS_SERVICE_VERSION,
    SEMRESATTRS_DEPLOYMENT_ENVIRONMENT
} from '@opentelemetry/semantic-conventions';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { MeterProvider, PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { LoggerProvider, BatchLogRecordProcessor } from '@opentelemetry/sdk-logs';
import { logs } from '@opentelemetry/api-logs';
import { trace } from '@opentelemetry/api';
import { W3CTraceContextPropagator } from '@opentelemetry/core';

// ============================================================================
// Configuración - USANDO OTEL COLLECTOR VÍA PROXY
// ============================================================================
const OTEL_CONFIG = {
    // Usar proxy local que se comunica con OTEL Collector vía gRPC
    tracesEndpoint: '/api/telemetry/v1/traces',
    logsEndpoint: '/api/telemetry/v1/logs',
    metricsEndpoint: '/api/telemetry/v1/metrics',
    serviceName: 'web-frontend-demo',
    serviceVersion: '1.0.0',
    environment: 'development'
};

// ============================================================================
// Resource común para todos los signals (Trazas, Logs, Métricas)
// ============================================================================
const resource = new Resource({
    [SEMRESATTRS_SERVICE_NAME]: OTEL_CONFIG.serviceName,
    [SEMRESATTRS_SERVICE_VERSION]: OTEL_CONFIG.serviceVersion,
    [SEMRESATTRS_DEPLOYMENT_ENVIRONMENT]: OTEL_CONFIG.environment,
});

// ============================================================================
// 1. Configuración de TRAZAS (Traces)
// ============================================================================
const traceExporter = new OTLPTraceExporter({
    url: OTEL_CONFIG.tracesEndpoint,
    headers: {
        'Content-Type': 'application/json',
    },
});

const tracerProvider = new WebTracerProvider({
    resource: resource,
});

tracerProvider.addSpanProcessor(new BatchSpanProcessor(traceExporter, {
    maxQueueSize: 2048,
    maxExportBatchSize: 512,
    scheduledDelayMillis: 1000,
    exportTimeoutMillis: 30000,
}));

tracerProvider.register({
    propagator: new W3CTraceContextPropagator(),
});

// ============================================================================
// 2. Configuración de LOGS
// ============================================================================
const logExporter = new OTLPLogExporter({
    url: OTEL_CONFIG.logsEndpoint,
    headers: {
        'Content-Type': 'application/json',
    },
});

const loggerProvider = new LoggerProvider({
    resource: resource,
});

loggerProvider.addLogRecordProcessor(new BatchLogRecordProcessor(logExporter, {
    maxQueueSize: 2048,
    maxExportBatchSize: 512,
    scheduledDelayMillis: 1000,
    exportTimeoutMillis: 30000,
}));

logs.setGlobalLoggerProvider(loggerProvider);

const logger = loggerProvider.getLogger('web-frontend-logger', '1.0.0');

// ============================================================================
// 3. Configuración de MÉTRICAS (Metrics)
// ============================================================================
const metricExporter = new OTLPMetricExporter({
    url: OTEL_CONFIG.metricsEndpoint,
    headers: {
        'Content-Type': 'application/json',
    },
});

const metricReader = new PeriodicExportingMetricReader({
    exporter: metricExporter,
    exportIntervalMillis: 15000, // Exportar cada 15 segundos
});

const meterProvider = new MeterProvider({
    resource: resource,
    readers: [metricReader],
});

const meter = meterProvider.getMeter('web-frontend-meter', '1.0.0');

// Crear métricas personalizadas
const pageViewCounter = meter.createCounter('page.views', {
    description: 'Contador de vistas de página',
    unit: '1',
});

const apiCallCounter = meter.createCounter('api.calls', {
    description: 'Contador de llamadas API',
    unit: '1',
});

const apiCallDuration = meter.createHistogram('api.call.duration', {
    description: 'Duración de llamadas API en milisegundos',
    unit: 'ms',
});

const errorCounter = meter.createCounter('errors', {
    description: 'Contador de errores',
    unit: '1',
});

const userInteractionCounter = meter.createCounter('user.interactions', {
    description: 'Contador de interacciones de usuario',
    unit: '1',
});

// ============================================================================
// 4. Instrumentaciones automáticas
// ============================================================================
registerInstrumentations({
    tracerProvider: tracerProvider,
    instrumentations: [
        new FetchInstrumentation({
            propagateTraceHeaderCorsUrls: [
                /localhost:\d+/,
                /https:\/\/localhost:\d+/,
            ],
            clearTimingResources: true,
            ignoreUrls: [
                /\/api\/telemetry\/.*/, // No instrumentar las llamadas al proxy
            ],
            applyCustomAttributesOnSpan: (span, request, result) => {
                const method = typeof request === 'string' ? 'GET' : request.method || 'GET';
                const url = typeof request === 'string' ? request : request.url;

                span.setAttribute('http.method', method);
                span.setAttribute('http.url', url);

                if (result instanceof Response) {
                    span.setAttribute('http.status_code', result.status);
                    span.setAttribute('http.response.status_code', result.status);
                }
            },
        }),
        new XMLHttpRequestInstrumentation({
            propagateTraceHeaderCorsUrls: [
                /localhost:\d+/,
            ],
            ignoreUrls: [
                /\/api\/telemetry\/.*/, // No instrumentar las llamadas al proxy
            ],
        }),
        new DocumentLoadInstrumentation(),
        new UserInteractionInstrumentation({
            eventNames: ['click', 'submit', 'keypress', 'change'],
            shouldPreventSpanCreation: (eventType, element, span) => {
                // Filtrar eventos triviales
                if (element.tagName === 'INPUT' && eventType === 'keypress') {
                    return true;
                }
                return false;
            },
        }),
    ],
});

// ============================================================================
// 5. Funciones helper para logging manual
// ============================================================================
window.otelLog = {
    info: (message, attributes = {}) => {
        logger.emit({
            severityText: 'INFO',
            severityNumber: 9,
            body: message,
            attributes: {
                'log.source': 'browser',
                ...attributes
            },
        });
        console.log(`[INFO] ${message}`, attributes);
    },
    warn: (message, attributes = {}) => {
        logger.emit({
            severityText: 'WARN',
            severityNumber: 13,
            body: message,
            attributes: {
                'log.source': 'browser',
                ...attributes
            },
        });
        console.warn(`[WARN] ${message}`, attributes);
    },
    error: (message, attributes = {}) => {
        logger.emit({
            severityText: 'ERROR',
            severityNumber: 17,
            body: message,
            attributes: {
                'log.source': 'browser',
                ...attributes
            },
        });
        errorCounter.add(1, { error_type: 'manual', ...attributes });
        console.error(`[ERROR] ${message}`, attributes);
    },
    debug: (message, attributes = {}) => {
        logger.emit({
            severityText: 'DEBUG',
            severityNumber: 5,
            body: message,
            attributes: {
                'log.source': 'browser',
                ...attributes
            },
        });
        console.debug(`[DEBUG] ${message}`, attributes);
    },
};

// ============================================================================
// 6. Funciones helper para métricas
// ============================================================================
window.otelMetrics = {
    recordPageView: (pageName, attributes = {}) => {
        pageViewCounter.add(1, {
            page: pageName,
            page_url: window.location.pathname,
            ...attributes
        });
        otelLog.debug('Page view recorded', { page: pageName });
    },
    recordApiCall: (endpoint, duration, statusCode, attributes = {}) => {
        apiCallCounter.add(1, {
            endpoint,
            status: statusCode,
            ...attributes
        });
        apiCallDuration.record(duration, {
            endpoint,
            status: statusCode,
            ...attributes
        });
    },
    recordError: (errorType, attributes = {}) => {
        errorCounter.add(1, {
            error_type: errorType,
            ...attributes
        });
    },
    recordUserInteraction: (interactionType, elementInfo, attributes = {}) => {
        userInteractionCounter.add(1, {
            interaction_type: interactionType,
            element: elementInfo,
            ...attributes
        });
    },
};

// ============================================================================
// 7. Funciones helper para trazas manuales
// ============================================================================
window.otelTrace = {
    startSpan: (name, attributes = {}) => {
        const tracer = trace.getTracer('web-frontend-tracer');
        const span = tracer.startSpan(name, {
            attributes: {
                'span.source': 'manual',
                ...attributes
            }
        });
        return {
            end: () => span.end(),
            setAttribute: (key, value) => span.setAttribute(key, value),
            setAttributes: (attrs) => span.setAttributes(attrs),
            addEvent: (name, attrs) => span.addEvent(name, attrs),
            recordException: (error) => {
                span.recordException(error);
                span.setStatus({ code: 2, message: error.message }); // ERROR status
            },
            setStatus: (status) => span.setStatus(status),
        };
    },
    withSpan: async (name, fn, attributes = {}) => {
        const span = window.otelTrace.startSpan(name, attributes);
        try {
            const result = await fn(span);
            span.setStatus({ code: 1 }); // OK status
            return result;
        } catch (error) {
            span.recordException(error);
            throw error;
        } finally {
            span.end();
        }
    },
};

// ============================================================================
// 8. Captura automática de errores no manejados
// ============================================================================
window.addEventListener('error', (event) => {
    otelLog.error('Unhandled Error', {
        'error.message': event.message,
        'error.filename': event.filename,
        'error.lineno': event.lineno,
        'error.colno': event.colno,
        'error.stack': event.error?.stack,
        'error.type': event.error?.name || 'Error',
    });

    otelMetrics.recordError('unhandled_error', {
        filename: event.filename,
        type: event.error?.name || 'Error',
    });
});

window.addEventListener('unhandledrejection', (event) => {
    otelLog.error('Unhandled Promise Rejection', {
        'error.reason': event.reason?.toString(),
        'error.stack': event.reason?.stack,
        'error.type': 'UnhandledRejection',
    });

    otelMetrics.recordError('unhandled_rejection', {
        reason: event.reason?.toString(),
    });
});

// ============================================================================
// 9. Inicialización y log de inicio
// ============================================================================
console.log('✅ OpenTelemetry inicializado correctamente (usando OTEL Collector)');
otelLog.info('OpenTelemetry Web SDK initialized', {
    service: OTEL_CONFIG.serviceName,
    environment: OTEL_CONFIG.environment,
    collector: 'localhost:4318 (via proxy to gRPC 4317)',
    userAgent: navigator.userAgent,
});

// Registrar vista de página inicial
if (document.readyState === 'complete') {
    otelMetrics.recordPageView(document.title || window.location.pathname);
} else {
    window.addEventListener('load', () => {
        otelMetrics.recordPageView(document.title || window.location.pathname);
    });
}

// ============================================================================
// 10. Interceptar fetch para métricas adicionales
// ============================================================================
const originalFetch = window.fetch;
window.fetch = async function (...args) {
    const startTime = performance.now();
    const url = typeof args[0] === 'string' ? args[0] : args[0].url;

    // No interceptar llamadas al proxy de telemetría
    if (url.includes('/api/telemetry/')) {
        return originalFetch.apply(this, args);
    }

    try {
        const response = await originalFetch.apply(this, args);
        const duration = performance.now() - startTime;

        otelMetrics.recordApiCall(url, duration, response.status, {
            method: args[1]?.method || 'GET',
        });

        return response;
    } catch (error) {
        const duration = performance.now() - startTime;
        otelMetrics.recordApiCall(url, duration, 0, {
            method: args[1]?.method || 'GET',
            error: error.message,
        });
        otelMetrics.recordError('fetch_error', {
            url: url,
            message: error.message,
        });
        throw error;
    }
};

// ============================================================================
// 11. Monitoreo de rendimiento (Web Vitals)
// ============================================================================
if ('PerformanceObserver' in window) {
    try {
        // Largest Contentful Paint (LCP)
        const lcpObserver = new PerformanceObserver((list) => {
            const entries = list.getEntries();
            const lastEntry = entries[entries.length - 1];
            const lcpHistogram = meter.createHistogram('web.vitals.lcp', {
                description: 'Largest Contentful Paint',
                unit: 'ms',
            });
            lcpHistogram.record(lastEntry.renderTime || lastEntry.loadTime, {
                page: window.location.pathname,
            });
        });
        lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });

        // First Input Delay (FID)
        const fidObserver = new PerformanceObserver((list) => {
            const entries = list.getEntries();
            entries.forEach((entry) => {
                const fidHistogram = meter.createHistogram('web.vitals.fid', {
                    description: 'First Input Delay',
                    unit: 'ms',
                });
                fidHistogram.record(entry.processingStart - entry.startTime, {
                    page: window.location.pathname,
                });
            });
        });
        fidObserver.observe({ entryTypes: ['first-input'] });

        // Cumulative Layout Shift (CLS)
        let clsValue = 0;
        const clsObserver = new PerformanceObserver((list) => {
            for (const entry of list.getEntries()) {
                if (!entry.hadRecentInput) {
                    clsValue += entry.value;
                }
            }
        });
        clsObserver.observe({ entryTypes: ['layout-shift'] });

        window.addEventListener('beforeunload', () => {
            const clsHistogram = meter.createHistogram('web.vitals.cls', {
                description: 'Cumulative Layout Shift',
                unit: '1',
            });
            clsHistogram.record(clsValue, {
                page: window.location.pathname,
            });
        });
    } catch (e) {
        console.warn('Performance Observer not fully supported', e);
    }
}
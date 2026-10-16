# Observabilidade

No domínio não há logging, tracing ou dependência de OpenTelemetry. Quando a instrumentação for introduzida, usar `ILogger<T>`, `ActivitySource`, `Activity` e `Meter` nativos no host, nos adapters e casos de uso apropriados; OpenTelemetry será configurado na composição. Não criar `ITelemetryService`.

Operações HTTP, publicação e consumo de mensagens, acesso a banco e processamento terão spans com IDs técnicos de correlação quando apropriado. Evitar CPF, email e outros dados pessoais em logs, tags e eventos de trace. Métricas futuras devem mostrar latência, throughput, erros e atraso da outbox/projeção. A propagação de contexto por Kafka será definida junto aos contratos.

Aspire poderá facilitar orquestração e inspeção local quando vários processos existirem. No grupo específico, comparar sua utilidade com Compose e não criar duas fontes conflitantes de configuração.

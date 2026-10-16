# 012 — Observabilidade distribuída (grupo 11)

- **Objetivo:** enxergar fluxo, falhas e atraso ponta a ponta.
- **Escopo:** tracing, métricas e logs com OpenTelemetry; avaliar Aspire para ambiente de desenvolvimento.
- **Fora de escopo:** telemetria no Domain e dados pessoais em tags/logs.
- **Arquivos:** hosts/adapters dos serviços, configuração local, docs.
- **Aceite:** trace correlaciona HTTP, outbox, Kafka e consumers; métricas de atraso/erro são consultáveis.
- **Testes esperados:** smoke test de exportação e verificação de propagação de contexto.
- **Estudar:** trace context, cardinalidade e SLOs.
- **Dependências:** 011.
- **Riscos/aberto:** backend de telemetria e Aspire dependem da utilidade comprovada no ambiente local.

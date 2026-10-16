# Backlog incremental

Esta é a fonte canônica da ordem e das dependências. Cada arquivo de task define escopo e aceite. Os grupos 0 a 2 estabeleceram workspace, Domain com Draft e Application. As tasks 004 e 005 possuem código e testes; a validação com containers e host HTTP permanece pendente pelas restrições deste ambiente. Backlog não é autorização para executar a próxima task.

| Grupo | Task | Entrega principal | Dependência |
| --- | --- | --- | --- |
| 0 | [001](001-bootstrap.md) | documentação e workspace | nenhuma |
| 1–2 | [002](002-registration-domain.md), [003](003-registration-application.md) | Registration, Draft, values, Application e testes | 001, depois 002 |
| 3 | [004](004-registration-infrastructure.md) | Mongo Draft, Oracle Registration, CEP adapter e composição; validação Docker pendente | 003 |
| 4 | [005](005-registration-api.md) | HTTP de onboarding, conta e login por código; teste HTTP em execução pendente | 004 |
| 5 | [006](006-initial-docker.md) | containers iniciais de Registration | 005 |
| 6 | [007](007-kafka-basic.md) | primeiro contrato e Kafka | 006 |
| 7 | [008](008-transactional-outbox.md) | publicação confiável e Worker do relay | 007 |
| 8 | [009](009-fraud-service.md) | bounded context Fraud | 008 |
| 9 | [011](011-idempotent-consumers.md) | inbox/deduplicação onde houver efeito | 009 |
| 10 | [012](012-observability.md) | telemetria distribuída | 011 |
| 11 | [013](013-complete-compose.md) | ambiente integrado existente | 012 |
| 12 | [014](014-feedback-ui.md) | feedback visual/SignalR | 013 |

[010 — projeção consolidada](010-consolidated-query-evaluation.md) é **condicional e sem grupo fixo**. Só será aprovada se consultas reais precisarem combinar dados de múltiplos contextos. GETs e retomada do Draft pertencem a Registration. Testcontainers entra com os adapters reais. Redis, eventual Identity independente, Compliance e Notification dependem de requisitos concretos. A [estrutura oficial](../architecture/08-local-development.md) indica serviços, hosts e Compose.

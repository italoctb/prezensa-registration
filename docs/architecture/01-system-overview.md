# Visão do sistema

O `registration-service` possui tanto a construção progressiva do pré-cadastro quanto o cadastro formal. `RegistrationDraft` e `Registration` são aggregates do mesmo bounded context, mesmo com adapters para bancos diferentes. A análise de risco pertencerá ao `fraud-service`. O host HTTP expõe onboarding, retomada do Draft, consulta de CEP e submit; consultas simples do Registration formal dependem de um caso de uso futuro.

```text
cliente -> Registration.Api (host atual) -> Application -> RegistrationDraft -> MongoDB (adapter atual)
                                                   -> SubmitDraft -> Registration -> Oracle (adapter atual)
                                                                  -> integração futura -> fraud-service
```

O host, os handlers e os adapters existem, mas o fluxo completo ainda depende de MongoDB, Oracle e SMTP externos; não há ambiente local integrado. Acesso por conta verificada com código de e-mail e sessão revogável pertence ao host Registration neste checkpoint; [rotas e garantias](09-registration-api.md) estão na fonte canônica da API. Respostas antifraude deverão voltar por contrato externo e ser aplicadas por um caso de uso de Registration. Uma decisão antifraude não conclui sozinha o cadastro: o aggregate decide se a conclusão é válida. Um eventual `registration-query-service` só será justificado por uma projeção consolidada de múltiplos contextos (por exemplo Registration, Fraud, Identity e Compliance), e não por operações GET comuns. A [sequência de planejamento](../tasks/README.md) está apenas no backlog.

## Critérios arquiteturais

- Operações síncronas ou assíncronas são escolhidas pelo requisito do fluxo, não pela presença da tecnologia no plano.
- Os [limites de serviço](02-bounded-contexts.md), as [dependências internas](03-dependency-rules.md) e os [contratos de eventos](05-event-driven-architecture.md) têm documentos próprios.

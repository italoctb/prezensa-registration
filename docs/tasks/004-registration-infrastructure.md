# 004 — Infrastructure de Registration

**Estado:** código e testes escritos; execução dos testes com containers pendente até haver acesso ao daemon Docker. O [desenho e as garantias efetivas](../architecture/06-persistence-strategy.md) são documentados em um só lugar.

- **Objetivo:** implementar os ports usados pelo onboarding e pelo submit.
- **Escopo:** `MongoRegistrationDraftRepository`, `OracleRegistrationRepository`, adapter de consulta de CEP, configuração e DI; índices/constraints para owner ativo, CPF único e concorrência otimista; política de retry seguro para a falha comum entre as duas gravações.
- **Fora de escopo:** API, hosts, Docker/Compose, Kafka, Outbox, Worker, Fraud, Query Service e frontend.
- **Arquivos:** `services/registration/src/Registration.Infrastructure/`, scripts/migrações necessários e testes de integração.
- **Aceite:** round trip dos dois aggregates, dados de CEP traduzidos para tipos locais, unicidade sob concorrência, versionamento e falha parcial do submit tratados com política documentada; Domain/Application sem referência aos drivers.
- **Testes esperados:** Testcontainers ou ambiente descartável equivalente para Mongo/Oracle, CEP adapter com HTTP controlado, concorrência, duplicate CPF e recuperação após falha entre gravações.
- **Estudar:** mapeamento de aggregate, índices únicos, optimistic concurrency, transação local e consistência entre stores.
- **Dependências:** 003.
- **Riscos/aberto:** validar os testes Testcontainers com Docker; substituir o criador inicial de tabela por migrações versionadas antes de produção; definir reconciliação operacional de estados divergentes, inclusive Draft alterado após gravação Oracle. Não pressupor Saga ou transação distribuída.

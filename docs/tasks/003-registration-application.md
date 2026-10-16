# 003 — Registration.Application: onboarding e submit

- **Objetivo:** orquestrar Draft e criação de Registration sem transporte ou persistência concreta.
- **Escopo:** StartDraft; dez atualizações de campo; LookupPostalCode; GetCurrentDraft; SubmitRegistrationDraft; ownership; CPF único no submit; ports específicos; comandos, queries, resultados e testes.
- **Fora de escopo:** Infrastructure, API, Mongo, Oracle, ViaCEP HTTP, Kafka e transação distribuída.
- **Arquivos:** `services/registration/src/Registration.Application/` e `services/registration/tests/Registration.Application.Tests/`.
- **Aceite:** handlers acessam Draft do owner, invocam value objects/aggregates, usam ports com `CancellationToken` e `TimeProvider`; não referenciam adapters nem ASP.NET; submit persiste Registration e associa ID ao Draft.
- **Testes esperados:** idempotência de Start, atualização válida/inválida, acesso negado/ausente, Draft terminal, CEP encontrado/ausente, snapshot de retomada, CPF duplicado, submit completo/incompleto e ordem das gravações.
- **Estudar:** vertical slices, ports, ownership, validação intrínseca versus sistêmica, consistência entre stores.
- **Dependências:** 002.
- **Riscos/aberto:** `GetActiveByOwnerAsync` + `AddAsync` exigirão índice/garantia contra corrida; `ExistsByCpfAsync` exigirá constraint transacional. Falha entre as duas gravações do submit requer política na task 004.

# Dependências e organização interna

```text
Domain <- Application <- Api/Worker
            ^
            |
       Infrastructure

Api/Worker -> Infrastructure apenas na composição
```

`Domain` não referencia nenhuma outra camada. `Application` referencia Domain e define ports conforme seus casos de uso. `Infrastructure` referencia Application e Domain para implementar ports. `Api` ou `Worker` compõe seu processo e chama casos de uso; endpoints e consumers não acessam Oracle, Kafka ou Mongo diretamente. A distinção entre serviço lógico e host está em [limites de serviço](02-bounded-contexts.md).

`Registration.Application` agrupa comandos, queries e handlers por use case em `Drafts/`, `Registrations/SubmitDraft/` e `Accounts/`. Os ports de Draft, Registration, CEP, conta, desafio, sessão e envio de e-mail são exigidos por fluxos concretos. `Registration.Infrastructure` implementa esses ports com MongoDB, Oracle, HTTP e SMTP; seus drivers não aparecem nas referências de Domain ou Application. `Registration.Api` compõe as dependências, autentica o owner por uma sessão emitida após código por e-mail e traduz HTTP para comandos e resultados sem acessar drivers nos endpoints. `TimeProvider` fornece tempo controlável, sem `IClock` próprio; `CancellationToken` atravessa I/O, não o Domain. Não há mediator, DTO HTTP dentro da Application ou validação duplicada dos value objects. O [contrato HTTP](09-registration-api.md) documenta o acesso.

Referências entre projetos internos de serviços diferentes são proibidas. A eventual comunicação HTTP ou Kafka usa contratos externos definidos na borda e traduzidos para o modelo local. Um workspace ou solution de desenvolvimento não é biblioteca compartilhada.

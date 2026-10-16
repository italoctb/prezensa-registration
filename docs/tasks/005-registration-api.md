# 005 — Minimal API de Registration

**Estado:** host, endpoints e testes HTTP escritos; o acesso foi corrigido para criação de conta e login exclusivos por código de e-mail corporativo, com sessões opacas. A execução dos testes HTTP depende de ambiente que permita iniciar ASP.NET Core. O [contrato HTTP](../architecture/09-registration-api.md) é a fonte detalhada das rotas e autenticação.

- **Objetivo:** expor onboarding, retomada, consulta de CEP e submit por HTTP.
- **Escopo:** host, endpoints por feature, composição, criação de conta, login por código enviado a `@prezensa.com`, sessão revogável como origem de `OwnerId`, mapeamento de erros e contratos externos claros.
- **Fora de escopo:** acesso direto a Mongo/Oracle nos endpoints, Kafka e Worker.
- **Arquivos:** `services/registration/src/Registration.Api/`; testes HTTP conforme contrato.
- **Aceite:** `Program.cs` enxuto; requests/responses separados dos aggregates; endpoints chamam Application; owner vem da identidade do usuário, não de valor confiado ao cliente.
- **Testes esperados:** contrato HTTP, criação de conta, login, expiração, uso único, revogação, erros, ownership e retomada.
- **Estudar:** Minimal APIs, DI, autenticação e composition root.
- **Dependências:** 004.
- **Riscos/aberto:** testar o host HTTP em ambiente que permita iniciar ASP.NET Core; validar SMTP real e proteção compartilhada de borda; escolher versionamento de contratos quando houver mudança incompatível. O `sub` autenticado deriva da sessão persistida e nunca é aceito como campo fornecido pelo cliente.

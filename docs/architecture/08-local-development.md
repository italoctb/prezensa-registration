# Workspace e desenvolvimento local

## Estrutura oficial

`PDI/` é a raiz do sistema no ambiente local. Esta é a única árvore de diretórios canônica: `[atual]` existe; `[futuro]` depende de necessidade e aprovação. Diretórios de build não aparecem nela.

```text
PDI/
├── docs/                                             [atual]
├── services/
│   ├── registration/
│   │   ├── Registration.slnx                         [atual]
│   │   ├── src/
│   │   │   ├── Registration.Domain/                  [atual]
│   │   │   │   ├── Applicants/                       [atual: perfil e valores compartilhados]
│   │   │   │   │   └── Addresses/                    [atual: componentes do endereço]
│   │   │   │   ├── Drafts/                          [atual: aggregate, ID, estado, etapas, eventos]
│   │   │   │   └── Registrations/                   [atual: aggregate definitivo e eventos]
│   │   │   ├── Registration.Application/             [atual]
│   │   │   │   ├── Abstractions/                     [atual: persistência e integrações]
│   │   │   │   ├── Accounts/                         [atual: criação de conta, login e sessão]
│   │   │   │   ├── Drafts/                          [atual: use cases de onboarding]
│   │   │   │   └── Registrations/SubmitDraft/       [atual]
│   │   │   ├── Registration.Infrastructure/          [atual: Mongo, Oracle, CEP, SMTP, DI]
│   │   │   ├── Registration.Api/                     [atual: host HTTP]
│   │   │   └── Registration.Worker/                  [futuro: processamento real]
│   │   └── tests/
│   │       ├── Registration.Domain.Tests/            [atual]
│   │       ├── Registration.Application.Tests/       [atual]
│   │       ├── Registration.Infrastructure.Tests/    [atual]
│   │       └── Registration.Api.Tests/               [atual]
│   ├── fraud/                                       [futuro]
│   └── registration-query/                          [condicional: projeção entre contextos]
├── apps/web/                                        [futuro: cliente]
├── deploy/docker/                                   [futuro: configuração auxiliar]
├── docker-compose.yml                               [futuro: ambiente integrado]
├── global.json                                      [atual]
├── .editorconfig                                    [atual]
├── .gitignore                                       [atual]
└── README.md                                        [atual]
```

`Applicants/` não define um Customer aggregate: reúne perfil e valores usados por Draft e Registration. `Drafts/` e `Registrations/` mantêm seus ciclos e eventos próximos. Os projetos Domain/Application não possuem packages externos de produção; apenas Infrastructure referencia drivers. O `.csproj` configura build com `Microsoft.NET.Sdk`; isso não acopla as regras à infraestrutura.

`bin/` e `obj/` são artefatos ignorados por `.gitignore`. `.vscode/settings.json` pode ocultá-los no Explorer sem afetar o build. Não criamos pastas ou projetos vazios por simetria. Cada pasta em `services/` poderá virar repositório independente, sem project references entre contextos. O workspace reúne documentação e, futuramente, orquestração local; não cria `BuildingBlocks` global. Os [limites entre contextos e hosts](02-bounded-contexts.md) têm fonte própria.

## Compose e Docker

O `docker-compose.yml` principal ficará na raiz porque representa o sistema inteiro e permitirá `docker compose up` dali quando houver ambiente integrado. `deploy/docker/` guardará suporte local, como scripts de inicialização. Dockerfiles ficarão próximos de cada host empacotado. Há host Api e adapters, mas ainda não há Compose nem Dockerfiles.

## Requisitos e comandos atuais

SDK .NET 10.0.401 (fixado em `global.json`).

```sh
dotnet build services/registration/Registration.slnx
dotnet test services/registration/Registration.slnx
```

Os testes xUnit exigem packages restaurados e um ambiente que permita ao VSTest abrir seu socket local. Os testes HTTP usam `WebApplicationFactory` com stores e transporte de e-mail controlados, mas exercitam o esquema Bearer próprio; os testes de integração de Infrastructure precisam de Docker disponível para iniciar MongoDB e Oracle Free via Testcontainers. A [configuração e rotas do host](09-registration-api.md) e a [persistência](06-persistence-strategy.md) têm fontes próprias. A [ordem de implementação](../tasks/README.md) indica os checkpoints seguintes.

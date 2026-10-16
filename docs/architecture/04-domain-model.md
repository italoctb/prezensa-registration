# Modelo de Registration

Esta é a fonte canônica dos aggregates, value objects, validações e máquinas de estado de `Registration.Domain`. A árvore física está em [workspace](08-local-development.md); integração entre contextos está em [eventos](05-event-driven-architecture.md).

## Dois aggregates no mesmo bounded context

`RegistrationDraft` representa o onboarding parcial, com identidade e ciclo próprios. `Registration` representa o cadastro formal submetido, com `ApplicantProfile` completo e imutável. `SubmitRegistrationDraftHandler` constrói esse perfil a partir do Draft, cria Registration e marca o Draft como Submitted. Os dois reutilizam os mesmos value objects de candidato. `Cpf?` no Draft significa ausente; um `Cpf` presente nunca é inválido. Os campos do perfil definitivo não são nullable.

`RegistrationDraftId`, `OwnerId` e `RegistrationId` são IDs fortes de `Guid` não vazio; aggregates comparam por identidade. `CustomerId` continua adiado porque não há fluxo que determine quem o cria ou quando. `ApprovalActor` continua o identificador opaco do ator de aprovação final de Registration; não o confundimos com o owner do Draft.

## Valores do candidato

| Valor | Regra intrínseca |
| --- | --- |
| `FullName` | Espaços normalizados e 3 a 15 caracteres visíveis. Limite literal deste desafio, não recomendação para pessoas reais. |
| `Cpf` | 11 dígitos canônicos, verificadores corretos e rejeição de sequência repetida; máscara removida. Unicidade não é regra do valor. |
| `BirthDate` | `DateOnly` não padrão e `IsAtLeast18On(referenceDate)` com data explícita; `DateOnly.AddYears(18)` considera 29/02 elegível em 28/02 no ano não bissexto. |
| `EmailAddress` | Sintaxe pragmática, trim, domínio minúsculo e parte local preservada. Não promete RFC completa, titularidade ou entrega. |
| `PhoneNumber` | DDD brasileiro entre 11 e 99 e 8 ou 9 dígitos de assinante; armazena apenas 10 ou 11 dígitos. |
| `MotherName`, `Nationality`, `Profession` | Tipos distintos, obrigatórios e com espaços normalizados; sem catálogo externo ou enum de profissões. |
| `MonthlyIncome` | `decimal >= 0`, interpretado em BRL neste fluxo. |
| `Address` | Composição obrigatória de `PostalCode`, `Street`, `District`, `City`, `FederativeUnit` e `AddressNumber`; `AddressComplement` é opcional. |
| Componentes de endereço | CEP com oito dígitos canônicos; UF entre as 27 siglas brasileiras; textos não vazios normalizados. Complemento, se informado, não pode ser vazio. Sem consulta externa no Domain. |

Os valores são imutáveis, construídos por fábricas e iguais por valor. `ApplicantProfile` é um valor composto que exige os dez dados completos; não é aggregate Customer. `Address` também é valor composto. O Domain não usa DTO, ORM, HTTP ou pacote externo.

## Draft: preenchimento e submissão

| Campo | Regra |
| --- | --- |
| `Status` | `InProgress` → `Submitted`; Submitted é terminal. Não há Expired sem política de expiração. |
| `CurrentStep` | Enum: FullName, Document, BirthDate, Email, Phone, MotherName, Nationality, Address, Profession, MonthlyIncome, Review. É derivado da primeira ausência nos dados, para UX/retomada; não autoriza submissão. Uma edição fora de ordem é permitida. |
| Dados parciais | Dez value objects nullable no Draft. `Set*` exige valor não nulo já válido, Draft em progresso e instante UTC monotônico. Não há evento por campo. |
| `CanSubmit` / `EnsureCanSubmit` | Todos os campos presentes, nascimento não futuro e idade de pelo menos 18 anos na data explícita. `ToApplicantProfile` aplica essa regra antes de produzir perfil completo. |
| `CreatedAt`, `UpdatedAt`, `SubmittedAt` | `DateTimeOffset` em UTC, recebidos nas operações. `UpdatedAt` muda em toda alteração e submissão; `SubmittedAt` só após submissão. |
| `RegistrationId` | Nulo antes do submit; associado pelo próprio Draft junto com status e timestamp. |
| `Version` | `long` inicia em 1 e cresce uma vez por mutação válida. O adapter Mongo compara a versão anterior na atualização; não há atributo de storage no Domain. |

`EnsureCanSubmitAt` verifica completude e tempo UTC monotônico antes de qualquer gravação externa no handler. `MarkSubmitted` repete a proteção, exige ID definitivo não vazio e estado InProgress. Repetição, edição posterior, tempo anterior ou offset não UTC falham sem mutar estado. `RegistrationDraftStarted` e `RegistrationDraftSubmitted` são os únicos eventos do Draft nesta etapa.

## Registration formal

`Registration.Create` recebe `ApplicantProfile` completo e instante UTC; confirma nascimento não futuro e 18 anos na data do submit. O perfil permanece imutável. Seu estado continua `Received`, `Processing`, `Completed`, `Rejected`, `Failed` ou `Cancelled`. `FraudCheckStatus` é apenas a visão local de Registration (`NotRequested`, `Pending`, `Approved`, `Rejected`), sem score ou estado interno do Fraud Service.

| Operação | Pré-condição | Efeito |
| --- | --- | --- |
| `Create` | ID, perfil completo e maioridade | `Received`, versão 1 |
| `StartProcessing` | `Received` | `Processing` |
| `RequestFraudCheck` | `Processing` e fraude `NotRequested` | fraude `Pending` |
| `ApproveFraud` / `RejectFraud` | `Processing` e fraude `Pending` | fraude aprovada ou Registration `Rejected` |
| `Complete` | `Processing`, fraude aprovada e ator válido | `Completed`, `ApprovedAt` e `ApprovedBy` juntos |
| `Fail` / `Cancel` | `Received` ou `Processing` | estado terminal correspondente |

`Completed`, `Rejected`, `Failed` e `Cancelled` são terminais. `ApprovedAt`/`ApprovedBy` só existem após `Complete`. `UpdatedAt` e `Version` crescem nas transições válidas. O aggregate registra fatos internos de recebimento, processamento, antifraude, conclusão, falha e cancelamento. Não há chamada direta a relógio, banco ou serviço externo.

## Dono das validações

| Regra | Responsável |
| --- | --- |
| Formato de CPF, email, telefone, CEP, UF, textos e renda | Value object correspondente |
| Idade >= 18 e nascimento não futuro no submit | `RegistrationDraft` com `BirthDate` e referência explícita; `Registration.Create` confirma |
| Dados completos e terminalidade do Draft | `RegistrationDraft` |
| Transições, aprovação e consistência temporal de Registration | `Registration` |
| Ownership do Draft | `Registration.Application` |
| CPF único | `Registration.Application` no submit; constraint única no Oracle impede corrida |
| Consulta de CEP | `IPostalCodeLookup` da Application; adapter HTTP na Infrastructure |
| Endereço real, identidade do cliente, antifraude | Integrações futuras, conforme requisito |

`DomainException` mantém código estável e mensagem contextual para violações do modelo. `RegistrationApplicationException` separa falta de recurso, acesso negado, CPF duplicado e CEP não encontrado. Não há uma exception por regra nem `Result<T>` global.

## Eventos internos

Eventos do Domain descrevem fatos locais e não são contratos Kafka. Eventos de integração exigirão tradução explícita, versão e identidade de mensagem. O evento de submissão do Draft carrega somente IDs e instante; não replica dados pessoais. A [estratégia de integração](05-event-driven-architecture.md) define a fronteira externa. Na Application atual, os eventos ficam registrados nos aggregates; não há dispatcher nem publicação.

`RegistrationDraft.Restore` e `Registration.Restore` reconstituem estados persistidos por snapshots tipados, validam consistência e deixam a lista de domain events vazia. São caminhos de leitura para os adapters; regras e transições continuam nos aggregates. O [mapeamento físico e a consistência entre stores](06-persistence-strategy.md) têm fonte própria.

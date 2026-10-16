# API HTTP de Registration

Esta é a fonte canônica do contrato HTTP e do acesso à API. [Dependências internas](03-dependency-rules.md), [persistência](06-persistence-strategy.md) e [árvore do workspace](08-local-development.md) têm documentos próprios.

## Conta, login e sessão

Há dois fluxos públicos distintos. **Criar conta** começa com `POST /accounts/codes` e termina em `POST /accounts`. **Entrar** começa com `POST /sessions/codes` e termina em `POST /sessions`. Ambos comprovam acesso a um endereço cujo domínio é exatamente `prezensa.com`. Qualquer pessoa que comprove esse acesso pode criar a conta. Não existem senha, login social ou autoridade JWT externa. O e-mail corporativo identifica a conta; o e-mail informado nos dados de um Draft é um dado do candidato e continua com suas próprias regras.

O endereço de acesso é aparado e normalizado em minúsculas, inclusive na parte local. Isso assume que as caixas corporativas da Prezensa não distinguem maiúsculas e minúsculas; a política deve ser confirmada com o administrador do domínio antes de produção. Subdomínios e domínios que apenas terminam com `prezensa.com` são rejeitados.

Um pedido válido de código responde `202 Accepted`, sem incluir código ou estado da conta. Se a conta já existe durante criação, ou não existe durante login, o mesmo `202` é retornado e nenhum código é enviado. O código de oito dígitos é gerado criptograficamente, enviado **somente por e-mail**, guardado como HMAC SHA-256 com chave de configuração, válido por dez minutos e consumido uma única vez. A emissão tem intervalo mínimo de um minuto por endereço/finalidade; cinco erros de verificação bloqueiam o desafio até expirar, inclusive após reenvio. A API também limita os quatro endpoints públicos a dez requisições por minuto por IP da conexão. O limite por IP é local à instância e não substitui proteção compartilhada de borda em uma implantação com várias réplicas. Respostas `202` uniformes reduzem enumeração direta, mas diferenças de latência entre envio e não envio ainda precisam ser avaliadas antes de produção.

Depois de verificar o código, a API devolve um **token opaco** Bearer de 256 bits e sua expiração em 12 horas. Somente o hash SHA-256 do token é persistido. O esquema de autenticação consulta a sessão ativa em MongoDB e deriva `OwnerId` dela; o cliente nunca fornece `OwnerId` em rotas ou corpos de onboarding. `DELETE /sessions/current` revoga o token. A criação da conta e a verificação do código são separadas do login: uma conta existente não é recriada pelo fluxo de cadastro.

Esse mecanismo comprova acesso à caixa de e-mail e não constitui autenticação multifator. A [NIST SP 800-63B](https://pages.nist.gov/800-63-4/sp800-63b.html) proíbe e-mail como autenticador fora de banda; este fluxo não reivindica conformidade com esse requisito. Esta é a forma de login solicitada para o projeto; requisitos futuros de garantia, recuperação de conta e proteção contra comprometimento do e-mail exigirão nova decisão. A escolha de token opaco revogável elimina a dependência de autoridade externa presumida anteriormente.

## Configuração

O host requer MongoDB, Oracle e um servidor SMTP com STARTTLS. Valores sensíveis vêm de variáveis de ambiente ou secret store, não do repositório:

```text
Registration__Mongo__ConnectionString
Registration__Mongo__Database
Registration__Oracle__ConnectionString
Registration__Authentication__CodeHashKey       # base64 de >= 32 bytes
Registration__Authentication__Smtp__Host
Registration__Authentication__Smtp__Port         # normalmente 587
Registration__Authentication__Smtp__From
Registration__Authentication__Smtp__Security     # StartTls
Registration__Authentication__Smtp__Username     # opcional, junto com Password
Registration__Authentication__Smtp__Password     # opcional, junto com Username
```

O adapter SMTP usa `System.Net.Mail.SmtpClient` com STARTTLS obrigatório para evitar uma dependência indisponível neste ambiente. [A documentação do .NET](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-10.0) recomenda MailKit para novos desenvolvimentos por suportar mais protocolos modernos. A compatibilidade com o provedor SMTP real e eventual troca por MailKit devem ser verificadas antes de produção. Nunca registrar códigos ou tokens em logs.

`OracleSchemaStartup` cria a tabela inicial antes de aceitar tráfego; o índice único de Draft ativo é criado na primeira inserção. MongoDB, Oracle e SMTP precisam estar configurados para executar o host real. Não há Compose ou Dockerfile nesta etapa.

## Rotas de acesso

| Método e rota | Corpo JSON | Resultado |
| --- | --- | --- |
| `POST /accounts/codes` | `{"email":"ana@prezensa.com"}` | `202`, código enviado quando elegível |
| `POST /accounts` | `{"email":"ana@prezensa.com","code":"12345678"}` | `201`, sessão e `ownerId` |
| `POST /sessions/codes` | `{"email":"ana@prezensa.com"}` | `202`, código enviado quando elegível |
| `POST /sessions` | `{"email":"ana@prezensa.com","code":"12345678"}` | `200`, sessão e `ownerId` |
| `DELETE /sessions/current` | Bearer token | `204`, sessão revogada |

## Rotas de cadastro

Todas as rotas abaixo exigem `Authorization: Bearer <token>` de uma sessão ativa.

| Método e rota | Corpo JSON | Resultado |
| --- | --- | --- |
| `POST /drafts` | vazio | `200` com Draft ativo criado ou reutilizado |
| `GET /drafts/current` | vazio | `200` com Draft ativo do owner; `404` se não existir |
| `PUT /drafts/{draftId}/full-name` | `{"fullName":"Ana Maria"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/document` | `{"document":"529.982.247-25"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/birth-date` | `{"birthDate":"1990-05-20"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/email` | `{"email":"ana@example.com"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/phone` | `{"phone":"11912345678"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/mother-name` | `{"motherName":"Maria Silva"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/nationality` | `{"nationality":"Brasileira"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/address` | `postalCode`, `street`, `district`, `city`, `federativeUnit`, `number`, `complement?` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/profession` | `{"profession":"Engenheira"}` | `200` com Draft atualizado |
| `PUT /drafts/{draftId}/monthly-income` | `{"monthlyIncome":5000}` | `200` com Draft atualizado |
| `GET /postal-codes/{postalCode}` | vazio | `200` com endereço; `404` se ausente |
| `POST /drafts/{draftId}/submit` | vazio | `200` com `registrationId` |

`DraftResponse` contém ID, status, etapa, campos preenchidos normalizados, timestamps UTC, `RegistrationId` opcional e versão. Após submit, o Draft deixa de ser ativo. O Registration formal ainda não possui GET próprio porque não existe caso de uso de consulta para ele.

## Erros e testes

Erros conhecidos usam `ProblemDetails` com extensão `code`. E-mail corporativo inválido e violações do Domain retornam `400`; código inválido, expirado ou já usado e sessão ausente ou inválida, `401`; owner de outro Draft, `403`; Draft ativo/CEP ausente, `404`; CPF duplicado, conflito de versão ou conta já existente na confirmação, `409`; limite de requisições, `429`. Falhas técnicas permanecem `500` sem detalhes internos.

Testes HTTP usam `WebApplicationFactory` com stores e transporte de e-mail falsos. Exercitam o esquema Bearer opaco e o fluxo de conta/login, mas não um provedor SMTP real nem os bancos. Testes de Application cobrem emissão, expiração, uso único, bloqueio e revogação sem HTTP. Testcontainers cobrem adapters de banco quando o daemon Docker estiver acessível; o ambiente atual também impede iniciar o host de testes HTTP.

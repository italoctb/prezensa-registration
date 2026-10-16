# PDI — plataforma de cadastro distribuído

`PDI/` é o workspace local do sistema inteiro. O desenvolvimento em .NET 10 avança por checkpoints aprovados; no momento, o bounded context Registration contém Domain, Application, Infrastructure, Api e testes. A API exige MongoDB, Oracle e SMTP configurados. Criação de conta e login usam códigos enviados exclusivamente a endereços `@prezensa.com`; o [contrato HTTP](docs/architecture/09-registration-api.md) é a fonte dos detalhes.

## Comece aqui

1. [Visão do sistema](docs/architecture/01-system-overview.md)
2. [Estrutura oficial do workspace e execução local](docs/architecture/08-local-development.md)
3. [Modelo e máquina de estados](docs/architecture/04-domain-model.md)
4. [Backlog e checkpoints](docs/tasks/README.md)
5. [Índice completo da documentação](docs/README.md)

## Estado do workspace

`services/registration/Registration.slnx` agrupa os projetos atuais. O Draft e Registration são dois aggregates do mesmo bounded context. O host Api chama os casos de uso; os adapters MongoDB, Oracle e CEP ficam na Infrastructure. Os diretórios futuros aparecem somente na [árvore planejada](docs/architecture/08-local-development.md), até existir uma tarefa aprovada que justifique sua criação.

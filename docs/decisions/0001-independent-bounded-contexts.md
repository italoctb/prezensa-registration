# ADR 0001: bounded contexts independentes

Status: aceito.

Contexto: regras de cadastro, análise antifraude e consultas evoluem por responsabilidades diferentes.

Decisão: manter serviços independentes em `services/`, sem referências a projetos internos de outro serviço; integrar por contratos externos. A distinção entre bounded context, serviço e host fica em [limites de serviço](../architecture/02-bounded-contexts.md), e a localização física em [estrutura do workspace](../architecture/08-local-development.md).

Alternativa: uma única solution com biblioteca de modelos compartilhados reduziria configuração inicial, mas tornaria mudanças locais dependências entre contextos. Poderá existir contrato publicado e versionado, sem compartilhar o Domain.

Consequência: alguma duplicação de conceitos nas bordas é aceitável; tradução explícita evita acoplamento semântico e facilita extração de repositórios.

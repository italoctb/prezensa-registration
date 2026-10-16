# ADR 0003: estado de Registration

Status: aceito para o primeiro fluxo.

Contexto: precisamos impedir conclusão sem aprovação antifraude e distinguir rejeição de falha operacional.

Decisão: separar estado geral de Registration do resumo local da exigência antifraude, permitir uma análise inicial e exigir sua aprovação antes da conclusão. O perfil obrigatório é imutável nesta etapa; aprovação registra ator/instante juntos e transições válidas avançam a versão. O [modelo, a máquina de estados e as invariantes](../architecture/04-domain-model.md) são a fonte detalhada.

Alternativa: um enum único para todas as combinações ou objetos de estado por transição. O primeiro cresce com novas verificações; o segundo acrescenta indirection sem comportamento suficiente agora.

Consequência: revisão manual, novas verificações, retry de análise e políticas de timeout exigem revisão explícita do modelo.

# 013 — Compose completo (grupo 12)

- **Objetivo:** subir a plataforma integrada de forma reproduzível.
- **Escopo:** integrar no Compose raiz os hosts e recursos já existentes, com health/readiness, volumes e configuração.
- **Fora de escopo:** Redis sem caso de uso e frontend.
- **Arquivos:** `docker-compose.yml` na raiz, configurações auxiliares justificadas em `deploy/docker/` e docs local.
- **Aceite:** uma sequência documentada sobe ambiente limpo e conclui fluxo de cadastro até decisão; inclui projeção somente se existir.
- **Testes esperados:** smoke end to end e reinício com persistência.
- **Estudar:** rede Compose, dependência por health e limites de recursos.
- **Dependências:** 012.
- **Riscos/aberto:** ajuste de recursos e ordem de startup em máquinas locais.

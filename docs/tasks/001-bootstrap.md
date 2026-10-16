# 001 — Workspace e arquitetura (grupo 0)

- **Objetivo:** tornar limites, estado inicial e evolução legíveis antes do código.
- **Escopo:** README, docs de arquitetura, ADRs relevantes, backlog, `.gitignore`, `.editorconfig`, SDK pin e solution do Registration.
- **Fora de escopo:** serviços executáveis, containers e integrações.
- **Arquivos:** raiz, `docs/`, `services/registration/Registration.slnx`.
- **Aceite:** árvore e dependências descritas; decisões tomadas/adiadas explícitas; solution abre com SDK 10.
- **Testes esperados:** inspeção dos links e `dotnet build` da solution após 002.
- **Estudar:** bounded contexts, composition root, diferenças entre arquitetura lógica e estrutura física.
- **Dependências:** nenhuma.
- **Riscos/aberto:** extração para múltiplos repositórios mudará CI e configuração local; ainda não há necessidade de decidir isso.

# 014 — Feedback visual (grupo 13)

- **Objetivo:** mostrar andamento do cadastro em tempo real.
- **Escopo:** avaliar frontend, contratos de leitura, SignalR e atualização da tela a partir de projeções/eventos.
- **Fora de escopo:** lógica de negócio no hub ou no frontend.
- **Arquivos:** `apps/web/`, host de feedback onde fizer sentido e testes de integração/UI.
- **Aceite:** tela mostra estados, erros públicos e reconexão/refresh sem perder o estado autoritativo de Registration (ou da projeção consolidada, se existir).
- **Testes esperados:** atualização, reconexão, estado inicial e atraso eventual.
- **Estudar:** push versus polling, SignalR e UX de consistência eventual.
- **Dependências:** 013.
- **Riscos/aberto:** Blazor versus outra opção, localização do hub e inclusão do web no Compose dependem de requisitos de UI.

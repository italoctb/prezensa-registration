# 002 — Registration.Domain e Draft

- **Objetivo:** proteger o cadastro definitivo e o onboarding parcial com dois aggregates no mesmo bounded context.
- **Escopo:** `Registration`, `RegistrationDraft`, IDs, estados, etapas, perfil, value objects compartilhados, endereço composto, timestamps, versões, eventos internos, erros e testes xUnit.
- **Fora de escopo:** handlers, adapters, API, mensageria e persistência concreta.
- **Arquivos:** `services/registration/src/Registration.Domain/` e `services/registration/tests/Registration.Domain.Tests/`.
- **Aceite:** valores não nulos sempre válidos; Draft só submete completo e adulto; Submitted é terminal; Registration mantém perfil obrigatório e sua máquina de estados; build sem warnings relevantes.
- **Testes esperados:** valores e limites, Draft progressivo, completude, owner, tempo, versão, submit, eventos, transições de Registration e igualdade.
- **Estudar:** aggregate root, nullability no aggregate, value object compartilhado, máquina de estados e domain event.
- **Dependências:** 001.
- **Riscos/aberto:** CustomerId, regras de expiração, revisão manual e garantia de concorrência dependem de fluxos/adapters concretos.

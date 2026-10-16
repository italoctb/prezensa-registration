# 007 — Kafka básico (grupo 6)

- **Objetivo:** estabelecer primeiro contrato externo e publicar no Kafka em ambiente de desenvolvimento.
- **Escopo:** broker KRaft local, contrato versionado, chave por cadastro candidata, tradução explícita dos eventos e publicação por adapter.
- **Fora de escopo:** entrega confiável antes da outbox, consumidor de produção, `Registration.Worker` sem trabalho real e lógica Fraud.
- **Arquivos:** contratos na borda de Registration, adapter, Compose raiz e testes de integração.
- **Aceite:** evento pode ser lido do Kafka por um consumidor de teste; contrato não expõe classe de Domain; janela de perda e duplicação é documentada.
- **Testes esperados:** broker descartável, publicação/leitura por consumidor de teste e compatibilidade de contrato.
- **Estudar:** partições, offset, at least once, chave e versionamento.
- **Dependências:** 006.
- **Riscos/aberto:** formato, consumer group de produção, retry e dead letter serão fechados com semântica concreta do consumidor. O relay da outbox dará trabalho real ao Worker em 008.

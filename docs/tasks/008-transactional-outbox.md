# 008 — Transactional Outbox (grupo 7)

- **Objetivo:** fechar o intervalo de falha entre commit Oracle e publish Kafka.
- **Escopo:** gravação atômica de aggregate + outbox, `Registration.Worker` para o publisher assíncrono, controle de entrega e observação.
- **Fora de escopo:** garantia exactly once ponta a ponta.
- **Arquivos:** projetos Application/Infrastructure de Registration, `services/registration/src/Registration.Worker/`, schema Oracle e testes.
- **Aceite:** falha de Kafka não perde mensagem confirmada no Oracle; retry não altera fato de negócio; publisher suporta reinício.
- **Testes esperados:** falha antes/depois de publish, concorrência de publishers e replay.
- **Estudar:** atomicidade local, at least once e relay pattern.
- **Dependências:** 007.
- **Riscos/aberto:** polling, locking e retenção da outbox dependem de carga observada.

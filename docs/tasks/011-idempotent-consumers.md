# 011 — Consumidores idempotentes (grupo 10)

- **Objetivo:** tornar efeitos transacionais seguros diante de duplicatas.
- **Escopo:** `MessageId` estável, inbox/processed messages e constraint única na mesma transação do efeito, onde necessário.
- **Fora de escopo:** usar timestamp ou cache volátil como garantia.
- **Arquivos:** adapters e schemas dos serviços consumidores, testes de integração.
- **Aceite:** entrega duplicada e reinício não repetem efeito de negócio; falha antes do commit permite retry.
- **Testes esperados:** duplicata, concorrência, falha e replay.
- **Estudar:** at least once, idempotência e transação local.
- **Dependências:** 009; 010 somente se a projeção existir.
- **Riscos/aberto:** banco e política de retenção da inbox variam por serviço.

# Eventos e integração

**Domain event** registra um fato dentro de Registration, com tipo e dados do modelo local. Ele pode apoiar handlers internos e tradução na Application. **Integration event** é um contrato externo versionado para outros contextos. Não se serializa uma classe de domain event diretamente no Kafka. A tradução seleciona campos públicos, atribui `MessageId` estável, versão e metadados de correlação/causação.

O Domain registra `RegistrationDraftStarted` e `RegistrationDraftSubmitted`, além de recebimento, processamento, antifraude, conclusão, falha e cancelamento de Registration. Não há evento por campo de onboarding. O fato local `FraudCheckRequested` não determina sozinho o formato de Kafka. A Application atual não despacha esses eventos; publicação e consumo dependem de adapters e persistência transacional futuros.

## Kafka futuro

- Chave por `RegistrationId` é candidata para preservar ordem por cadastro em uma partição; número de partições e grupos dependerão de volume e consumidores.
- Consumidores devem tolerar entrega ao menos uma vez, duplicações e reordenação entre tópicos. Cada mensagem terá identidade estável; timestamp não serve como chave de idempotência.
- Retries distinguirão erros transitórios de mensagens inválidas. Destino de mensagens irrecuperáveis, observação e reprocessamento serão definidos com o broker real.
- Contratos terão versão explícita e evolução compatível; schema registry e formato serão escolhidos quando existir um primeiro contrato.

## Outbox e inbox futuros

Persistir Registration e uma `OutboxMessage` na mesma transação Oracle poderá evitar o buraco entre commit do banco e publicação Kafka. Um publisher assíncrono enviará mensagens pendentes; duplicatas ainda são possíveis. Para efeitos transacionais de consumidores, uma inbox/`ProcessedMessages` com `UNIQUE(MessageId)` fará inserção da identidade e alteração de negócio na mesma transação. Isso não resolve a consistência entre Mongo Draft e Oracle Registration no submit; a política desse fluxo será decidida com os adapters. O desenho concreto pertence aos grupos de persistência e integração.

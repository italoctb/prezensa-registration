# ADR 0004: papéis dos dados e da integração

Status: papéis aceitos; adapters de Draft e Registration implementados, hosts e integração assíncrona adiados.

Contexto: o Draft parcial e o Registration formal têm ciclos e necessidades de persistência diferentes; publicação direta após commit pode perder eventos.

Decisão: MongoDB persiste Draft dentro de Registration; Oracle é o source of truth do cadastro formal. Kafka entrará quando houver integração assíncrona concreta, com outbox/inbox nas fronteiras que a exigirem. Um Query Service só nascerá com projeção consolidada real. As garantias implementadas e a lacuna de consistência do submit estão em [persistência](../architecture/06-persistence-strategy.md) e [eventos](../architecture/05-event-driven-architecture.md).

Alternativa: um único banco simplificaria a consistência do submit. A escolha de dois stores implica reconciliação para falhas parciais. Publicar diretamente no Kafka não garante atomicidade com Oracle.

Consequência: o retry seguro para a falha comum está implementado; reconciliação operacional completa, topologia Kafka, evolução de esquemas, persistência de Fraud e eventual Query Service permanecem abertos. Redis depende de necessidade concreta.

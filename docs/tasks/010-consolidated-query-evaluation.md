# 010 — Avaliar projeção consolidada (condicional)

- **Objetivo:** decidir se consultas reais precisam combinar Registration, Fraud, Identity ou Compliance em um read model independente.
- **Escopo se aprovado:** contratos de eventos, projeção materializada, reconstrução/replay, API de leitura e storage escolhido por perfil de consulta.
- **Fora de escopo:** GETs simples e retomada de Draft, que pertencem ao registration-service; implementação automática de Mongo só por CQRS.
- **Arquivos se necessário:** `services/registration-query/{src,tests}/` com solution própria; Compose raiz ao existir executáveis.
- **Aceite:** justificar consulta entre contextos, latência/defasagem e ownership; nenhum projeto importa código interno de outro contexto.
- **Testes esperados:** projeção, reprocessamento, integração do storage escolhido e consultas reais.
- **Estudar:** CQRS, projeções, consistência eventual e custo de um serviço adicional.
- **Dependências:** eventos estáveis dos contextos participantes e requisito concreto de leitura. Sem grupo fixo.
- **Riscos/aberto:** campos, storage e momento de criação permanecem indecididos até haver evidência.

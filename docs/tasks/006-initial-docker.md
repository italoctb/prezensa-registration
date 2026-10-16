# 006 — Dockerização inicial (grupo 5)

- **Objetivo:** reproduzir API, Mongo e Oracle localmente.
- **Escopo:** Dockerfile próximo ao host `Registration.Api`, Compose inicial na raiz, health checks e configuração local.
- **Fora de escopo:** Kafka/Fraud/Query.
- **Arquivos:** `services/registration/src/Registration.Api/Dockerfile`, `docker-compose.yml` na raiz e `deploy/docker/` somente se houver configuração auxiliar, além da documentação local.
- **Aceite:** ambiente sobe do zero, API responde e banco preserva dados conforme volume documentado.
- **Testes esperados:** build de imagem e smoke test HTTP com Mongo e Oracle.
- **Estudar:** multi stage Dockerfile, readiness e volumes.
- **Dependências:** 005.
- **Riscos/aberto:** requisitos de memória e tempo de startup de Oracle Free e MongoDB.

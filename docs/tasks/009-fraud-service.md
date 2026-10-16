# 009 — Fraud Service (grupo 8)

- **Objetivo:** implementar um bounded context antifraude autônomo.
- **Escopo:** modelo próprio, casos de uso, host Api e/ou Worker conforme entradas reais, consumo da solicitação e publicação de decisão externa.
- **Fora de escopo:** importar Registration.Domain/Application, motor de risco fictício complexo.
- **Arquivos:** `services/fraud/{src,tests}/`, Compose e contratos de borda.
- **Aceite:** decisão é produzida por regra explícita, Registration a recebe por adapter/caso de uso; contexts compilam isolados.
- **Testes esperados:** domínio Fraud, contrato, integração via Kafka e falhas de consumo.
- **Estudar:** linguagem ubíqua por contexto, anti corruption layer e decisões assíncronas.
- **Dependências:** 008.
- **Riscos/aberto:** persistência do Fraud, necessidade de HTTP e tratamento de revisão manual dependem da política de análise definida nesse grupo; não criar hosts por simetria.

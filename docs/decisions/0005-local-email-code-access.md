# ADR 0005: acesso local por código de e-mail

Status: aceito para o checkpoint atual.

Contexto: o host HTTP havia presumido uma autoridade JWT externa sem especificação de criação de conta ou login. O requisito agora é acesso exclusivo por código enviado a e-mail `@prezensa.com`, com criação de conta separada de login.

Decisão: manter conta e sessão como módulo do registration-service, verificar acesso à caixa antes de criar conta ou emitir sessão e usar token opaco revogável. O [contrato HTTP e suas garantias](../architecture/09-registration-api.md) é a fonte detalhada; [persistência](../architecture/06-persistence-strategy.md) descreve os stores.

Alternativa: autoridade OIDC/JWT externa simplificaria a delegação de identidade, mas reintroduziria um provedor e um fluxo de conta não definidos pelo projeto. Uma sessão em cookie dependeria de decisões do frontend ainda ausente.

Consequência: Registration mantém provisoriamente dados de acesso além dos aggregates de cadastro. A futura extração de Identity, recuperação de conta e requisitos de garantia mais alta precisarão de contratos e decisão explícitos. Nenhuma outra forma de login é oferecida.

# ADR 0002: Hexagonal e vertical slices enxutas

Status: aceito.

Contexto: precisamos proteger regras de negócio sem uma hierarquia de abstrações cerimonial.

Decisão: aplicar camadas hexagonais enxutas e organizar casos de uso por feature, criando hosts Api/Worker conforme entradas reais. Minimal APIs serão o adapter HTTP. SDK .NET 10 e C# 14 são o baseline. A regra detalhada de referências e organização está em [dependências](../architecture/03-dependency-rules.md).

Alternativa: controller tradicional ou interfaces para cada classe são possíveis, mas não resolvem um problema presente. Podemos adotá-los localmente se houver requisito concreto.

Consequência: não há projeto Presentation extra, mediator obrigatório, `BuildingBlocks` ou wrapper de telemetria inicial.

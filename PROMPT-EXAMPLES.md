# Prompts para usar a esteira

Os exemplos abaixo assumem que o `AGENTS.md` da raiz é a fonte principal das regras de orquestração, Git, arquitetura, testes e entrega. Uma nova atividade deve usar branch própria baseada em `develop`; uma continuação deve reutilizar a branch da atividade atual. Nenhum exemplo autoriza automaticamente commit, push, merge, rebase ou criação de Pull Request.

## Feature descrita em arquivo

```text
Implemente FEATURE-ID descrita em docs/requirements/FEATURE-ID.md.

Considere esta solicitação uma nova atividade. Antes de qualquer alteração, o orquestrador deve preparar o Git conforme o AGENTS.md: se develop ainda não existir, criá-la uma única vez a partir da main; depois criar uma branch feature/* baseada em develop.

Use a esteira multiagente definida no AGENTS.md:
architect → developer → reviewer + qa → correções → platform → quality gates.

Não faça commit, push, merge, rebase ou Pull Request. Não finalize enquanto os gates obrigatórios não passarem. Se uma decisão de negócio material estiver ambígua, pare e me pergunte.
```

## Endpoint informado diretamente

```text
Implemente POST /api/v1/proposals.

Contrato:
- request: name, email, phone e message opcional;
- 201 Created no sucesso;
- 400 com Problem Details para entrada inválida;
- 409 quando o mesmo telefone normalizado já tiver proposta nas últimas 24 horas.

Persistência: PostgreSQL com Dapper.
Requisitos: async, CancellationToken, logs estruturados, migration, testes unitários e de integração.

Esta é uma nova atividade. O orquestrador deve criar uma branch feature/* baseada em develop antes de delegar trabalho com escrita. Se develop ainda não existir, aplicar primeiro o bootstrap baseado em main definido no AGENTS.md.

Antes de editar, peça ao architect para esclarecer as decisões ocultas e montar o plano. Depois execute a esteira padrão do AGENTS.md. Não faça commit, push, merge, rebase ou Pull Request.
```

## Continuação da mesma atividade

```text
Continue a implementação de FEATURE-ID na branch atual.

Esta solicitação pertence ao mesmo escopo e ao mesmo Pull Request da atividade em andamento. Reutilize a branch atual; não crie uma nova branch.

Execute as etapas restantes da esteira do AGENTS.md, incluindo reviewer e qa após mudanças materiais e todos os quality gates obrigatórios. Preserve alterações não relacionadas. Não faça commit, push, merge, rebase ou Pull Request.
```

## Apenas revisão

```text
Revise a branch de atividade atual contra develop usando reviewer e qa em paralelo. Não altere arquivos nem o estado do Git.

Consolide achados por severidade, com arquivo, cenário, consequência e correção recomendada. Diferencie defeitos confirmados, lacunas de teste e riscos não verificados.
```

## Revisão de promoção para produção

```text
Revise o diff de develop contra main para avaliar uma futura promoção para produção.

Use reviewer e qa em paralelo, ambos somente leitura. Verifique regressões, segurança, migrations, PostgreSQL/Dapper, autenticação JWT, Docker, README e evidências dos quality gates.

Não altere arquivos e não faça merge, rebase, push, release ou Pull Request. Entregue os bloqueios e riscos de promoção por severidade.
```

## Correção já especificada

```text
Implemente a correção descrita em docs/requirements/BUG-ID.md.

Considere esta correção uma nova atividade independente. O orquestrador deve criar uma branch fix/* baseada em develop antes de qualquer alteração. Se a correção for apenas um achado da feature atualmente em desenvolvimento, trate-a como continuação e reutilize a branch atual em vez de criar outra.

Como o escopo já está aprovado e é localizado, avalie se architect é realmente necessário e registre a decisão. Use developer para implementar, reviewer e qa para validar, e platform apenas se houver impacto operacional. Não faça commit, push, merge, rebase ou Pull Request.
```

## Hotfix de produção

```text
Implemente o hotfix descrito em docs/requirements/HOTFIX-ID.md.

Autorizo classificar esta solicitação como correção urgente de produção. O orquestrador deve criar uma branch hotfix/* baseada em main, conforme o AGENTS.md, antes de qualquer alteração.

Execute planejamento proporcional ao risco, implementação, testes, reviewer, qa e quality gates. Não faça commit, push, merge, rebase, release ou Pull Request sem uma autorização posterior e específica.
```

## Preparar commit após validação

```text
Os testes e quality gates desta atividade já foram executados. Verifique novamente o diff e o estado do Git.

Se não houver falhas, segredos ou alterações não relacionadas, crie commits pequenos usando Conventional Commits na branch atual. Esta autorização vale apenas para commit local: não faça push, merge, rebase, Pull Request, release ou exclusão de branch.

Ao final, informe os hashes e mensagens dos commits e o próximo passo recomendado.
```

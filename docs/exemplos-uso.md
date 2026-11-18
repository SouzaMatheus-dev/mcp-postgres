# Exemplos de uso

Valide primeiro com `usuario_conectado`. O campo `modo_mcp` deve ser `somente_leitura`.

## Mapear um banco

```
listar_schemas()
listar_tabelas(schema="public")
descrever_tabela(tabela="pedidos", schema="public")
listar_chaves_estrangeiras(schema="public", tabela="pedidos")
listar_indices(schema="public", tabela="pedidos")
```

## Trocar de banco no mesmo servidor

```
listar_bancos()
listar_tabelas(database="vendas")
executar_consulta(sql="SELECT * FROM public.pedidos LIMIT 10", database="financeiro")
```

Cada `database` abre outra conexão com o mesmo usuário. O usuário precisa de `CONNECT` naquele banco.

## Amostra e perfil

```
amostrar_tabela(tabela="pedidos", schema="public", limite=20)
perfil_coluna(tabela="pedidos", coluna="status", schema="public")
```

## Plano sem executar a consulta

```
estimar_plano_consulta(sql="SELECT * FROM public.pedidos WHERE status = 'aberto'")
extrair_sugestoes_plano(sql="SELECT * FROM public.pedidos WHERE status = 'aberto'")
```

`medir_consulta` usa `EXPLAIN (ANALYZE, BUFFERS)` e portanto executa o `SELECT`.

## Estatísticas de runtime

Defina `PG_ENABLE_PERFORMANCE_STATS=true`. `consultas_lentas` só responde se `pg_stat_statements` existir no banco.

```
consultas_lentas(limite=20)
indices_nao_utilizados(schema="public")
```

## Três ambientes

Com `postgres-dev`, `postgres-hml` e `postgres-prod` no cliente:

```
No postgres-dev, liste as tabelas do schema public.
No postgres-hml, faça o mesmo e compare.
```

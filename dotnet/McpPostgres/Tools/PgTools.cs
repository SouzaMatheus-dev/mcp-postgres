using System.ComponentModel;
using System.Text;
using Npgsql;
using ModelContextProtocol.Server;
using McpPostgres.Services;

namespace McpPostgres.Tools;

[McpServerToolType]
public sealed class PgTools(McpConfig config, PgExecutor executor)
{
    private const string UserSchemas = "n.nspname NOT IN ('pg_catalog', 'information_schema') AND n.nspname NOT LIKE 'pg_%'";

    [McpServerTool, Description("Mostra usuário da sessão, usuário efetivo, banco atual, servidor e modo do MCP.")]
    public string UsuarioConectado()
    {
        var modo = config.ReadOnly ? "somente_leitura" : "leitura_escrita";
        return executor.Execute(
            """
            SELECT
                session_user AS login,
                current_user AS usuario_banco,
                current_database() AS banco_atual,
                COALESCE(inet_server_addr()::text, 'local') AS servidor,
                inet_server_port() AS porta,
                @modo AS modo_mcp
            """,
            parameters: [new NpgsqlParameter("modo", modo)]);
    }

    [McpServerTool, Description("Lista bancos de dados do cluster visíveis para o usuário conectado.")]
    public string ListarBancos()
    {
        return executor.Execute(
            """
            SELECT datname AS name,
                   pg_encoding_to_char(encoding) AS encoding,
                   datallowconn AS permite_conexao
            FROM pg_database
            WHERE datistemplate = false
            ORDER BY datname
            """);
    }

    [McpServerTool, Description("Lista schemas de usuário do banco (exclui pg_catalog e information_schema).")]
    public string ListarSchemas(string database = "")
    {
        return executor.Execute(
            """
            SELECT n.nspname AS schema_name
            FROM pg_namespace n
            WHERE n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND n.nspname NOT LIKE 'pg_%'
            ORDER BY schema_name
            """,
            Db(database));
    }

    [McpServerTool, Description("Lista apenas tabelas base (não inclui views).")]
    public string ListarTabelas(string database = "", string schema = "")
    {
        var sql = """
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND table_schema NOT IN ('pg_catalog', 'information_schema')
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND table_schema = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += " ORDER BY table_schema, table_name";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Lista views e materialized views disponíveis para leitura.")]
    public string ListarViews(string database = "", string schema = "")
    {
        var sql = """
            SELECT n.nspname AS table_schema,
                   c.relname AS table_name,
                   CASE c.relkind WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE 'VIEW' END AS tipo
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind IN ('v', 'm')
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND n.nspname = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += " ORDER BY table_schema, table_name";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Lista functions e procedures visíveis (somente metadados).")]
    public string ListarProcedures(string database = "", string schema = "")
    {
        var sql = $"""
            SELECT
                n.nspname AS schema_name,
                p.proname AS object_name,
                CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END AS tipo,
                pg_get_function_identity_arguments(p.oid) AS argumentos
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE {UserSchemas}
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND n.nspname = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += " ORDER BY schema_name, object_name";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Mostra colunas, tipos, nulabilidade e chave primária de uma tabela.")]
    public string DescreverTabela(string tabela, string database = "", string schema = "public")
    {
        const string sql = """
            SELECT
                c.column_name,
                c.data_type,
                c.character_maximum_length AS tamanho,
                c.is_nullable,
                c.column_default,
                CASE WHEN pk.column_name IS NOT NULL THEN 'PK' ELSE '' END AS chave
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT kcu.table_schema, kcu.table_name, kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                   AND tc.table_schema = kcu.table_schema
                   AND tc.constraint_schema = kcu.constraint_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
            ) pk ON pk.table_schema = c.table_schema
                AND pk.table_name = c.table_name
                AND pk.column_name = c.column_name
            WHERE c.table_name = @tabela AND c.table_schema = @schema
            ORDER BY c.ordinal_position
            """;
        return executor.Execute(
            sql,
            Db(database),
            new NpgsqlParameter("tabela", tabela),
            new NpgsqlParameter("schema", schema));
    }

    [McpServerTool, Description("Mostra colunas e tipos de uma view ou materialized view.")]
    public string DescreverView(string view, string database = "", string schema = "public")
    {
        const string sql = """
            SELECT
                a.attname AS column_name,
                format_type(a.atttypid, a.atttypmod) AS data_type,
                CASE WHEN a.attnotnull THEN 'NO' ELSE 'YES' END AS is_nullable
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
            WHERE c.relkind IN ('v', 'm')
              AND c.relname = @view
              AND n.nspname = @schema
            ORDER BY a.attnum
            """;
        return executor.Execute(
            sql,
            Db(database),
            new NpgsqlParameter("view", view),
            new NpgsqlParameter("schema", schema));
    }

    [McpServerTool, Description("Mostra parâmetros, tipos e direção de uma function ou procedure.")]
    public string DescreverProcedure(string procedure, string database = "", string schema = "public")
    {
        const string sql = """
            SELECT
                COALESCE(p.proargnames[args.ord], '') AS parametro,
                format_type(args.argtype, NULL) AS tipo,
                CASE p.proargmodes[args.ord]
                    WHEN 'i' THEN 'IN'
                    WHEN 'o' THEN 'OUT'
                    WHEN 'b' THEN 'INOUT'
                    WHEN 'v' THEN 'VARIADIC'
                    WHEN 't' THEN 'TABLE'
                    ELSE 'IN'
                END AS direcao
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN LATERAL unnest(COALESCE(p.proallargtypes, p.proargtypes::oid[])) WITH ORDINALITY AS args(argtype, ord) ON true
            WHERE p.proname = @procedure
              AND n.nspname = @schema
            ORDER BY args.ord
            """;
        return executor.Execute(
            sql,
            Db(database),
            new NpgsqlParameter("procedure", procedure),
            new NpgsqlParameter("schema", schema));
    }

    [McpServerTool, Description("Retorna a definição SQL de uma view, materialized view, function ou procedure.")]
    public string ObterDefinicaoSql(string objeto, string database = "", string schema = "public", string tipo = "")
    {
        var tipoNorm = tipo.Trim().ToUpperInvariant();
        var incluirView = tipoNorm is "" or "VIEW" or "MATERIALIZED VIEW";
        var incluirRotina = tipoNorm is "" or "FUNCTION" or "PROCEDURE";
        if (!incluirView && !incluirRotina)
        {
            return "tipo deve ser VIEW, MATERIALIZED VIEW, FUNCTION, PROCEDURE ou vazio.";
        }

        var parts = new List<string>();
        var parameters = new List<NpgsqlParameter>
        {
            new("objeto", objeto),
            new("schema", schema),
            new("tipo", tipoNorm),
        };

        if (incluirView)
        {
            parts.Add(
                """
                SELECT
                    n.nspname AS schema_name,
                    c.relname AS object_name,
                    CASE c.relkind WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE 'VIEW' END AS tipo,
                    pg_get_viewdef(c.oid, true) AS definicao_sql
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relkind IN ('v', 'm')
                  AND c.relname = @objeto
                  AND n.nspname = @schema
                  AND (@tipo = '' OR @tipo = CASE c.relkind WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE 'VIEW' END)
                """);
        }

        if (incluirRotina)
        {
            parts.Add(
                """
                SELECT
                    n.nspname AS schema_name,
                    p.proname AS object_name,
                    CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END AS tipo,
                    pg_get_functiondef(p.oid) AS definicao_sql
                FROM pg_proc p
                JOIN pg_namespace n ON n.oid = p.pronamespace
                WHERE p.proname = @objeto
                  AND n.nspname = @schema
                  AND (@tipo = '' OR @tipo = CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END)
                """);
        }

        var sql = string.Join("\nUNION ALL\n", parts) + "\nORDER BY schema_name, object_name, tipo";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Consulta uma view com LIMIT (somente leitura).")]
    public string ConsultarView(string view, string database = "", string schema = "public", int limite = 100)
    {
        if (limite < 1 || limite > config.MaxRows)
        {
            return $"Bloqueado: limite deve estar entre 1 e {config.MaxRows}.";
        }

        if (!IsSafeIdentifier(schema) || !IsSafeIdentifier(view))
        {
            return "Bloqueado: schema e view devem conter apenas letras, números e underscore, e começar com letra ou underscore.";
        }

        var sql = $"SELECT * FROM {Quote(schema)}.{Quote(view)} LIMIT {limite}";
        var error = ReadOnlyValidator.Validate(sql);
        if (error is not null)
        {
            return error;
        }

        return executor.Execute(sql, Db(database));
    }

    [McpServerTool, Description("Executa SELECT/WITH validado. Bloqueia DDL, DML e CALL quando PG_READONLY=true.")]
    public string ExecutarConsulta(string sql, string database = "")
    {
        if (config.ReadOnly)
        {
            var error = ReadOnlyValidator.Validate(sql);
            if (error is not null)
            {
                return error;
            }
        }

        return executor.Execute(sql, Db(database));
    }

    [McpServerTool, Description("Lista foreign keys do banco. Opcionalmente filtra por schema ou tabela.")]
    public string ListarChavesEstrangeiras(string database = "", string schema = "", string tabela = "")
    {
        var sql = """
            SELECT
                c.conname AS constraint_name,
                n.nspname AS tabela_origem_schema,
                t.relname AS tabela_origem,
                a.attname AS coluna_origem,
                nr.nspname AS tabela_destino_schema,
                tr.relname AS tabela_destino,
                ar.attname AS coluna_destino
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            JOIN pg_class tr ON tr.oid = c.confrelid
            JOIN pg_namespace nr ON nr.oid = tr.relnamespace
            JOIN LATERAL unnest(c.conkey, c.confkey) WITH ORDINALITY AS cols(attnum, refattnum, ord) ON true
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = cols.attnum
            JOIN pg_attribute ar ON ar.attrelid = tr.oid AND ar.attnum = cols.refattnum
            WHERE c.contype = 'f'
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND n.nspname = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        if (!string.IsNullOrWhiteSpace(tabela))
        {
            sql += " AND t.relname = @tabela";
            parameters.Add(new NpgsqlParameter("tabela", tabela));
        }

        sql += " ORDER BY tabela_origem_schema, tabela_origem, constraint_name, cols.ord";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Busca colunas pelo nome (ILIKE). Ex: 'cliente' encontra cliente_id e nome_cliente.")]
    public string BuscarColuna(string termo, string database = "")
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return "Informe um termo de busca para a coluna.";
        }

        const string sql = """
            SELECT c.table_schema, c.table_name, c.column_name, c.data_type, c.is_nullable
            FROM information_schema.columns c
            WHERE c.table_schema NOT IN ('pg_catalog', 'information_schema')
              AND c.column_name ILIKE @termo ESCAPE '\'
            ORDER BY c.table_schema, c.table_name, c.ordinal_position
            """;
        return executor.Execute(
            sql,
            Db(database),
            new NpgsqlParameter("termo", LikePattern(termo)));
    }

    [McpServerTool, Description("Busca tabelas, views e routines pelo nome (ILIKE).")]
    public string BuscarObjeto(string termo, string database = "")
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return "Informe um termo de busca para o objeto.";
        }

        var sql = $"""
            SELECT n.nspname AS schema_name,
                   c.relname AS object_name,
                   CASE c.relkind
                       WHEN 'r' THEN 'TABLE'
                       WHEN 'v' THEN 'VIEW'
                       WHEN 'm' THEN 'MATERIALIZED VIEW'
                   END AS tipo
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind IN ('r', 'v', 'm')
              AND {UserSchemas}
              AND c.relname ILIKE @termo ESCAPE '\'
            UNION ALL
            SELECT n.nspname,
                   p.proname,
                   CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE {UserSchemas}
              AND p.proname ILIKE @termo ESCAPE '\'
            ORDER BY schema_name, object_name
            """;
        return executor.Execute(sql, Db(database), new NpgsqlParameter("termo", LikePattern(termo)));
    }

    [McpServerTool, Description("Retorna comentários (COMMENT ON) de tabela, view, function e suas colunas.")]
    public string ObterDocumentacaoObjeto(string objeto, string database = "", string schema = "public")
    {
        const string sql = """
            SELECT
                n.nspname AS schema_name,
                c.relname AS object_name,
                CASE c.relkind WHEN 'r' THEN 'TABLE' WHEN 'v' THEN 'VIEW' WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE c.relkind::text END AS tipo,
                'objeto' AS nivel,
                d.description AS descricao
            FROM pg_description d
            JOIN pg_class c ON c.oid = d.objoid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE d.classoid = 'pg_class'::regclass
              AND d.objsubid = 0
              AND c.relname = @objeto
              AND n.nspname = @schema
            UNION ALL
            SELECT
                n.nspname,
                c.relname,
                CASE c.relkind WHEN 'r' THEN 'TABLE' WHEN 'v' THEN 'VIEW' WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE c.relkind::text END,
                a.attname,
                d.description
            FROM pg_description d
            JOIN pg_class c ON c.oid = d.objoid
            JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = d.objsubid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE d.classoid = 'pg_class'::regclass
              AND d.objsubid > 0
              AND c.relname = @objeto
              AND n.nspname = @schema
            UNION ALL
            SELECT
                n.nspname,
                p.proname,
                CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END,
                'objeto',
                d.description
            FROM pg_description d
            JOIN pg_proc p ON p.oid = d.objoid
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE d.classoid = 'pg_proc'::regclass
              AND p.proname = @objeto
              AND n.nspname = @schema
            ORDER BY nivel
            """;
        return executor.Execute(
            sql,
            Db(database),
            new NpgsqlParameter("objeto", objeto),
            new NpgsqlParameter("schema", schema));
    }

    [McpServerTool, Description("Lista índices (btree, hash, gin, unique, PK). Filtra por schema/tabela.")]
    public string ListarIndices(string database = "", string schema = "", string tabela = "")
    {
        var sql = """
            SELECT
                n.nspname AS schema_name,
                t.relname AS tabela,
                i.relname AS indice,
                am.amname AS tipo_indice,
                ix.indisunique AS is_unique,
                ix.indisprimary AS is_primary_key,
                (
                    SELECT string_agg(a.attname, ', ' ORDER BY cols.ord)
                    FROM unnest(ix.indkey) WITH ORDINALITY AS cols(attnum, ord)
                    JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = cols.attnum
                    WHERE cols.ord <= ix.indnkeyatts AND cols.attnum > 0
                ) AS colunas
            FROM pg_index ix
            JOIN pg_class t ON t.oid = ix.indrelid
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            JOIN pg_am am ON am.oid = i.relam
            WHERE n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND t.relkind = 'r'
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND n.nspname = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        if (!string.IsNullOrWhiteSpace(tabela))
        {
            sql += " AND t.relname = @tabela";
            parameters.Add(new NpgsqlParameter("tabela", tabela));
        }

        sql += " ORDER BY schema_name, tabela, indice";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Lista dependências de/para um objeto. direcao: ambos, referencia, referenciado_por.")]
    public string ListarDependencias(
        string objeto,
        string database = "",
        string schema = "public",
        string direcao = "ambos")
    {
        var direcaoNorm = direcao.Trim().ToLowerInvariant();
        if (direcaoNorm is not ("ambos" or "referencia" or "referenciado_por"))
        {
            return "direcao deve ser: ambos, referencia ou referenciado_por.";
        }

        var parts = new List<string>();
        if (direcaoNorm is "ambos" or "referencia")
        {
            parts.Add(DependencySql(referencia: true));
        }

        if (direcaoNorm is "ambos" or "referenciado_por")
        {
            parts.Add(DependencySql(referencia: false));
        }

        var sql = string.Join("\nUNION\n", parts) + """

            ORDER BY referenciador_schema, referenciador, referenciado_schema, referenciado
            """;
        return executor.Execute(
            sql,
            Db(database),
            new NpgsqlParameter("objeto", objeto),
            new NpgsqlParameter("schema", schema));
    }

    [McpServerTool, Description("Visão geral do banco: contagens por tipo de objeto e TOP 20 maiores tabelas.")]
    public string ResumirBanco(string database = "")
    {
        var sqlContagens = $"""
            SELECT categoria, SUM(quantidade)::bigint AS quantidade
            FROM (
                SELECT
                    CASE c.relkind
                        WHEN 'r' THEN 'TABLE'
                        WHEN 'v' THEN 'VIEW'
                        WHEN 'm' THEN 'MATERIALIZED VIEW'
                        WHEN 'i' THEN 'INDEX'
                        WHEN 'S' THEN 'SEQUENCE'
                        ELSE c.relkind::text
                    END AS categoria,
                    COUNT(*) AS quantidade
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE {UserSchemas}
                  AND c.relkind IN ('r', 'v', 'm', 'i', 'S')
                GROUP BY c.relkind
                UNION ALL
                SELECT
                    CASE WHEN p.prokind = 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END,
                    COUNT(*)
                FROM pg_proc p
                JOIN pg_namespace n ON n.oid = p.pronamespace
                WHERE {UserSchemas}
                GROUP BY p.prokind
            ) objetos
            GROUP BY categoria
            ORDER BY quantidade DESC
            """;
        const string sqlMaiores = """
            SELECT
                n.nspname AS schema_name,
                c.relname AS tabela,
                c.reltuples::bigint AS linhas_aprox,
                ROUND((pg_total_relation_size(c.oid) / 1024.0 / 1024.0)::numeric, 2) AS tamanho_mb
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind = 'r'
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND n.nspname NOT LIKE 'pg_%'
            ORDER BY pg_total_relation_size(c.oid) DESC
            LIMIT 20
            """;
        var db = Db(database);
        var contagens = executor.Execute(sqlContagens, db);
        var maiores = executor.Execute(sqlMaiores, db);
        return $"""
            === Contagens por tipo de objeto ===
            {contagens}

            === Maiores tabelas (TOP 20 por tamanho) ===
            {maiores}
            """;
    }

    [McpServerTool, Description("Amostra linhas de uma tabela (SELECT com LIMIT).")]
    public string AmostrarTabela(string tabela, string database = "", string schema = "public", int limite = 20)
    {
        if (limite < 1 || limite > config.MaxRows)
        {
            return $"Bloqueado: limite deve estar entre 1 e {config.MaxRows}.";
        }

        if (!IsSafeIdentifier(schema) || !IsSafeIdentifier(tabela))
        {
            return "Bloqueado: schema e tabela devem conter apenas letras, números e underscore, e começar com letra ou underscore.";
        }

        var sql = $"SELECT * FROM {Quote(schema)}.{Quote(tabela)} LIMIT {limite}";
        var error = ReadOnlyValidator.Validate(sql);
        if (error is not null)
        {
            return error;
        }

        return executor.Execute(sql, Db(database));
    }

    [McpServerTool, Description("Perfil estatístico de uma coluna: nulos, distintos, min/max e amostras.")]
    public string PerfilColuna(string tabela, string coluna, string database = "", string schema = "public")
    {
        if (!IsSafeIdentifier(schema) || !IsSafeIdentifier(tabela) || !IsSafeIdentifier(coluna))
        {
            return "Bloqueado: identificadores devem conter apenas letras, números e underscore, e começar com letra ou underscore.";
        }

        const string metaSql = """
            SELECT data_type
            FROM information_schema.columns
            WHERE table_schema = @schema AND table_name = @tabela AND column_name = @coluna
            """;
        var db = Db(database);
        var meta = executor.Execute(
            metaSql,
            db,
            new NpgsqlParameter("schema", schema),
            new NpgsqlParameter("tabela", tabela),
            new NpgsqlParameter("coluna", coluna));

        if (meta.Contains("(0 linha(s) exibida(s))", StringComparison.Ordinal))
        {
            return $"Coluna {Quote(schema)}.{Quote(tabela)}.{Quote(coluna)} não encontrada.";
        }

        var dataType = meta.Split('\n')
            .Skip(2)
            .Select(line => line.Split('|').FirstOrDefault()?.Trim().ToLowerInvariant())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "desconhecido";

        var quotedColumn = Quote(coluna);
        var quotedTable = $"{Quote(schema)}.{Quote(tabela)}";
        var stats = executor.Execute(
            $"""
            SELECT
                COUNT(*) AS total_linhas,
                SUM(CASE WHEN {quotedColumn} IS NULL THEN 1 ELSE 0 END) AS nulos,
                COUNT(DISTINCT {quotedColumn}) AS valores_distintos
            FROM {quotedTable}
            """,
            db);

        var extras = new List<string>();
        if (IsComparableType(dataType))
        {
            extras.Add("=== Min / Max ===");
            extras.Add(executor.Execute(
                $"SELECT MIN({quotedColumn}) AS minimo, MAX({quotedColumn}) AS maximo FROM {quotedTable}",
                db));
        }

        extras.Add("=== Valores mais frequentes (TOP 5) ===");
        extras.Add(executor.Execute(
            $"""
            SELECT {quotedColumn} AS valor, COUNT(*) AS ocorrencias
            FROM {quotedTable}
            WHERE {quotedColumn} IS NOT NULL
            GROUP BY {quotedColumn}
            ORDER BY ocorrencias DESC
            LIMIT 5
            """,
            db));

        return $"""
            === Perfil de {quotedTable}.{quotedColumn} (tipo: {dataType}) ===
            {stats}

            {string.Join("\n\n", extras)}
            """;
    }

    [McpServerTool, Description("Busca texto dentro de views, functions e procedures.")]
    public string BuscarTextoSql(string termo, string database = "", string schema = "")
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return "Informe um termo de busca.";
        }

        var sql = """
            SELECT table_schema AS schema_name,
                   table_name AS object_name,
                   'VIEW' AS tipo,
                   LEFT(view_definition, 500) AS trecho
            FROM information_schema.views
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
              AND view_definition ILIKE @termo ESCAPE '\'
            """;
        var parameters = new List<NpgsqlParameter> { new("termo", LikePattern(termo)) };
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND table_schema = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += """

            UNION ALL
            SELECT n.nspname,
                   p.proname,
                   CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END,
                   LEFT(p.prosrc, 500)
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND p.prosrc ILIKE @termo ESCAPE '\'
            """;
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND n.nspname = @schema";
        }

        sql += " ORDER BY schema_name, object_name";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Lista FKs cuja primeira coluna não é a primeira coluna de nenhum índice.")]
    public string ListarFksSemIndice(string database = "", string schema = "")
    {
        var sql = """
            SELECT
                n.nspname AS schema_name,
                t.relname AS tabela,
                c.conname AS fk_name,
                a.attname AS coluna_fk,
                nr.nspname AS ref_schema,
                tr.relname AS tabela_referenciada,
                ar.attname AS coluna_referenciada
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            JOIN pg_class tr ON tr.oid = c.confrelid
            JOIN pg_namespace nr ON nr.oid = tr.relnamespace
            JOIN LATERAL unnest(c.conkey, c.confkey) WITH ORDINALITY AS cols(attnum, refattnum, ord) ON cols.ord = 1
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = cols.attnum
            JOIN pg_attribute ar ON ar.attrelid = tr.oid AND ar.attnum = cols.refattnum
            WHERE c.contype = 'f'
              AND NOT EXISTS (
                  SELECT 1
                  FROM pg_index ix
                  JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS k(attnum, ord) ON k.ord = 1
                  WHERE ix.indrelid = t.oid
                    AND k.attnum = cols.attnum
              )
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND n.nspname = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += " ORDER BY schema_name, tabela, fk_name";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Verifica se colunas (separadas por vírgula) são cobertas por índices existentes.")]
    public string AnalisarCoberturaIndice(
        string tabela,
        string colunas,
        string database = "",
        string schema = "public")
    {
        var colList = colunas
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (colList.Count == 0)
        {
            return "Informe ao menos uma coluna (ex: status,cliente_id).";
        }

        if (!IsSafeIdentifier(schema) || !IsSafeIdentifier(tabela)
            || colList.Any(column => !IsSafeIdentifier(column)))
        {
            return "Bloqueado: identificadores devem conter apenas letras, números e underscore, e começar com letra ou underscore.";
        }

        const string sql = """
            SELECT
                i.relname AS indice,
                cols.ord AS key_ordinal,
                cols.ord > ix.indnkeyatts AS is_included,
                a.attname AS coluna
            FROM pg_index ix
            JOIN pg_class t ON t.oid = ix.indrelid
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS cols(attnum, ord) ON cols.attnum > 0
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = cols.attnum
            WHERE n.nspname = @schema AND t.relname = @tabela
            ORDER BY i.relname, cols.ord
            """;

        try
        {
            using var connection = new NpgsqlConnection(config.GetConnectionString(Db(database)));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = config.CommandTimeoutSeconds;
            command.Parameters.Add(new NpgsqlParameter("schema", schema));
            command.Parameters.Add(new NpgsqlParameter("tabela", tabela));

            var indices = new Dictionary<string, (List<string> Keys, List<string> Includes)>(StringComparer.Ordinal);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var indexName = reader.GetString(0);
                var isIncluded = reader.GetBoolean(2);
                var columnName = reader.GetString(3);
                if (!indices.TryGetValue(indexName, out var parts))
                {
                    parts = (new List<string>(), new List<string>());
                    indices[indexName] = parts;
                }

                if (isIncluded)
                {
                    parts.Includes.Add(columnName);
                }
                else if (!parts.Keys.Contains(columnName))
                {
                    parts.Keys.Add(columnName);
                }
            }

            if (indices.Count == 0)
            {
                return $"Nenhum índice encontrado para {Quote(schema)}.{Quote(tabela)}.";
            }

            var output = new StringBuilder();
            output.AppendLine($"Colunas analisadas: {string.Join(", ", colList)}");
            output.AppendLine($"Tabela: {Quote(schema)}.{Quote(tabela)}");
            output.AppendLine();
            output.AppendLine("indice | cobertura | colunas_chave | colunas_include");
            output.AppendLine(new string('-', 72));

            var best = "NAO";
            foreach (var (indexName, parts) in indices.OrderBy(entry => entry.Key))
            {
                var coverage = EvaluateCoverage(colList, parts.Keys, parts.Includes);
                if (coverage.StartsWith("COMPLETA", StringComparison.Ordinal))
                {
                    best = "COMPLETA";
                }
                else if (coverage.StartsWith("PARCIAL", StringComparison.Ordinal) && best == "NAO")
                {
                    best = "PARCIAL";
                }

                output.AppendLine(
                    $"{indexName} | {coverage} | " +
                    $"{(parts.Keys.Count > 0 ? string.Join(", ", parts.Keys) : "-")} | " +
                    $"{(parts.Includes.Count > 0 ? string.Join(", ", parts.Includes) : "-")}");
            }

            output.AppendLine();
            output.AppendLine($"Melhor cobertura encontrada: {best}");
            return output.ToString().TrimEnd();
        }
        catch (PostgresException ex)
        {
            return PgErrorFormatter.Format(ex);
        }
        catch (NpgsqlException ex)
        {
            return $"Erro de conexão PostgreSQL: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    [McpServerTool, Description("Lista índices redundantes: prefixo duplicado ou chaves idênticas.")]
    public string CompararIndicesRedundantes(string database = "", string schema = "")
    {
        var sql = """
            WITH index_keys AS (
                SELECT
                    n.nspname AS schema_name,
                    t.relname AS tabela,
                    i.relname AS indice,
                    ix.indexrelid,
                    t.oid AS table_oid,
                    (
                        SELECT string_agg(a.attname, ', ' ORDER BY cols.ord)
                        FROM unnest(ix.indkey) WITH ORDINALITY AS cols(attnum, ord)
                        JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = cols.attnum
                        WHERE cols.ord <= ix.indnkeyatts AND cols.attnum > 0
                    ) AS colunas_chave
                FROM pg_index ix
                JOIN pg_class t ON t.oid = ix.indrelid
                JOIN pg_class i ON i.oid = ix.indexrelid
                JOIN pg_namespace n ON n.oid = t.relnamespace
                WHERE t.relkind = 'r'
                  AND n.nspname NOT IN ('pg_catalog', 'information_schema')
            )
            SELECT
                a.schema_name,
                a.tabela,
                a.indice AS indice_a,
                b.indice AS indice_b,
                a.colunas_chave AS chaves_a,
                b.colunas_chave AS chaves_b,
                CASE
                    WHEN a.colunas_chave = b.colunas_chave THEN 'duplicado'
                    ELSE 'prefixo'
                END AS tipo_redundancia
            FROM index_keys a
            JOIN index_keys b
                ON a.table_oid = b.table_oid
               AND a.indexrelid < b.indexrelid
            WHERE a.colunas_chave IS NOT NULL
              AND b.colunas_chave IS NOT NULL
              AND (
                  a.colunas_chave = b.colunas_chave
                  OR starts_with(b.colunas_chave, a.colunas_chave || ',')
              )
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND a.schema_name = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += " ORDER BY schema_name, tabela, indice_a, indice_b";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Colunas sem índice em tabelas grandes — candidatas a índice.")]
    public string ListarColunasCandidatasIndice(string database = "", string schema = "", int minLinhas = 1000)
    {
        if (minLinhas < 1)
        {
            return "min_linhas deve ser >= 1.";
        }

        var sql = """
            SELECT
                n.nspname AS schema_name,
                c.relname AS tabela,
                a.attname AS coluna,
                format_type(a.atttypid, a.atttypmod) AS tipo,
                c.reltuples::bigint AS linhas_aprox,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM pg_constraint fk
                    WHERE fk.conrelid = c.oid
                      AND fk.contype = 'f'
                      AND a.attnum = ANY (fk.conkey)
                ) THEN 1 ELSE 0 END AS em_fk
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
            JOIN pg_type ty ON ty.oid = a.atttypid
            WHERE c.relkind = 'r'
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND n.nspname NOT LIKE 'pg_%'
              AND c.reltuples >= @minLinhas
              AND ty.typname NOT IN ('text', 'varchar', 'bpchar', 'bytea', 'json', 'jsonb', 'xml', 'tsvector', 'jsonpath')
              AND NOT EXISTS (
                  SELECT 1
                  FROM pg_index ix
                  JOIN LATERAL unnest(ix.indkey) AS k(attnum) ON k.attnum = a.attnum
                  WHERE ix.indrelid = c.oid
                    AND k.attnum > 0
              )
            """;
        var parameters = new List<NpgsqlParameter> { new("minLinhas", minLinhas) };
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND n.nspname = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += " ORDER BY em_fk DESC, linhas_aprox DESC, schema_name, tabela, coluna LIMIT 50";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Executa SELECT validado com EXPLAIN (ANALYZE, BUFFERS). A consulta de leitura roda de verdade.")]
    public string MedirConsulta(string sql, string database = "")
    {
        var error = ReadOnlyValidator.Validate(sql);
        if (error is not null)
        {
            return error;
        }

        return executor.ExecuteExplain(sql, Db(database), analyze: true);
    }

    [McpServerTool, Description("Estima o plano com EXPLAIN e aponta Seq Scan como candidata a índice.")]
    public string ExtrairSugestoesPlano(string sql, string database = "")
    {
        var error = ReadOnlyValidator.Validate(sql);
        if (error is not null)
        {
            return error;
        }

        var plan = executor.ExecuteExplain(sql, Db(database));
        return ExplainParser.ExtractSeqScanSuggestions(plan);
    }

    [McpServerTool, Description("Lista consultas com maior tempo médio (pg_stat_statements). Requer PG_ENABLE_PERFORMANCE_STATS=true.")]
    public string ConsultasLentas(string database = "", int limite = 20)
    {
        var bloqueio = RequirePerformanceStats();
        if (bloqueio is not null)
        {
            return bloqueio;
        }

        if (limite < 1 || limite > config.MaxRows)
        {
            return $"Bloqueado: limite deve estar entre 1 e {config.MaxRows}.";
        }

        var sql = $"""
            SELECT
                calls AS execution_count,
                ROUND(total_exec_time::numeric, 2) AS total_elapsed_ms,
                ROUND(mean_exec_time::numeric, 2) AS avg_elapsed_ms,
                rows AS linhas,
                LEFT(query, 500) AS query_text
            FROM pg_stat_statements
            ORDER BY mean_exec_time DESC
            LIMIT {limite}
            """;
        return executor.Execute(sql, Db(database));
    }

    [McpServerTool, Description("Lista índices sem leituras (idx_scan = 0). Requer PG_ENABLE_PERFORMANCE_STATS=true.")]
    public string IndicesNaoUtilizados(string database = "", string schema = "")
    {
        var bloqueio = RequirePerformanceStats();
        if (bloqueio is not null)
        {
            return bloqueio;
        }

        var sql = """
            SELECT
                s.schemaname AS schema_name,
                s.relname AS tabela,
                s.indexrelname AS indice,
                s.idx_scan,
                s.idx_tup_read,
                s.idx_tup_fetch
            FROM pg_stat_user_indexes s
            JOIN pg_index ix ON ix.indexrelid = s.indexrelid
            WHERE s.idx_scan = 0
              AND NOT ix.indisprimary
              AND NOT ix.indisunique
            """;
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND s.schemaname = @schema";
            parameters.Add(new NpgsqlParameter("schema", schema));
        }

        sql += " ORDER BY schema_name, tabela, indice";
        return executor.Execute(sql, Db(database), parameters.ToArray());
    }

    [McpServerTool, Description("Estima plano de execução (EXPLAIN) para SELECT validado. Não executa a consulta.")]
    public string EstimarPlanoConsulta(string sql, string database = "")
    {
        var error = ReadOnlyValidator.Validate(sql);
        if (error is not null)
        {
            return error;
        }

        return executor.ExecuteExplain(sql, Db(database));
    }

    private string? RequirePerformanceStats() =>
        config.PerformanceStatsEnabled
            ? null
            : "Bloqueado: ferramentas de performance exigem PG_ENABLE_PERFORMANCE_STATS=true.";

    private static string DependencySql(bool referencia)
    {
        var viewFilter = referencia
            ? "nv.nspname = @schema AND v.relname = @objeto"
            : "ns.nspname = @schema AND s.relname = @objeto";
        var routineFilter = referencia
            ? "n.nspname = @schema AND p.proname = @objeto"
            : "ns.nspname = @schema AND c.relname = @objeto";

        return $"""
            SELECT
                nv.nspname AS referenciador_schema,
                v.relname AS referenciador,
                CASE v.relkind WHEN 'v' THEN 'VIEW' WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE v.relkind::text END AS tipo_referenciador,
                ns.nspname AS referenciado_schema,
                s.relname AS referenciado,
                CASE s.relkind WHEN 'r' THEN 'TABLE' WHEN 'v' THEN 'VIEW' WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE s.relkind::text END AS tipo_referenciado
            FROM pg_depend d
            JOIN pg_rewrite rw ON rw.oid = d.objid
            JOIN pg_class v ON v.oid = rw.ev_class
            JOIN pg_class s ON s.oid = d.refobjid
            JOIN pg_namespace nv ON nv.oid = v.relnamespace
            JOIN pg_namespace ns ON ns.oid = s.relnamespace
            WHERE d.classid = 'pg_rewrite'::regclass
              AND d.deptype = 'n'
              AND v.oid <> s.oid
              AND {viewFilter}
            UNION
            SELECT
                n.nspname,
                p.proname,
                CASE WHEN p.prokind = 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END,
                ns.nspname,
                c.relname,
                CASE c.relkind WHEN 'r' THEN 'TABLE' WHEN 'v' THEN 'VIEW' WHEN 'm' THEN 'MATERIALIZED VIEW' ELSE c.relkind::text END
            FROM pg_depend d
            JOIN pg_proc p ON p.oid = d.objid
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_class c ON c.oid = d.refobjid
            JOIN pg_namespace ns ON ns.oid = c.relnamespace
            WHERE d.classid = 'pg_proc'::regclass
              AND d.refclassid = 'pg_class'::regclass
              AND {routineFilter}
            """;
    }

    private static bool IsComparableType(string dataType) =>
        dataType is "integer" or "bigint" or "smallint" or "numeric" or "decimal" or "real"
            or "double precision" or "money" or "date" or "timestamp without time zone"
            or "timestamp with time zone" or "time without time zone" or "time with time zone";

    private static string EvaluateCoverage(IReadOnlyList<string> columns, List<string> keys, List<string> includes)
    {
        if (keys.Count == 0 && includes.Count == 0)
        {
            return "NAO";
        }

        if (keys.Count >= columns.Count
            && keys.Take(columns.Count).SequenceEqual(columns, StringComparer.OrdinalIgnoreCase))
        {
            return "COMPLETA (prefixo leading)";
        }

        if (columns.All(column => keys.Contains(column, StringComparer.OrdinalIgnoreCase)
            || includes.Contains(column, StringComparer.OrdinalIgnoreCase)))
        {
            return "PARCIAL";
        }

        return columns.Any(column => keys.Contains(column, StringComparer.OrdinalIgnoreCase)
            || includes.Contains(column, StringComparer.OrdinalIgnoreCase))
            ? "PARCIAL"
            : "NAO";
    }

    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && (char.IsLetter(value[0]) || value[0] == '_')
        && value.All(ch => char.IsLetterOrDigit(ch) || ch == '_');

    private static string Quote(string identifier) => $"\"{identifier}\"";

    private static string? Db(string database) =>
        string.IsNullOrWhiteSpace(database) ? null : database;

    private static string LikePattern(string termo)
    {
        var escaped = termo
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }
}

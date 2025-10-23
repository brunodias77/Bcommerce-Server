SELECT
    DISTINCT evolucao_multi.seq_evolucao_multi,
    evolucao_multi.dat_hora_inclui dat_hora_inclui,
    evolucao_multi.des_evolucao_multi des_evolucao_multi,
    paciente.nro_prontuario,
    pessoa.nom_pessoa NomPessoa,
    pessoa.dat_nascimento DatNasc,
    pessoa.NOM_MAE,
    pessoa.NRO_CPF_NUMERO,
    pessoa.NRO_CPF_DIGITO,
    atendimento.NRO_FAA,
    sihosp.get_idade_pessoa(pessoa.dat_nascimento) idade_paciente,
    internacao.seq_internacao,
    internacao.dat_hora_internacao,
    leito_historico.cod_leito || ' - ' || sihosp.get_composicao_infra(leito_historico.seq_infra_estrutura_compor) nom_infra,
    COALESCE(
        UPPER(centro_custo.nom_centro_custo),
        UPPER(especialidade_residencia.nom_especialidade)
    ) equipe,
    convenio.nom_convenio,
    anamnese_internacao.dat_hora_anamnese,
    ca_cliente_anamnese.nom_cliente profissional_inclui_anamnese,
    ca_cliente_evolucao.nom_cliente profissional_inclui_evolucao,
    LPAD(TO_CHAR(atendimento.NRO_FAA), 9, '0') AS codigo_barra
FROM
    evolucao_multi
    INNER JOIN paciente ON paciente.seq_paciente = evolucao_multi.seq_paciente
    INNER JOIN pessoa ON pessoa.seq_pessoa = paciente.seq_pessoa
    LEFT JOIN internacao ON internacao.seq_internacao = evolucao_multi.seq_internacao
    LEFT JOIN ATENDIMENTO ON ATENDIMENTO.SEQ_ATENDIMENTO = evolucao_multi.SEQ_ATENDIMENTO
    LEFT JOIN (
        SELECT
            *
        FROM
            (
                SELECT
                    mov.seq_movimentacao_internacao,
                    mov.seq_internacao,
                    mov.seq_leito,
                    mov.seq_convenio,
                    ROW_NUMBER() OVER (
                        PARTITION BY mov.seq_internacao
                        ORDER BY
                            mov.dat_hora_inclusao DESC
                    ) AS rn
                FROM
                    movimentacao_internacao mov
            )
        WHERE
            rn = 1
    ) movimentacao_internacao ON movimentacao_internacao.seq_internacao = internacao.seq_internacao
    LEFT JOIN leito_historico ON leito_historico.seq_leito = movimentacao_internacao.seq_leito
    LEFT JOIN convenio ON convenio.seq_convenio = ATENDIMENTO.seq_convenio
    LEFT JOIN (
        SELECT
            *
        FROM
            (
                SELECT
                    ai.*,
                    ROW_NUMBER() OVER (
                        PARTITION BY ai.seq_internacao
                        ORDER BY
                            ai.seq_anamnese_internacao ASC
                    ) AS rn
                FROM
                    anamnese_internacao ai
                WHERE
                    ai.dat_hora_cancela IS NULL
            )
        WHERE
            rn = 1
    ) anamnese_internacao ON anamnese_internacao.seq_internacao = internacao.seq_internacao
    LEFT JOIN ca_cliente ca_cliente_anamnese ON ca_cliente_anamnese.seq_cliente = anamnese_internacao.seq_cliente_inclui
    LEFT JOIN ca_cliente ca_cliente_evolucao ON ca_cliente_evolucao.seq_cliente = evolucao_multi.seq_cliente_inclui
    LEFT JOIN (
        SELECT
            *
        FROM
            (
                SELECT
                    ano.seq_pessoa,
                    ano.seq_curso_especialidade,
                    ROW_NUMBER() OVER(
                        PARTITION BY ano.seq_ano_letivo
                        ORDER BY
                            ano.seq_ano_letivo DESC
                    ) AS rn
                FROM
                    ano_letivo ano
                WHERE
                    EXTRACT(
                        YEAR
                        FROM
                            ano.dat_ano
                    ) = EXTRACT(
                        YEAR
                        FROM
                            SYSDATE
                    )
            )
        WHERE
            rn = 1
    ) ano_letivo ON ano_letivo.seq_pessoa = ca_cliente_evolucao.seq_pessoa_estudante
    LEFT JOIN curso_especialidade ON curso_especialidade.seq_curso_especialidade = ano_letivo.seq_curso_especialidade
    LEFT JOIN especialidade_residencia ON especialidade_residencia.seq_especialidade_residencia = curso_especialidade.seq_especialidade_residencia
    LEFT JOIN (
        SELECT
            *
        FROM
            (
                SELECT
                    prof.seq_pessoa,
                    prof.seq_centro_custo,
                    ROW_NUMBER() OVER(
                        PARTITION BY prof.seq_pessoa
                        ORDER BY
                            prof.dat_admissao DESC
                    ) AS rn
                FROM
                    profissional prof
                WHERE
                    prof.dat_desligamento IS NULL
            )
        WHERE
            rn = 1
    ) profissional ON profissional.seq_pessoa = ca_cliente_evolucao.seq_pessoa_profissional
    LEFT JOIN centro_custo ON centro_custo.seq_centro_custo = profissional.seq_centro_custo
WHERE
    evolucao_multi.seq_evolucao_multi IN (66217)
ORDER BY
    evolucao_multi.dat_hora_inclui DESC
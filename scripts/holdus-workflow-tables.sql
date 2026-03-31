-- ═══════════════════════════════════════════════════════════════
-- HOLDUS — Tabelas do Workflow (PostgreSQL)
-- 6 tabelas para persistir o Quadro Mestre do Método Tríade
-- ═══════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS workflow_instances (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    projeto_id        UUID NOT NULL UNIQUE,
    name              VARCHAR(200) NOT NULL DEFAULT 'Workflow 3 Células',
    created_at_utc    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at_utc  TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS workflow_step_states (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_instance_id    UUID NOT NULL REFERENCES workflow_instances(id) ON DELETE CASCADE,
    step_code               INT NOT NULL,
    phase                   INT NOT NULL,
    order_in_phase          INT NOT NULL,
    title                   VARCHAR(300) NOT NULL,
    objective               VARCHAR(1000) NOT NULL,
    status                  INT NOT NULL DEFAULT 0,
    started_at_utc          TIMESTAMPTZ,
    finished_at_utc         TIMESTAMPTZ,
    notes                   VARCHAR(2000),
    optional                BOOLEAN NOT NULL DEFAULT FALSE,
    UNIQUE(workflow_instance_id, step_code)
);

CREATE TABLE IF NOT EXISTS workflow_dependencies (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_step_state_entity_id   UUID NOT NULL REFERENCES workflow_step_states(id) ON DELETE CASCADE,
    depends_on_step_code            INT NOT NULL
);

CREATE TABLE IF NOT EXISTS workflow_document_requirements (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_step_state_entity_id   UUID NOT NULL REFERENCES workflow_step_states(id) ON DELETE CASCADE,
    code                            VARCHAR(150) NOT NULL,
    name                            VARCHAR(300) NOT NULL,
    required                        BOOLEAN NOT NULL DEFAULT TRUE,
    generated                       BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS workflow_validation_gates (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_step_state_entity_id   UUID NOT NULL REFERENCES workflow_step_states(id) ON DELETE CASCADE,
    code                            VARCHAR(150) NOT NULL,
    description                     VARCHAR(1000) NOT NULL,
    passed                          BOOLEAN NOT NULL DEFAULT FALSE,
    blocking                        BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS workflow_blocking_reasons (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_step_state_entity_id   UUID NOT NULL REFERENCES workflow_step_states(id) ON DELETE CASCADE,
    reason                          VARCHAR(1000) NOT NULL
);

-- Índices
CREATE INDEX IF NOT EXISTS idx_workflow_steps_instance ON workflow_step_states(workflow_instance_id);
CREATE INDEX IF NOT EXISTS idx_workflow_deps_step ON workflow_dependencies(workflow_step_state_entity_id);
CREATE INDEX IF NOT EXISTS idx_workflow_docs_step ON workflow_document_requirements(workflow_step_state_entity_id);
CREATE INDEX IF NOT EXISTS idx_workflow_gates_step ON workflow_validation_gates(workflow_step_state_entity_id);
CREATE INDEX IF NOT EXISTS idx_workflow_blocking_step ON workflow_blocking_reasons(workflow_step_state_entity_id);

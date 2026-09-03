(() => {
    "use strict";
    const fieldsSelector = "input:not([type=hidden]):not([type=submit]), select, textarea";

    function connectValidation(form) {
        form.querySelectorAll("[data-valmsg-for]").forEach(error => {
            const name = error.getAttribute("data-valmsg-for");
            const field = name ? form.elements.namedItem(name) : null;
            if (!(field instanceof HTMLElement)) return;
            if (!error.id) error.id = `${field.id || name}-error`;
            const describedBy = new Set((field.getAttribute("aria-describedby") || "").split(/\s+/).filter(Boolean));
            describedBy.add(error.id);
            field.setAttribute("aria-describedby", Array.from(describedBy).join(" "));
            if (error.textContent.trim()) field.setAttribute("aria-invalid", "true");
            const observer = new MutationObserver(() => {
                field.setAttribute("aria-invalid", String(Boolean(error.textContent.trim())));
            });
            observer.observe(error, { childList: true, subtree: true, characterData: true });
        });
    }

    function validateDates(form) {
        const starts = form.querySelector('[name="DataInicio"]');
        const ends = form.querySelector('[name="DataFim"]');
        if (!(starts instanceof HTMLInputElement) || !(ends instanceof HTMLInputElement)) return true;
        const valid = !starts.value || !ends.value || new Date(ends.value) > new Date(starts.value);
        ends.setCustomValidity(valid ? "" : "O término precisa ser posterior ao início.");
        return valid;
    }

    function focusFirstInvalid(form) {
        const invalid = form.querySelector(".input-validation-error, :invalid, [aria-invalid='true']");
        if (invalid instanceof HTMLElement) {
            invalid.focus({ preventScroll: true });
            invalid.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }

    function wireDirtyState(form) {
        if (!form.matches("[data-unsaved-form]")) return;
        let dirty = false;
        let banner = form.querySelector("[data-unsaved-banner]");
        if (!banner) {
            banner = document.createElement("span");
            banner.className = "pp-unsaved-changes-banner";
            banner.dataset.unsavedBanner = "true";
            banner.setAttribute("role", "status");
            banner.textContent = "● Existem alterações não salvas";
            form.querySelector(".pp-form-status, .pp-form-footer, .pp-form-sticky-footer")?.prepend(banner);
        }
        const setDirty = value => { dirty = value; banner?.classList.toggle("is-visible", value); };
        form.addEventListener("input", () => setDirty(true));
        form.addEventListener("change", () => setDirty(true));
        form.addEventListener("submit", () => setDirty(false));
        window.addEventListener("beforeunload", event => { if (dirty) event.preventDefault(); });
    }

    function wireSubmitFeedback(form) {
        if (!form.matches('[method="post"], [data-submit-feedback]') || form.matches('[data-ajax-form="true"], [data-saude360-form], [data-confirm="true"]')) return;
        form.addEventListener("submit", event => {
            if (event.defaultPrevented || !form.checkValidity()) return;
            const button = event.submitter instanceof HTMLButtonElement
                ? event.submitter
                : form.querySelector('button[type="submit"]');
            if (!(button instanceof HTMLButtonElement)) return;
            if (button.getAttribute("aria-busy") === "true") {
                event.preventDefault();
                return;
            }
            button.setAttribute("aria-busy", "true");
            button.disabled = true;
            button.querySelector("[data-submit-spinner]")?.classList.remove("d-none");
            const label = button.querySelector("[data-submit-label]");
            if (label) label.textContent = "Enviando com segurança…";
        });
    }

    document.querySelectorAll("[data-character-counter]").forEach(counter => {
        const field = document.getElementById(counter.dataset.for);
        if (!field) return;
        const update = () => { counter.textContent = `${field.value.length.toLocaleString("pt-BR")} de ${field.maxLength.toLocaleString("pt-BR")} caracteres`; };
        field.addEventListener("input", update); update();
    });

    document.querySelectorAll("form").forEach(form => {
        form.classList.add("pp-form-enhanced");
        form.querySelectorAll("input[required], select[required], textarea[required]").forEach(field => {
            const label = field.id ? form.querySelector(`label[for="${CSS.escape(field.id)}"]`) : null;
            if (label && !label.querySelector(".pp-required-marker")) {
                const marker = document.createElement("span");
                marker.className = "pp-required-marker";
                marker.setAttribute("aria-hidden", "true");
                marker.textContent = "*";
                label.append(marker);
            }
            field.setAttribute("aria-required", "true");
        });
        let summary = form.querySelector(".validation-summary-errors, [data-validation-summary]");
        if (!summary && form.querySelector(fieldsSelector)) {
            summary = document.createElement("div");
            summary.className = "pp-client-validation-summary";
            summary.setAttribute("role", "alert");
            summary.setAttribute("tabindex", "-1");
            const heading = document.createElement("strong"); heading.textContent = "Revise os campos destacados"; const copy = document.createElement("p"); copy.textContent = "Preencha as informações obrigatórias e corrija os formatos indicados."; summary.append(heading, copy);
            form.prepend(summary);
        }
        connectValidation(form); wireDirtyState(form);
        form.querySelectorAll(fieldsSelector).forEach(field => field.addEventListener("change", () => validateDates(form)));
        form.addEventListener("submit", event => {
            if (!validateDates(form) || !form.checkValidity()) {
                event.preventDefault();
                form.classList.add("was-validated");
                summary?.classList.add("is-visible");
                focusFirstInvalid(form);
                window.PlantaoProToast?.error("Revise os campos destacados antes de continuar.", { title: "Há informações pendentes" });
            }
        }, true);
        wireSubmitFeedback(form);
        if (form.matches("[data-focus-invalid]")) focusFirstInvalid(form);
    });

    const guide = document.querySelector("[data-screen-guide]");
    if (guide) {
        const area = location.pathname.split("/").filter(Boolean)[0]?.toLowerCase() || "início";
        const guides = {
            usuarios: ["Cadastre pessoas da instituição e controle os acessos.", "Administradores devem atribuir somente os perfis necessários.", "Cadastre ou localize uma pessoa.", "Revise perfil, status e instituição antes de salvar.", "Depois, confirme as permissões concedidas."],
            perfis: ["Organize permissões por função de trabalho.", "Administradores podem montar perfis para escala, financeiro e atendimento.", "Escolha um perfil ou crie um novo grupo.", "Conceda apenas os acessos indispensáveis.", "Depois, revise a matriz de permissões."],
            plantoes: ["Acompanhe plantões por período, unidade, profissional e cobertura.", "Coordenação e escala usam esta área para manter a operação coberta.", "Filtre o período e abra o plantão desejado.", "Confirme profissional, horário e status antes de alterar.", "Depois, acompanhe confirmação e presença."],
            escalas: ["Planeje a cobertura das unidades e organize os plantões.", "A equipe de escala deve revisar conflitos e lacunas.", "Selecione o período e a unidade de trabalho.", "Evite sobreposição de horários e plantões sem responsável.", "Depois, publique e acompanhe as confirmações."],
            financeiro: ["Controle valores previstos, pagamentos, pendências e divergências.", "Perfis financeiros trabalham somente com dados do próprio tenant.", "Use os filtros para conferir o período.", "Revise valores e favorecidos antes de concluir ações.", "Depois, acompanhe a situação financeira."],
            relatorios: ["Gere visões operacionais, financeiras e executivas da instituição.", "Gestores devem aplicar filtros antes de consultar ou exportar.", "Defina período, unidade e demais critérios.", "Exportações podem conter dados sensíveis.", "Depois, analise os indicadores ou exporte com segurança."],
            adminsaas: ["Gerencie clientes, planos, cobrança, bloqueios e suporte da plataforma.", "Esta área é exclusiva do Super Admin MNSOFT.", "Localize o cliente e escolha a ação necessária.", "Confirme tenant, impacto e motivo de ações sensíveis.", "Depois, acompanhe o evento na auditoria."],
            onboarding: ["Conduza a configuração inicial de um cliente.", "Implantação e administradores acompanham cada etapa.", "Complete os dados e requisitos da etapa atual.", "Confira responsáveis e informações da instituição.", "Depois, avance para o próximo item pendente."],
            suporte: ["Registre e acompanhe solicitações de atendimento.", "Usuários autorizados devem informar contexto sem incluir segredos.", "Descreva a necessidade e defina a prioridade correta.", "Não envie senhas, tokens ou dados clínicos desnecessários.", "Depois, acompanhe as respostas e o prazo."],
            notificacoes: ["Acompanhe eventos importantes e escolha como deseja ser avisado.", "Cada pessoa pode priorizar os alertas úteis à sua rotina.", "Filtre ou ajuste suas preferências.", "Mantenha ativos os avisos críticos da operação.", "Depois, marque como lidos os itens já tratados."]
        };
        const copy = guides[area];
        if (copy) ["purpose", "audience", "action", "care", "next"].forEach((key, index) => {
            const target = guide.querySelector(`[data-screen-guide-${key}]`);
            if (target) target.textContent = copy[index];
        });
    }
})();

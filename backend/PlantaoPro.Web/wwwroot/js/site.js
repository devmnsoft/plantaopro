document.addEventListener("DOMContentLoaded", () => {
  const body = document.body;
  const toggle = document.getElementById("sidebarToggle");
  const overlay = document.getElementById("sidebarOverlay");

  toggle?.addEventListener("click", () => body.classList.toggle("sidebar-open"));
  overlay?.addEventListener("click", () => body.classList.remove("sidebar-open"));

  document.querySelectorAll('[title]').forEach((el) => {
    if (window.bootstrap) new bootstrap.Tooltip(el);
  });

  document.querySelectorAll("form").forEach((form) => {
    form.addEventListener("submit", () => {
      form.querySelectorAll('button[type="submit"]').forEach((btn) => {
        if (!btn.disabled) {
          btn.dataset.originalText = btn.innerHTML;
          btn.disabled = true;
          btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Carregando...';
        }
      });
    });
  });

  document.querySelectorAll('[data-confirm]').forEach((el) => {
    el.addEventListener('click', (event) => {
      if (!window.confirm(el.getAttribute('data-confirm') || 'Confirma esta ação?')) {
        event.preventDefault();
      }
    });
  });
});

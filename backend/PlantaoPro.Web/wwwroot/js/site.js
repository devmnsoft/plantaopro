document.addEventListener("DOMContentLoaded", () => {
  const body = document.body;
  const toggle = document.getElementById("sidebarToggle");
  const overlay = document.getElementById("sidebarOverlay");

  toggle?.addEventListener("click", () => body.classList.toggle("sidebar-open"));
  overlay?.addEventListener("click", () => body.classList.remove("sidebar-open"));
  document.querySelectorAll('.nav-link-app').forEach((link)=>link.addEventListener('click',()=>body.classList.remove('sidebar-open')));

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
      if (el.dataset.confirmed === 'true') {
        el.dataset.confirmed = 'false';
        return;
      }
      event.preventDefault();
      const modalElement = document.getElementById('globalActionModal');
      const messageElement = document.getElementById('globalActionModalMessage');
      const confirmButton = document.getElementById('globalActionModalConfirm');
      const justificationWrap = document.getElementById('globalActionModalJustificationWrap');
      const justificationInput = document.getElementById('globalActionModalJustification');
      if (!modalElement || !confirmButton) return;

      messageElement.textContent = el.getAttribute('data-confirm') || 'Confirma esta ação?';
      const justificationField = el.getAttribute('data-justify-field');
      const requiresJustification = !!justificationField;
      justificationWrap.classList.toggle('d-none', !requiresJustification);
      if (justificationInput) justificationInput.value = '';
      confirmButton.className = `btn ${el.getAttribute('data-confirm-class') || 'btn-danger'}`;

      const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
      confirmButton.onclick = () => {
        if (requiresJustification && justificationInput && !justificationInput.value.trim()) {
          justificationInput.classList.add('is-invalid');
          return;
        }
        if (requiresJustification && justificationInput) {
          justificationInput.classList.remove('is-invalid');
          const targetForm = el.closest('form');
          if (targetForm && justificationField) {
            let hidden = targetForm.querySelector(`input[name="${justificationField}"]`);
            if (!hidden) {
              hidden = document.createElement('input');
              hidden.type = 'hidden';
              hidden.name = justificationField;
              targetForm.appendChild(hidden);
            }
            hidden.value = justificationInput.value.trim();
          }
        }
        modal.hide();
        if (el.tagName === 'A') window.location.href = el.getAttribute('href');
        else if (el.tagName === 'BUTTON' && el.type === 'submit') {
          el.dataset.confirmed = 'true';
          el.click();
        }
        else el.click();
      };
      modal.show();
    });
  });
});

function togglePassword(inputId, button){const input=document.getElementById(inputId);if(!input)return;const icon=button?.querySelector("i");const show=input.type==="password";input.type=show?"text":"password";if(icon){icon.classList.toggle("bi-eye",!show);icon.classList.toggle("bi-eye-slash",show);}}

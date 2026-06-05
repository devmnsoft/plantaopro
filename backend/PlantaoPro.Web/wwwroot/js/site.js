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

  // Loading, confirmação e AJAX são centralizados em plantaopro-ui.js para evitar duplo submit em ações críticas.

  setupLoginFeedback();
});

function setupLoginFeedback() {
  const form = document.querySelector("[data-login-form='true']");
  const warning = document.getElementById("loginAttemptWarning");
  if (!form || !warning) return;

  const failed = document.querySelector(".validation-summary-errors, .field-validation-error");
  const key = "pp_login_attempts";
  let attempts = Number(localStorage.getItem(key) || 0);
  if (failed) attempts += 1;
  else attempts = 0;

  localStorage.setItem(key, String(attempts));
  if (attempts >= 2) {
    warning.classList.remove("d-none");
    warning.querySelector("span").textContent = attempts >= 5
      ? "Múltiplas falhas detectadas. Aguarde alguns minutos antes de tentar novamente."
      : `Tentativas falhas recentes: ${attempts}. Verifique e-mail e senha.`;
  }
}

function togglePassword(inputId, button){const input=document.getElementById(inputId);if(!input)return;const icon=button?.querySelector("i");const show=input.type==="password";input.type=show?"text":"password";if(icon){icon.classList.toggle("bi-eye",!show);icon.classList.toggle("bi-eye-slash",show);}}

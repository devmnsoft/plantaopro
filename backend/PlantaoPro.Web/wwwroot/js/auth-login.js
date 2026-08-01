(() => {
  "use strict";
  const password = document.getElementById("senha");
  const toggle = document.querySelector("[data-password-toggle]");
  const warning = document.getElementById("capsLockWarning");
  if (password && toggle) {
    toggle.addEventListener("click", () => {
      const reveal = password.type === "password";
      password.type = reveal ? "text" : "password";
      toggle.setAttribute("aria-label", reveal ? "Ocultar senha" : "Mostrar senha");
      toggle.setAttribute("title", reveal ? "Ocultar senha" : "Mostrar senha");
    });
    password.addEventListener("keyup", event => warning?.classList.toggle("d-none", !event.getModifierState("CapsLock")));
  }
  document.getElementById("loginForm")?.addEventListener("submit", event => {
    const button = event.currentTarget.querySelector("#btnLogin");
    button?.setAttribute("aria-busy", "true");
    button?.querySelector(".spinner-border")?.classList.remove("d-none");
  });
})();

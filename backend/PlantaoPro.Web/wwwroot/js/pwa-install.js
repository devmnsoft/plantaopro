(function () {
    'use strict';
    // R102: prompt de instalacao PWA para o fluxo critico de triagem/recepção no celular.
    // Captura beforeinstallprompt, oferece um botao fixo discreto e some apos appinstalled.
    // Sem dependencias: estile inline e estado local.
    var deferred = null;
    var button = null;

    function ensureButton() {
        if (button || document.getElementById('pwaInstallButton')) return;
        button = document.createElement('button');
        button.id = 'pwaInstallButton';
        button.type = 'button';
        button.textContent = 'Instalar no celular';
        button.style.cssText = 'position:fixed;right:16px;bottom:16px;z-index:2100;display:none;' +
            'padding:12px 18px;border:0;border-radius:999px;background:#155eef;color:#fff;' +
            'font:600 14px/1 system-ui,sans-serif;box-shadow:0 8px 24px rgba(9,36,60,.35);cursor:pointer;';
        button.setAttribute('aria-label', 'Instalar o PlantãoPro no celular');
        button.addEventListener('click', openPrompt);
        document.body.appendChild(button);
    }

    function openPrompt() {
        if (!deferred) return;
        deferred.prompt();
        var choice = deferred.userChoice;
        if (choice && typeof choice.then === 'function') {
            choice.then(function (result) {
                if (result && result.outcome === 'accepted') hideButton();
            }).catch(function () { /* escolha rejeitada ou indisponivel */ });
        }
        deferred = null;
    }

    function hideButton() {
        if (button) {
            button.remove();
            button = null;
        }
    }

    window.addEventListener('beforeinstallprompt', function (event) {
        event.preventDefault();
        deferred = event;
        ensureButton();
        if (button) button.style.display = 'inline-block';
    });

    window.addEventListener('appinstalled', function () {
        deferred = null;
        hideButton();
    });
})();

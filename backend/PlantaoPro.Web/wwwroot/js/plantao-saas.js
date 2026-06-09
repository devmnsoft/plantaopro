(function () {
    "use strict";
    document.addEventListener("click", function (event) {
        var trigger = event.target.closest("[data-confirm-target]");
        if (!trigger) return;
        var modalElement = document.querySelector(trigger.getAttribute("data-confirm-target"));
        if (!modalElement || !window.bootstrap) return;
        var modal = window.bootstrap.Modal.getOrCreateInstance(modalElement);
        modal.show();
    });
})();

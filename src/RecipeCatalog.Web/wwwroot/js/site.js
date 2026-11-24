// Рецептник – малки подобрения на интерфейса. Всичко работи и без JavaScript.
(function () {
    "use strict";

    // Смяна на филтър или режим на търсене → автоматично изпращане на формата.
    var form = document.getElementById("search-form");
    if (form) {
        form.querySelectorAll("[data-autosubmit], input[name=mode]").forEach(function (el) {
            el.addEventListener("change", function () { form.submit(); });
        });
    }

    // Потвърждение преди изтриване.
    document.querySelectorAll("form[data-confirm]").forEach(function (f) {
        f.addEventListener("submit", function (e) {
            if (!window.confirm(f.getAttribute("data-confirm"))) {
                e.preventDefault();
            }
        });
    });
})();

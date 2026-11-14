// MediCare - Medical Records & Prescriptions (Member 5)
(function () {
    "use strict";

    // Open dialogs that the server asked to show (success message, or upload form with errors).
    document.querySelectorAll('[data-mr-autoshow="true"]').forEach(function (el) {
        if (window.bootstrap) bootstrap.Modal.getOrCreateInstance(el).show();
    });

    // Clickable table rows.
    document.querySelectorAll("tr[data-href]").forEach(function (row) {
        row.addEventListener("click", function (e) {
            if (e.target.closest("a, button, form")) return;
            window.location.href = row.dataset.href;
        });
    });

    // File inputs inside a drop zone: show chosen file names and support drag & drop.
    document.querySelectorAll(".mr-dropzone").forEach(function (zone) {
        var input = zone.querySelector('input[type="file"]');
        var label = zone.querySelector("[data-mr-file-label]");
        if (!input || !label) return;
        var original = label.textContent;

        function refresh() {
            var names = Array.prototype.map.call(input.files, function (f) { return f.name; });
            label.textContent = names.length ? names.join(", ") : original;
        }

        input.addEventListener("change", refresh);
        ["dragenter", "dragover"].forEach(function (evt) {
            zone.addEventListener(evt, function (e) { e.preventDefault(); zone.classList.add("dragover"); });
        });
        ["dragleave", "drop"].forEach(function (evt) {
            zone.addEventListener(evt, function () { zone.classList.remove("dragover"); });
        });
        zone.addEventListener("drop", function (e) {
            e.preventDefault();
            if (e.dataTransfer && e.dataTransfer.files.length) {
                input.files = e.dataTransfer.files;
                refresh();
            }
        });
    });

    // New prescription form: add / remove medication rows and keep the field indexes in order
    // (ASP.NET model binding needs Items[0], Items[1], ... with no gaps).
    var itemsBox = document.getElementById("rxItems");
    var addBtn = document.getElementById("rxAddItem");
    if (itemsBox && addBtn) {
        function renumber() {
            var rows = itemsBox.querySelectorAll("[data-rx-item]");
            rows.forEach(function (row, index) {
                row.querySelectorAll("[name]").forEach(function (field) {
                    field.name = field.name.replace(/Items\[\d+\]/, "Items[" + index + "]");
                    if (field.id) field.id = field.id.replace(/Items_\d+__/, "Items_" + index + "__");
                });
                row.querySelectorAll("[for]").forEach(function (lbl) {
                    lbl.htmlFor = lbl.htmlFor.replace(/Items_\d+__/, "Items_" + index + "__");
                });
                row.querySelectorAll("[data-valmsg-for]").forEach(function (msg) {
                    msg.setAttribute("data-valmsg-for", msg.getAttribute("data-valmsg-for").replace(/Items\[\d+\]/, "Items[" + index + "]"));
                    msg.textContent = "";
                });
                var number = row.querySelector("[data-rx-number]");
                if (number) number.textContent = "Medication " + (index + 1);
                var remove = row.querySelector("[data-rx-remove]");
                if (remove) remove.style.visibility = rows.length > 1 ? "visible" : "hidden";
            });

            // Re-parse unobtrusive validation for the new fields.
            var form = itemsBox.closest("form");
            if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive && form) {
                jQuery(form).removeData("validator").removeData("unobtrusiveValidation");
                jQuery.validator.unobtrusive.parse(form);
            }
        }

        addBtn.addEventListener("click", function () {
            var rows = itemsBox.querySelectorAll("[data-rx-item]");
            var clone = rows[rows.length - 1].cloneNode(true);
            clone.querySelectorAll("input, textarea").forEach(function (f) {
                if (f.type === "number") f.value = "30";
                else if (f.name.endsWith(".Dose")) f.value = "1 tablet";
                else f.value = "";
                f.classList.remove("input-validation-error");
            });
            clone.querySelectorAll("select").forEach(function (s) { s.selectedIndex = 0; });
            itemsBox.appendChild(clone);
            renumber();
            clone.querySelector("input").focus();
        });

        itemsBox.addEventListener("click", function (e) {
            var btn = e.target.closest("[data-rx-remove]");
            if (!btn) return;
            if (itemsBox.querySelectorAll("[data-rx-item]").length <= 1) return;
            btn.closest("[data-rx-item]").remove();
            renumber();
        });

        renumber();
    }
})();

// =========================================
// Doctors Filter: Dual-Thumb Slider & Form Sync
// =========================================
(function () {
    function initFeeSlider() {
        const minInput = document.getElementById("minFeeInput");
        const maxInput = document.getElementById("maxFeeInput");
        const minLabel = document.getElementById("minFeeLabel");
        const maxLabel = document.getElementById("maxFeeLabel");
        const track = document.getElementById("feeSliderTrack");

        if (!minInput || !maxInput || !track) return;

        const minGap = 50;
        const minBound = parseInt(minInput.min, 10) || 0;
        const maxBound = parseInt(maxInput.max, 10) || 2000;
        const rangeSpan = maxBound - minBound || 1;

        function syncUI(source) {
            let minVal = parseInt(minInput.value, 10);
            let maxVal = parseInt(maxInput.value, 10);

            if (maxVal - minVal < minGap) {
                if (source === minInput) {
                    minVal = Math.max(minBound, maxVal - minGap);
                    minInput.value = minVal;
                } else {
                    maxVal = Math.min(maxBound, minVal + minGap);
                    maxInput.value = maxVal;
                }
            }

            if (source === minInput) {
                minInput.style.zIndex = "4";
                maxInput.style.zIndex = "3";
            } else if (source === maxInput) {
                maxInput.style.zIndex = "4";
                minInput.style.zIndex = "3";
            } else {
                minInput.style.zIndex = minVal > maxBound - minGap ? "4" : "3";
                maxInput.style.zIndex = "3";
            }

            if (minLabel) minLabel.textContent = minVal;
            if (maxLabel) maxLabel.textContent = maxVal;

            const leftPct = ((minVal - minBound) / rangeSpan) * 100;
            const rightPct = ((maxVal - minBound) / rangeSpan) * 100;

            track.style.background =
                `linear-gradient(to right, ` +
                `var(--shefa-line, #E2E8F0) ${leftPct}%, ` +
                `var(--shefa-primary, #0EA5B7) ${leftPct}%, ` +
                `var(--shefa-primary, #0EA5B7) ${rightPct}%, ` +
                `var(--shefa-line, #E2E8F0) ${rightPct}%)`;
        }

        minInput.addEventListener("input", function () { syncUI(minInput); });
        maxInput.addEventListener("input", function () { syncUI(maxInput); });
        minInput.addEventListener("change", function () { syncUI(minInput); });
        maxInput.addEventListener("change", function () { syncUI(maxInput); });

        syncUI(null);
    }

    function initFilterSync() {
        const topForm = document.getElementById("topSearchForm");
        const sideForm = document.getElementById("sidebarFilterForm");
        if (!topForm || !sideForm) return;

        const filterNames = ["searchTerm", "city", "specialty", "minFee", "maxFee"];

        function syncForms(source, target) {
            filterNames.forEach(function (name) {
                const src = source.querySelector(`[name="${name}"]`);
                const dst = target.querySelector(`[name="${name}"]`);
                if (src && dst) {
                    dst.value = src.value;
                }
            });
        }

        topForm.addEventListener("submit", function () {
            syncForms(topForm, sideForm);
        });

        sideForm.addEventListener("submit", function () {
            syncForms(sideForm, topForm);
        });
    }

    function init() {
        initFeeSlider();
        initFilterSync();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
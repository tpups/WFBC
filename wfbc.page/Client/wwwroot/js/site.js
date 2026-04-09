// Site-wide JS interop functions

window.rulebookAccordion = {
    toggleAll: function (expand) {
        document.querySelectorAll('details.rulebook-section').forEach(function (d) {
            d.open = expand;
        });
    }
};
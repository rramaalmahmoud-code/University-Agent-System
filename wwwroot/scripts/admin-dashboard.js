(function () {
    "use strict";

    function initializeDashboardFilters() {
        const form = document.getElementById("dashboardFilterForm");
        const searchInput = document.getElementById("dashboardSearchInput");
        const intakeSelect = document.getElementById("dashboardIntakeSelect");

        if (!form || !searchInput || !intakeSelect) {
            console.error("Dashboard search/filter elements were not found.");
            return;
        }

        function navigateToFilteredDashboard(event) {
            if (event) {
                event.preventDefault();
            }

            const baseUrl =
                form.getAttribute("data-dashboard-filter-url") ||
                form.action;

            const parameters = new URLSearchParams();

            const search = (searchInput.value || "").trim();
            const intake = intakeSelect.value || "current";

            if (search.length > 0) {
                parameters.set("search", search);
            }

            parameters.set("intake", intake);
            parameters.set("page", "1");

            window.location.href =
                baseUrl + "?" + parameters.toString();
        }

        // Search button and normal form submission
        form.addEventListener(
            "submit",
            navigateToFilteredDashboard
        );

        // Automatically filter after changing intake
        intakeSelect.addEventListener(
            "change",
            navigateToFilteredDashboard
        );

        // Search after pressing Enter
        searchInput.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                navigateToFilteredDashboard(event);
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initializeDashboardFilters
        );
    } else {
        initializeDashboardFilters();
    }
})();
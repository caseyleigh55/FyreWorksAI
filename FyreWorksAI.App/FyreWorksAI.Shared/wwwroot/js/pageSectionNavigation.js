window.fyreWorksPageSectionNavigation = {
    scrollToSection(sectionElementId) {
        if (!sectionElementId) {
            return;
        }

        const sectionElement = document.getElementById(sectionElementId);
        if (!sectionElement) {
            return;
        }

        const prefersReducedMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches ?? false;
        sectionElement.scrollIntoView({
            behavior: prefersReducedMotion ? "auto" : "smooth",
            block: "start",
            inline: "nearest"
        });
    }
};

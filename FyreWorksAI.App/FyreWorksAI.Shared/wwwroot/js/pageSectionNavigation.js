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
        const topbarElement = document.querySelector(".shell-topbar");
        const topbarHeight = topbarElement?.getBoundingClientRect().height ?? 0;
        const sectionTop = sectionElement.getBoundingClientRect().top + window.scrollY;
        const targetTop = Math.max(sectionTop - topbarHeight - 16, 0);

        window.scrollTo({
            top: targetTop,
            behavior: prefersReducedMotion ? "auto" : "smooth"
        });
    }
};

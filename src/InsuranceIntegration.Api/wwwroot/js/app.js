// Theme interop: persists the light/dark preference in localStorage and
// applies it to the document element so CSS variables switch themes.
window.themeInterop = {
    storageKey: "ii-theme",

    get: function () {
        return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
    },

    set: function (theme) {
        const next = theme === "dark" ? "dark" : "light";
        document.documentElement.setAttribute("data-theme", next);
        try {
            localStorage.setItem(this.storageKey, next);
        } catch (e) {
            // Ignore storage failures (e.g. private mode); theme still applies for this session.
        }
        return next;
    },

    toggle: function () {
        return this.set(this.get() === "dark" ? "light" : "dark");
    }
};

// Mermaid interop for the domain-event flow diagram.
window.mermaidInterop = {
    initialized: false,

    // Resolve once the Mermaid script has finished loading. The component's
    // OnAfterRenderAsync can fire before the script is ready, so we poll
    // briefly instead of failing (and leaving a stale error graphic on) the first render.
    waitForMermaid: function (timeoutMs) {
        return new Promise((resolve) => {
            if (typeof mermaid !== "undefined") {
                resolve(true);
                return;
            }

            const started = Date.now();
            const timer = setInterval(() => {
                if (typeof mermaid !== "undefined") {
                    clearInterval(timer);
                    resolve(true);
                } else if (Date.now() - started > timeoutMs) {
                    clearInterval(timer);
                    resolve(false);
                }
            }, 50);
        });
    },

    ensureInit: function () {
        if (this.initialized || typeof mermaid === "undefined") {
            return;
        }

        mermaid.initialize({ startOnLoad: false, securityLevel: "strict", theme: "neutral" });
        this.initialized = true;
    },

    render: async function (elementId, definition) {
        const element = document.getElementById(elementId);
        if (!element) {
            return;
        }

        if (!definition) {
            element.innerHTML = "";
            return;
        }

        const ready = await this.waitForMermaid(5000);
        if (!ready) {
            element.innerHTML = "<p class='muted'>Diagram library failed to load.</p>";
            return;
        }

        this.ensureInit();

        try {
            const { svg } = await mermaid.render(elementId + "-svg", definition);
            element.innerHTML = svg;
        } catch (err) {
            element.innerHTML = "<p class='muted'>Unable to render flow diagram.</p>";
            console.error("mermaid render failed", err);
        }
    }
};
